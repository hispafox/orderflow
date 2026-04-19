var builder = DistributedApplication.CreateBuilder(args);

// SQL Server local (LocalDB) — sin Docker
var sqlServer = builder.AddConnectionString("sqlserver");

// RabbitMQ local — sin Docker
var messaging = builder.AddConnectionString("messaging");

// Servicio Orders.API
builder.AddProject<Projects.Orders_API>("orders-api")
    .WithReference(sqlServer)
    .WithReference(messaging);

builder.Build().Run();
