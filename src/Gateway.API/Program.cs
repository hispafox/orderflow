using System.Security.Authentication;
using System.Security.Claims;
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
            OnAuthenticationFailed = async ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                var ip = ctx.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                logger.LogWarning(
                    "[SECURITY] Authentication failed from {IpAddress}. Error: {Error}. Path: {Path}",
                    ip, ctx.Exception.Message, ctx.HttpContext.Request.Path);

                var counter  = ctx.HttpContext.RequestServices
                    .GetRequiredService<IAuthFailureCounter>();
                var failures = await counter.IncrementAsync(ip);

                if (failures > 10)
                {
                    logger.LogError(
                        "[SECURITY] Possible brute force from {IpAddress}. {Failures} failures in the last minute",
                        ip, failures);
                }
            },
            OnTokenValidated = ctx =>
            {
                var userId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
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

// ─── Auth Failure Counter (brute force detection) ────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAuthFailureCounter, MemoryAuthFailureCounter>();

// ─── YARP ─────────────────────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ─── Rate Limiting inteligente: por usuario autenticado o por IP ─────────────
builder.Services.AddRateLimiter(options =>
{
    // Usuarios autenticados: limitar por userId (60/min)
    // Usuarios anónimos:    limitar por IP (30/min — más restrictivo)
    options.AddPolicy("SmartRateLimit", httpContext =>
    {
        var userId = httpContext.User?.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(userId))
        {
            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: $"user:{userId}",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    Window               = TimeSpan.FromMinutes(1),
                    PermitLimit          = 60,
                    SegmentsPerWindow    = 4,
                    QueueLimit           = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        }

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: $"ip:{clientIp}",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window               = TimeSpan.FromMinutes(1),
                PermitLimit          = 30,
                SegmentsPerWindow    = 4,
                QueueLimit           = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });

    options.AddPolicy("CreateOrder", httpContext =>
    {
        var userId = httpContext.User?.FindFirstValue("sub")
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"create-order:{userId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit  = 0
            });
    });

    // Rate limiting estricto por IP — para endpoints sensibles (pagos, admin)
    options.AddPolicy("PerIp", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"per-ip:{clientIp}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = 20,
                QueueLimit  = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var userId = context.HttpContext.User?.FindFirstValue("sub") ?? "anonymous";
        var path   = context.HttpContext.Request.Path;
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogWarning("[SECURITY] Rate limit exceeded for {UserId} on {Path}", userId, path);

        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title      = "Too Many Requests",
            status     = 429,
            detail     = "Rate limit exceeded. Please try again in 60 seconds.",
            retryAfter = 60
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

// ─── Kestrel: TLS mínimo + límite de body ────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });

    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024; // 1 MB
});

var app = builder.Build();

// ─── Seed de usuarios de prueba ───────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<GatewayIdentityDbContext>()
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

// ─── Cabeceras HTTP de seguridad ─────────────────────────────────────────────
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    if (!context.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment())
    {
        headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    headers.Append("X-Frame-Options", "DENY");
    headers.Append("X-Content-Type-Options", "nosniff");
    headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
    headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), interest-cohort=()");

    headers.Remove("Server");
    headers.Remove("X-Powered-By");
    headers.Remove("X-AspNet-Version");
    headers.Remove("X-AspNetMvc-Version");

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapAccountEndpoints();
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
