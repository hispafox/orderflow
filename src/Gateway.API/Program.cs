using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ─── ServiceDefaults (OpenTelemetry, health checks base) ─────────────────────
builder.AddServiceDefaults();

// ─── Serilog ─────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, svc, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(svc)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "gateway-api")
    .Enrich.WithMachineName());

// ─── YARP ─────────────────────────────────────────────────────────────────────
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ─── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Política general: 100 peticiones por minuto por IP (ventana deslizante)
    options.AddSlidingWindowLimiter("PerIp", config =>
    {
        config.Window              = TimeSpan.FromMinutes(1);
        config.PermitLimit         = 100;
        config.SegmentsPerWindow   = 4;
        config.QueueLimit          = 20;
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Política estricta para creación de pedidos: 10 por minuto
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

// ─── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// 1. CORS — antes del rate limiter para que los preflight OPTIONS pasen
app.UseCors("TechShopFrontend");

// 2. CorrelationId inline
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? System.Diagnostics.Activity.Current?.TraceId.ToString()
        ?? Guid.NewGuid().ToString("N");

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

// 3. Serilog request logging
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

// 4. Rate Limiting — ANTES de MapReverseProxy
app.UseRateLimiter();

// 5. YARP
app.MapReverseProxy();

// 6. Health y alive del propio gateway
app.MapDefaultEndpoints();

app.Run();
