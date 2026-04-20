using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Products.API.API.Middleware;
using Products.API.Domain.Interfaces;
using Products.API.Features;
using Products.API.Infrastructure.Persistence;
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

    // ─── Aspire ServiceDefaults ───────────────────────────────────────────────
    builder.AddServiceDefaults();

    // ─── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, svc, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(svc)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("ServiceName", "products-api"));

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
    builder.Services.AddDbContext<ProductDbContext>((sp, options) =>
        options.UseSqlServer(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("sqlserver"),
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "products")));

    builder.Services.AddScoped<IProductRepository, SqlProductRepository>();

    // ─── MassTransit + RabbitMQ ───────────────────────────────────────────────
    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumers(typeof(Program).Assembly);

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
