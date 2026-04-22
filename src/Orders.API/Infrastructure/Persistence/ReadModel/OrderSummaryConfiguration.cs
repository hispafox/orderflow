using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.API.Infrastructure.Persistence.ReadModel;

public class OrderSummaryConfiguration : IEntityTypeConfiguration<OrderSummary>
{
    public void Configure(EntityTypeBuilder<OrderSummary> builder)
    {
        builder.ToTable("OrderSummaries", "orders");
        builder.HasKey(s => s.OrderId);
        builder.Property(s => s.OrderId).ValueGeneratedNever();

        builder.Property(s => s.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.FirstItemName).HasMaxLength(200);
        builder.Property(s => s.ShippingCity).HasMaxLength(100);
        builder.Property(s => s.CancellationReason).HasMaxLength(500);

        builder.HasIndex(s => s.CustomerId).HasDatabaseName("IX_OrderSummaries_CustomerId");
        builder.HasIndex(s => s.Status).HasDatabaseName("IX_OrderSummaries_Status");
        builder.HasIndex(s => s.CreatedAt).HasDatabaseName("IX_OrderSummaries_CreatedAt");
        builder.HasIndex(s => new { s.CustomerId, s.Status })
               .HasDatabaseName("IX_OrderSummaries_CustomerId_Status");
    }
}
