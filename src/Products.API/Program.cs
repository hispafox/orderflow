using Azure.Identity;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Products.API.API.Middleware;
using Products.API.Domain.Interfaces;
using Products.API.Features;
using Products.API.Infrastructure.Persistence;
using Products.API.Infrastructure.Persistence.Interceptors;
using Products.API.Infrastructure.Persistence.Seeds;
using Products.API.Infrastructure.Settings;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Azure Key Vault (solo en producción) ─────────────────────────────────
    if (builder.Environment.IsProduction())
    {
        var keyVaultUri = new Uri(
            builder.Configuration["KeyVault:Uri"]
            ?? throw new InvalidOperationException("KeyVault:Uri is required in Production"));

        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }

    // ─── Aspire ServiceDefaults ───────────────────────────────────────────────
    builder.AddServiceDefaults();

    // ─── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, svc, cfg) =>
    {
        cfg.ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(svc)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("ServiceName", "products-api");

        var otlp = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrEmpty(otlp))
        {
            cfg.WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = otlp;
                o.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                o.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = "products-api" };
            });
        }
    });

    // ─── Options ──────────────────────────────────────────────────────────────
    builder.Services
        .AddOptions<ProductsSettings>()
        .BindConfiguration(ProductsSettings.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // ─── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionStringFactory: sp =>
                sp.GetRequiredService<IConfiguration>().GetConnectionString("sqlserver")!,
            name: "products-db",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready", "db"]);

    // ─── Infraestructura ──────────────────────────────────────────────────────
    builder.Services.AddScoped<AuditInterceptor>();

    builder.Services.AddDbContext<ProductDbContext>((sp, options) =>
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("sqlserver")!,
            sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                sql.CommandTimeout(60);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "products");
            })
        .AddInterceptors(sp.GetRequiredService<AuditInterceptor>());

        if (builder.Environment.IsDevelopment())
            options.EnableSensitiveDataLogging().EnableDetailedErrors();
    });

    builder.Services.AddScoped<IProductRepository, SqlProductRepository>();

    // ─── MassTransit: RabbitMQ (dev) / Azure Service Bus (prod) ─────────────────
    if (builder.Environment.IsProduction())
    {
        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(Program).Assembly);

            x.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(
                    new Uri("sb://orderflow.servicebus.windows.net"),
                    host => { host.TokenCredential = new DefaultAzureCredential(); });

                cfg.UseMessageRetry(r => r.Exponential(
                    5, TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });
    }
    else
    {
        var messagingTransport = builder.Configuration["Messaging:Transport"] ?? "RabbitMQ";

        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(Program).Assembly);

            if (messagingTransport == "InMemory")
            {
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
            else
            {
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
            }
        });
    }

    // ─── Authorization ───────────────────────────────────────────────────────
    builder.Services.AddAuthorization();

    // ─── FluentValidation ────────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // ─── OpenAPI ─────────────────────────────────────────────────────────────
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title       = "Products API";
            document.Info.Description = "Catálogo de productos y gestión de stock — OrderFlow";
            return Task.CompletedTask;
        });
    });

    var app = builder.Build();

    // ─── Pipeline ─────────────────────────────────────────────────────────────
    app.UseMiddleware<ProductsExceptionMiddleware>(); // ← PRIMERO

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (ctx, _, ex) =>
        {
            if (ex is not null || ctx.Response.StatusCode >= 500) return LogEventLevel.Error;
            if (ctx.Response.StatusCode >= 400) return LogEventLevel.Warning;
            if (ctx.Request.Path.StartsWithSegments("/health")) return LogEventLevel.Verbose;
            return LogEventLevel.Information;
        };
    });

    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Products API");
            options.WithTheme(ScalarTheme.Moon);
            options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();

    app.MapProductEndpoints();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapDefaultEndpoints();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.MigrateAsync();
        await CategorySeed.InitializeAsync(db);
        await ProductSeed.InitializeAsync(db);
    }
    else
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            app.Logger.LogCritical(
                "There are {Count} pending migrations: {Migrations}. Service will start but health check will be Unhealthy.",
                pending.Count(), string.Join(", ", pending));
        }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Products.API startup failed");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
