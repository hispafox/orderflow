using Azure.Identity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Orders.API.Application.Settings;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// ─── Aspire ServiceDefaults ───────────────────────────────────────────────────
builder.AddServiceDefaults();

// ─── Validación del DI container en desarrollo ───────────────────────────────
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = builder.Environment.IsDevelopment();
    options.ValidateOnBuild = builder.Environment.IsDevelopment();
});

// ─── Patrón Options con validación en startup ─────────────────────────────────
builder.Services
    .AddOptions<OrdersSettings>()
    .BindConfiguration(OrdersSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddSingleton<IValidateOptions<OrdersSettings>, OrdersSettingsValidator>();

// ─── RabbitMQ connection (singleton) para Health Check ───────────────────────
builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(builder.Configuration.GetConnectionString("messaging")!)
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// ─── Health Checks ─────────────────────────────────────────────────────────────
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

// ─── Servicios ────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ─── Azure Key Vault en producción (Managed Identity, sin credenciales en código)
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

// ─── Pipeline ─────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapDefaultEndpoints(); // /health y /alive — de ServiceDefaults

app.Run();
