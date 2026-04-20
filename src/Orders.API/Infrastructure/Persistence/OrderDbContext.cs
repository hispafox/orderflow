using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Domain.ValueObjects;
using Orders.API.Infrastructure.Audit;
using Orders.API.Sagas;

namespace Orders.API.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order>         Orders    => Set<Order>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders", schema: "orders");
            entity.HasKey(o => o.Id);

            entity.Property(o => o.CustomerId).IsRequired();
            entity.Property(o => o.CreatedAt).IsRequired();
            entity.Property(o => o.ConfirmedAt);
            entity.Property(o => o.CancelledAt);
            entity.Property(o => o.CancellationReason).HasMaxLength(500);

            entity.Property(o => o.Status)
                .HasConversion(
                    s => s.Value,
                    v => OrderStatus.FromString(v))
                .HasColumnName("Status")
                .HasMaxLength(20)
                .IsRequired();

            entity.OwnsOne(o => o.ShippingAddress, addr =>
            {
                addr.Property(a => a.Street) .HasColumnName("ShippingStreet") .HasMaxLength(200).IsRequired();
                addr.Property(a => a.City)   .HasColumnName("ShippingCity")   .HasMaxLength(100).IsRequired();
                addr.Property(a => a.ZipCode).HasColumnName("ShippingZipCode").HasMaxLength(20) .IsRequired();
                addr.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(2)  .IsRequired();
                addr.Property(a => a.State)  .HasColumnName("ShippingState")  .HasMaxLength(100);
            });

            // EF Core usa el campo privado _lines a través de la propiedad Lines (IReadOnlyList)
            entity.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(o => o.Lines).HasField("_lines");
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("OrderLines", schema: "orders");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.ProductId).IsRequired();
            entity.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(l => l.Quantity).IsRequired();

            entity.OwnsOne(l => l.UnitPrice, money =>
            {
                money.Property(m => m.Amount)  .HasColumnName("UnitPriceAmount")  .HasPrecision(18, 2).IsRequired();
                money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3)     .IsRequired();
            });
        });

        // ─── OrderSagaState — persistencia de la Saga ────────────────────────────────
        modelBuilder.Entity<OrderSagaState>(entity =>
        {
            entity.ToTable("OrderSagaState", schema: "orders");
            entity.HasKey(s => s.CorrelationId);

            entity.Property(s => s.CurrentState).HasMaxLength(64);
            entity.Property(s => s.CustomerEmail).HasMaxLength(320);
            entity.Property(s => s.Currency).HasMaxLength(3);
            entity.Property(s => s.FailureReason).HasMaxLength(500);
            entity.Property(s => s.Amount).HasPrecision(18, 2);

            entity.Property(s => s.RowVersion)
                  .IsRowVersion()
                  .IsConcurrencyToken();

            entity.Property(s => s.ReservationTimeoutTokenId)
                  .IsRequired(false);
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogs", "orders");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.UserId)
                  .HasDatabaseName("IX_AuditLogs_UserId");
            entity.HasIndex(e => e.Timestamp)
                  .HasDatabaseName("IX_AuditLogs_Timestamp");
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId })
                  .HasDatabaseName("IX_AuditLogs_Resource");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserEmail).HasMaxLength(320);
            entity.Property(e => e.ResourceType).HasMaxLength(100);
            entity.Property(e => e.ResourceId).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
        });
    }
}
