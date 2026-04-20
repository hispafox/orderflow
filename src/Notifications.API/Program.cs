using Azure.Identity;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.API.Consumers;
using Notifications.API.Infrastructure;
using Notifications.API.Services;
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

// ─── ServiceDefaults (OpenTelemetry, health checks base) ─────────────────────
builder.AddServiceDefaults();

// ─── Serilog ─────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName());

// ─── NotificationDbContext ────────────────────────────────────────────────────
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("sqlserver"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications")));

// ─── Email service ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, FakeEmailService>();

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
    .AddDbContextCheck<NotificationDbContext>(
        name: "notifications-db",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

// ─── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapDefaultEndpoints();

app.Run();
