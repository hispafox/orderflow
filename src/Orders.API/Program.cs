using Azure.Identity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Orders.API.API.Middleware;
using Orders.API.Application.Settings;
using Orders.API.Infrastructure.Telemetry;
using RabbitMQ.Client;
using Serilog;
using Serilog.Events;

// ─── Bootstrap logger ──────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Aspire ServiceDefaults ───────────────────────────────────────────────
    builder.AddServiceDefaults();

    // ─── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.With<OpenTelemetryEnricher>());

    // ─── OpenTelemetry — extender lo que ServiceDefaults ya configuró ─────────
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("Orders.API"))
        .WithMetrics(metrics => metrics
            .AddMeter("Orders.API"));

    builder.Services.AddSingleton<OrdersMetrics>();

    // ─── Validación del DI container en desarrollo ────────────────────────────
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = builder.Environment.IsDevelopment();
        options.ValidateOnBuild = builder.Environment.IsDevelopment();
    });

    // ─── Patrón Options con validación en startup ─────────────────────────────
    builder.Services
        .AddOptions<OrdersSettings>()
        .BindConfiguration(OrdersSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services
        .AddSingleton<IValidateOptions<OrdersSettings>, OrdersSettingsValidator>();

    // ─── RabbitMQ connection (singleton) para Health Check ───────────────────
    builder.Services.AddSingleton<IConnection>(_ =>
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(builder.Configuration.GetConnectionString("messaging")!)
        };
        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    });

    // ─── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString: builder.Configuration.GetConnectionString("sqlserver")!,
            healthQuery: "SELECT 1",
            name: "orders-db",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready", "db"])
        .AddRabbitMQ(
            name: "rabbitmq",
            failureStatus: HealthStatus.Degraded,
            tags: ["ready", "messaging"]);

    // ─── Servicios ────────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // ─── Azure Key Vault en producción ────────────────────────────────────────
    if (!builder.Environment.IsDevelopment())
    {
        var keyVaultUri = builder.Configuration["KeyVaultUri"];
        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }

    var app = builder.Build();

    // ─── Pipeline ─────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseHttpsRedirection();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent",
                httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        };

        options.GetLevel = (ctx, elapsed, ex) =>
        {
            if (ex is not null) return LogEventLevel.Error;
            if (ctx.Response.StatusCode >= 500) return LogEventLevel.Error;
            if (ctx.Response.StatusCode >= 400) return LogEventLevel.Warning;
            if (ctx.Request.Path.StartsWithSegments("/health") ||
                ctx.Request.Path.StartsWithSegments("/alive"))
                return LogEventLevel.Verbose;
            return LogEventLevel.Information;
        };
    });

    app.UseAuthorization();
    app.MapControllers();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapDefaultEndpoints(); // /health y /alive

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}
