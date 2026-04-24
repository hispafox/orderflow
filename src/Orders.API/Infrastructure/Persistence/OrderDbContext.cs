using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Infrastructure.Audit;
using Orders.API.Infrastructure.Messaging;
using Orders.API.Infrastructure.Persistence.ReadModel;
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

    // Read Model — alimentado por proyectores, consultado por Dapper
    public DbSet<OrderSummary> OrderSummaries => Set<OrderSummary>();

    // Historial persistente de eventos pasando por MassTransit (demo).
    // Poblado por los observers en Infrastructure/Messaging. Lectura via Dapper.
    public DbSet<DemoEventLog> DemoEventLog => Set<DemoEventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);

        // MassTransit Outbox tables — necesario para que ef migrations las detecte
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
    }
}
