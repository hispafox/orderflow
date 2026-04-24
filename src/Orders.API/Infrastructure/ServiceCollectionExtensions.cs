using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Orders.API.Domain.Interfaces;
using Orders.API.Infrastructure.Persistence;
using Orders.API.Infrastructure.Persistence.Interceptors;

namespace Orders.API.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            var connectionString = environment.IsProduction()
                ? configuration.GetConnectionString("sqlserver")
                  ?? "Server=tcp:orderflow-sql.database.windows.net,1433;Database=OrdersDb;" +
                     "Authentication=Active Directory Default;TrustServerCertificate=False;"
                : configuration.GetConnectionString("sqlserver")!;

            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sql.CommandTimeout(60);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders");
            })
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>());

            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();

                // Tolerar pequeñas divergencias entre el snapshot y el modelo en dev
                // (ej. diffs del schema de MassTransit Outbox al cambiar de versión).
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        services.AddScoped<IOrderRepository, SqlOrderRepository>();

        return services;
    }
}
