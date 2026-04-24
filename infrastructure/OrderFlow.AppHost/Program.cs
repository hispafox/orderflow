using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// RabbitMQ local — sin Docker
var messaging = builder.AddConnectionString("messaging");

// Identity DB para Gateway.API
var identityDb = builder.AddConnectionString("IdentityDb");

// Connection strings por servicio (Database per Service) — leídas de appsettings.json
var ordersDbConn        = builder.Configuration.GetConnectionString("ordersDb");
var productsDbConn      = builder.Configuration.GetConnectionString("productsDb");
var paymentsDbConn      = builder.Configuration.GetConnectionString("paymentsDb");
var notificationsDbConn = builder.Configuration.GetConnectionString("notificationsDb");

// Products.API — puerto HTTPS fijo en 7200 para que la SPA (Vite proxy) pueda consumirlo
var products = builder
    .AddProject<Projects.Products_API>("products-api")
    .WithEndpoint("https", e => e.Port = 7200)
    .WithEnvironment("ConnectionStrings__sqlserver", productsDbConn)
    .WithReference(messaging);

// Notifications.API
var notifications = builder
    .AddProject<Projects.Notifications_API>("notifications-api")
    .WithEnvironment("ConnectionStrings__sqlserver", notificationsDbConn)
    .WithReference(messaging);

// Payments.API
var payments = builder
    .AddProject<Projects.Payments_API>("payments-api")
    .WithEnvironment("ConnectionStrings__sqlserver", paymentsDbConn)
    .WithReference(messaging);

// Orders.API — puerto HTTPS fijo en 7153 para que la SPA (Vite proxy) pueda consumirlo
var orders = builder.AddProject<Projects.Orders_API>("orders-api")
    .WithEndpoint("https", e => e.Port = 7153)
    .WithEnvironment("ConnectionStrings__sqlserver", ordersDbConn)
    .WithReference(messaging)
    .WithReference(products);

// Gateway.API
builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithReference(orders)
    .WithReference(products)
    .WithReference(payments)
    .WithReference(notifications)
    .WithReference(identityDb);

builder.Build().Run();
