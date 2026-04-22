using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.API.Domain.Entities;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.CustomerId).IsRequired();

        builder.Property(o => o.Status)
               .HasConversion(
                   s => s.Value,
                   v => OrderStatus.FromString(v))
               .HasColumnName("Status")
               .HasMaxLength(20)
               .IsRequired();

        // Money (Total) como Owned Entity — columnas TotalAmount y TotalCurrency
        builder.OwnsOne(o => o.Total, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("TotalAmount")
                 .HasPrecision(18, 2)
                 .IsRequired();
            money.Property(m => m.Currency)
                 .HasColumnName("TotalCurrency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // Address como Owned Entity
        builder.OwnsOne(o => o.ShippingAddress, addr =>
        {
            addr.Property(a => a.Street) .HasColumnName("ShippingStreet") .HasMaxLength(200).IsRequired();
            addr.Property(a => a.City)   .HasColumnName("ShippingCity")   .HasMaxLength(100).IsRequired();
            addr.Property(a => a.ZipCode).HasColumnName("ShippingZipCode").HasMaxLength(20) .IsRequired();
            addr.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(2)  .IsRequired();
            addr.Property(a => a.State)  .HasColumnName("ShippingState")  .HasMaxLength(100);
        });

        // Lines collection via private field _lines
        builder.HasMany(o => o.Lines)
               .WithOne()
               .HasForeignKey("OrderId")
               .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Lines).HasField("_lines");

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.ConfirmedAt);
        builder.Property(o => o.CancelledAt);
        builder.Property(o => o.CancellationReason).HasMaxLength(500);

        // Soft delete — el filtro global excluye automáticamente los eliminados
        builder.Property(o => o.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(o => !o.IsDeleted);

        // Índices
        builder.HasIndex(o => o.CustomerId)
               .HasDatabaseName("IX_Orders_CustomerId");

        builder.HasIndex(o => o.Status)
               .HasDatabaseName("IX_Orders_Status");

        builder.HasIndex(o => new { o.CustomerId, o.Status })
               .HasDatabaseName("IX_Orders_CustomerId_Status");

        builder.HasIndex(o => o.CreatedAt)
               .HasDatabaseName("IX_Orders_CreatedAt");

        // Concurrencia optimista
        builder.Property<byte[]>("RowVersion")
               .IsRowVersion()
               .IsConcurrencyToken();
    }
}
