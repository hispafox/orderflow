using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.API.Domain.Entities;

namespace Orders.API.Infrastructure.Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines", "orders");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.ProductId).IsRequired();

        // Desnormalización histórica: nombre y precio al momento de la compra
        builder.Property(l => l.ProductName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(l => l.ProductSku)
               .HasMaxLength(50);

        // UnitPrice como Owned Entity (precio al momento de la compra)
        builder.OwnsOne(l => l.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("UnitPriceAmount")
                 .HasPrecision(18, 2)
                 .IsRequired();
            money.Property(m => m.Currency)
                 .HasColumnName("UnitPriceCurrency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        builder.Property(l => l.Quantity).IsRequired();

        builder.HasIndex("OrderId")
               .HasDatabaseName("IX_OrderLines_OrderId");
    }
}
