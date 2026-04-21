using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Infrastructure.Audit;
using Orders.API.Sagas;

namespace Orders.API.Infrastructure.Persistence;

/// <summary>
/// DbContext exclusivo de Orders.API.
/// Ningún otro servicio tiene referencia a este DbContext.
/// Schema: "orders" — diferencia tablas en entornos con schemas compartidos (dev LocalDB).
/// </summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order>          Orders     => Set<Order>();
    public DbSet<AuditLogEntry>  AuditLogs  => Set<AuditLogEntry>();

    // OrderSagaState gestionado por MassTransit, disponible para consultas
    public DbSet<OrderSagaState> SagaStates => Set<OrderSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
    }
}
