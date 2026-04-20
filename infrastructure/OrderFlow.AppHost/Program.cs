var builder = DistributedApplication.CreateBuilder(args);

// SQL Server local (LocalDB) — sin Docker
var sqlServer = builder.AddConnectionString("sqlserver");

// RabbitMQ local — sin Docker
var messaging = builder.AddConnectionString("messaging");

// Products.API
var products = builder
    .AddProject<Projects.Products_API>("products-api")
    .WithReference(sqlServer)
    .WithReference(messaging);

// Notifications.API — consumers de OrderCreated, OrderConfirmed, OrderFailed
var notifications = builder
    .AddProject<Projects.Notifications_API>("notifications-api")
    .WithReference(sqlServer)
    .WithReference(messaging);

// Payments.API — consumer de ProcessPayment
var payments = builder
    .AddProject<Projects.Payments_API>("payments-api")
    .WithReference(sqlServer)
    .WithReference(messaging);

// Orders.API — Saga orchestrator, referencia a Products para Service Discovery
var orders = builder.AddProject<Projects.Orders_API>("orders-api")
    .WithReference(sqlServer)
    .WithReference(messaging)
    .WithReference(products);

// Gateway.API — punto de entrada único para todo el tráfico externo
builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithReference(orders)
    .WithReference(products)
    .WithReference(payments)
    .WithReference(notifications);

builder.Build().Run();
