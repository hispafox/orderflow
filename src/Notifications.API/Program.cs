using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.API.Consumers;
using Notifications.API.Infrastructure;
using Notifications.API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

// ─── MassTransit + RabbitMQ ───────────────────────────────────────────────────
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
