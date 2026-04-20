using System.Text;
using System.Threading.RateLimiting;
using Azure.Identity;
using Gateway.API.Data;
using Gateway.API.Endpoints;
using Gateway.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ─── Azure Key Vault (solo en producción) ─────────────────────────────────────
if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri(
        builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault:Uri is required in Production"));

    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

// ─── ServiceDefaults ─────────────────────────────────────────────────────────
builder.AddServiceDefaults();

// ─── Serilog ─────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, svc, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(svc)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "gateway-api")
    .Enrich.WithMachineName());

// ─── Identity DbContext ───────────────────────────────────────────────────────
builder.Services.AddDbContext<GatewayIdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("IdentityDb"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

// ─── ASP.NET Core Identity ────────────────────────────────────────────────────
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength         = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedEmail     = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<GatewayIdentityDbContext>()
    .AddDefaultTokenProviders();

// ─── JWT Authentication ───────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var signingKey  = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSettings["SigningKey"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer    = true,
            ValidIssuer       = jwtSettings["Issuer"],
            ValidateAudience  = true,
            ValidAudience     = jwtSettings["Audience"],
            ValidateLifetime  = true,
            IssuerSigningKey  = signingKey,
            ClockSkew         = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Log.Warning("JWT validation failed: {Error} for {Path}",
                    ctx.Exception.Message, ctx.HttpContext.Request.Path);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var userId = ctx.Principal?
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                Log.Debug("JWT valid for user {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });

// ─── Authorization Policies ───────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOrAdmin", policy =>
        policy.RequireRole("customer", "admin"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));
});

// ─── JwtService ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtService>();

// ─── YARP ─────────────────────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ─── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("PerIp", config =>
    {
        config.Window              = TimeSpan.FromMinutes(1);
        config.PermitLimit         = 100;
        config.SegmentsPerWindow   = 4;
        config.QueueLimit          = 20;
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("CreateOrder", config =>
    {
        config.Window      = TimeSpan.FromMinutes(1);
        config.PermitLimit = 10;
        config.QueueLimit  = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title  = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please try again in 60 seconds.",
            type   = "https://orderflow.api/errors/rate-limit"
        }, ct);
    };
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("TechShopFrontend", policy =>
    {
        policy.WithOrigins(
                "https://techshop.es",
                "https://www.techshop.es",
                builder.Environment.IsDevelopment() ? "http://localhost:3000" : string.Empty)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Correlation-Id", "Location", "Retry-After");
    });
});

if (builder.Environment.IsProduction())
{
    var kvUri = builder.Configuration["KeyVault:Uri"]!;
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<GatewayIdentityDbContext>(
            name: "identity-db",
            failureStatus: HealthStatus.Unhealthy)
        .AddAzureKeyVault(
            new Uri(kvUri),
            new DefaultAzureCredential(),
            options => { options.AddSecret("Jwt--SigningKey"); },
            name: "key-vault",
            failureStatus: HealthStatus.Unhealthy);
}
else
{
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<GatewayIdentityDbContext>(
            name: "identity-db",
            failureStatus: HealthStatus.Unhealthy);
}

var app = builder.Build();

// ─── Seed de usuarios de prueba ───────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await app.Services.GetRequiredService<GatewayIdentityDbContext>()
        .Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// ─── Pipeline (orden crítico) ─────────────────────────────────────────────────
app.UseCors("TechShopFrontend");

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? System.Diagnostics.Activity.Current?.TraceId.ToString()
        ?? Guid.NewGuid().ToString("N");

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (ctx, _, _) =>
        ctx.Request.Path.StartsWithSegments("/health")
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    opts.EnrichDiagnosticContext = (diag, ctx) =>
    {
        diag.Set("CorrelationId", ctx.Items["CorrelationId"]?.ToString());
        diag.Set("ClientIp", ctx.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapAccountEndpoints();
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
