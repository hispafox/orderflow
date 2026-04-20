using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using FluentValidation;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MediatR;
using Orders.API.Sagas;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Orders.API.Application.Behaviors;
using Orders.API.Infrastructure;
using Orders.API.Infrastructure.Http;
using Orders.API.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Orders.API.API.Middleware;
using Orders.API.Application.Settings;
using Orders.API.Infrastructure.Telemetry;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Timeout;
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
            .AddMeter("Orders.API")
            .AddMeter("MassTransit"));

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
            connectionStringFactory: sp =>
                sp.GetRequiredService<IConfiguration>().GetConnectionString("sqlserver")!,
            healthQuery: "SELECT 1",
            name: "orders-db",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready", "db"])
        .AddRabbitMQ(
            name: "rabbitmq",
            failureStatus: HealthStatus.Degraded,
            tags: ["ready", "messaging"]);

    // ─── Infraestructura ──────────────────────────────────────────────────────────
    builder.Services.AddOrdersInfrastructure(builder.Configuration);

    // ─── HTTP Context (necesario para CorrelationIdDelegatingHandler) ─────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddMemoryCache();

    // ─── DelegatingHandlers ───────────────────────────────────────────────────────
    builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
    builder.Services.AddTransient<LoggingDelegatingHandler>();

    // ─── ProductsClient con DelegatingHandlers + Polly ───────────────────────────
    builder.Services
        .AddHttpClient<ProductsClient>(client =>
        {
            client.BaseAddress = new Uri("http://products-api");
            client.DefaultRequestHeaders.Add("Accept",     "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "Orders.API/1.0");
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddHttpMessageHandler<LoggingDelegatingHandler>()
        .AddResilienceHandler("products-pipeline", (pipeline, context) =>
        {
            var logger = context.ServiceProvider
                .GetRequiredService<ILogger<Program>>();

            pipeline
                .AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<BrokenCircuitException>()
                        .Handle<TimeoutRejectedException>(),
                    FallbackAction = _ =>
                    {
                        logger.LogWarning("Fallback activated for Products.API");
                        return ValueTask.FromResult(
                            Outcome.FromResult(
                                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(10))
                .AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay            = TimeSpan.FromMilliseconds(500),
                    BackoffType      = DelayBackoffType.Exponential,
                    UseJitter        = true,
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Retry {Attempt}/3 for Products.API — {Cause} — delay {DelayMs}ms",
                            args.AttemptNumber + 1,
                            args.Outcome.Exception?.Message
                                ?? args.Outcome.Result?.StatusCode.ToString(),
                            args.RetryDelay.TotalMilliseconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio      = 0.5,
                    SamplingDuration  = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration     = TimeSpan.FromSeconds(30),
                    OnOpened = args =>
                    {
                        logger.LogError(
                            "Circuit OPENED for Products.API! Break: {Sec}s",
                            args.BreakDuration.TotalSeconds);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("Circuit CLOSED — Products.API healthy again");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = _ =>
                    {
                        logger.LogInformation("Circuit HALF-OPEN — testing Products.API...");
                        return ValueTask.CompletedTask;
                    }
                })
                .AddTimeout(TimeSpan.FromSeconds(5));
        });

    // ─── MassTransit + RabbitMQ + Saga ───────────────────────────────────────────
    builder.Services.AddMassTransit(x =>
    {
        x.AddSagaStateMachine<OrderSaga, OrderSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.ExistingDbContext<OrderDbContext>();
            });

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(new Uri(builder.Configuration.GetConnectionString("messaging")!));

            cfg.UseMessageRetry(r => r.Exponential(
                retryLimit:    5,
                minInterval:   TimeSpan.FromSeconds(1),
                maxInterval:   TimeSpan.FromSeconds(30),
                intervalDelta: TimeSpan.FromSeconds(2)));

            cfg.ConfigureEndpoints(context);
        });
    });

    // ─── FluentValidation ────────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // ─── MediatR + Pipeline Behaviors ────────────────────────────────────────
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
    });

    // ─── Controllers + JSON ───────────────────────────────────────────────────
    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy   = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title       = "Orders API";
            document.Info.Description = "API para gestión de pedidos de TechShop / OrderFlow";
            return Task.CompletedTask;
        });
    });

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
    app.UseMiddleware<DomainExceptionMiddleware>(); // ← PRIMERO: captura excepciones de dominio

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
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

// Necesario para WebApplicationFactory en tests de integración
public partial class Program { }
