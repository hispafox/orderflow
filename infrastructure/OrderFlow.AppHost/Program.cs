var builder = DistributedApplication.CreateBuilder(args);

// Recursos externos — sin Docker
// Las connection strings se configuran en appsettings.json o User Secrets
var sqlServer = builder.AddConnectionString("sqlserver");
var messaging = builder.AddConnectionString("messaging");

// Los servicios se añaden módulo a módulo:
// M2.1 → orders-api
// M3.3 → products-api
// M4.2 → notifications-api
// M4.3 → payments-api
// M4.4 → gateway-api

builder.Build().Run();
