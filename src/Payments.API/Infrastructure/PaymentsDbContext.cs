using Microsoft.EntityFrameworkCore;
using Payments.API.Domain;

namespace Payments.API.Infrastructure;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(p => p.Id);

            entity.HasIndex(p => p.OrderId)
                  .HasDatabaseName("IX_Payments_OrderId");

            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.Currency).HasMaxLength(3);
            entity.Property(p => p.TransactionId).HasMaxLength(100);
            entity.Property(p => p.FailureReason).HasMaxLength(500);
            entity.Property(p => p.Status).HasConversion<string>();
        });
    }
}
