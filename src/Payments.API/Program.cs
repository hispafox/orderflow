using MassTransit;
using Microsoft.EntityFrameworkCore;
using Payments.API.Consumers;
using Payments.API.Infrastructure;
using Payments.API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName());

// ─── PaymentsDbContext ────────────────────────────────────────────────────────
builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("sqlserver"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "payments")));

// ─── Payment Gateway ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentGateway, FakePaymentGateway>();

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
    .AddDbContextCheck<PaymentsDbContext>(
        name: "payments-db",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapDefaultEndpoints();

app.Run();
