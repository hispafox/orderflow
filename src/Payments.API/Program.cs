using Azure.Identity;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payments.API.Consumers;
using Payments.API.Infrastructure;
using Payments.API.Infrastructure.Interceptors;
using Payments.API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Azure Key Vault (solo en producción) ─────────────────────────────────────
if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri(
        builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault:Uri is required in Production"));

    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

builder.AddServiceDefaults();

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName());

// ─── PaymentsDbContext ────────────────────────────────────────────────────────
builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddDbContext<PaymentsDbContext>((sp, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("sqlserver")!,
        sql =>
        {
            sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            sql.CommandTimeout(60);
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "payments");
        })
    .AddInterceptors(sp.GetRequiredService<AuditInterceptor>());

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
});

// ─── Payment Gateway ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();

// ─── MassTransit: RabbitMQ (dev) / Azure Service Bus (prod) ──────────────────
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
}

// ─── OpenTelemetry con métricas de MassTransit ───────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m.AddMeter("MassTransit"));

// ─── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentsDbContext>(
        name: "payments-db",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    await db.Database.MigrateAsync();
}
else
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    var pending = await db.Database.GetPendingMigrationsAsync();
    if (pending.Any())
        app.Logger.LogCritical(
            "There are {Count} pending migrations: {Migrations}.",
            pending.Count(), string.Join(", ", pending));
}

app.Run();
