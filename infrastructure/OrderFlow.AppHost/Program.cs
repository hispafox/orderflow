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
builder
    .AddProject<Projects.Payments_API>("payments-api")
    .WithReference(sqlServer)
    .WithReference(messaging);

// Orders.API — Saga orchestrator, referencia a Products para Service Discovery
builder.AddProject<Projects.Orders_API>("orders-api")
    .WithReference(sqlServer)
    .WithReference(messaging)
    .WithReference(products);

builder.Build().Run();
