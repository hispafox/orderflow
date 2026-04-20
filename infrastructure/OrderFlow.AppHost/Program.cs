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

// Notifications.API — consumer de OrderCreated
var notifications = builder
    .AddProject<Projects.Notifications_API>("notifications-api")
    .WithReference(sqlServer)
    .WithReference(messaging);

// Orders.API — referencia a Products para Service Discovery
builder.AddProject<Projects.Orders_API>("orders-api")
    .WithReference(sqlServer)
    .WithReference(messaging)
    .WithReference(products);

builder.Build().Run();
