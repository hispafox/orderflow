using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Orders.API.Domain.Interfaces;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<OrderDbContext>(options =>
        {
            if (environment.IsProduction())
            {
                // Sin password — Managed Identity del App Service se autentica automáticamente
                options.UseSqlServer(
                    configuration.GetConnectionString("sqlserver")
                    ?? "Server=tcp:orderflow-sql.database.windows.net,1433;Database=OrdersDb;" +
                       "Authentication=Active Directory Default;TrustServerCertificate=False;",
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders"));
            }
            else
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("sqlserver"),
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders"));
            }
        });

        services.AddScoped<IOrderRepository, SqlOrderRepository>();

        return services;
    }
}
