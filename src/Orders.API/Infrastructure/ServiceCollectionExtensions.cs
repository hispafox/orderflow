using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Interfaces;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("sqlserver"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders")));

        services.AddScoped<IOrderRepository, SqlOrderRepository>();

        return services;
    }
}
