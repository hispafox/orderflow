using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Products.API.Domain;

namespace Products.API.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)       .HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Price)      .HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Currency)   .HasMaxLength(3).IsRequired();
        builder.Property(p => p.Stock)      .IsRequired();
        builder.Property(p => p.IsActive)   .IsRequired();
        builder.Property(p => p.CreatedAt)  .IsRequired();
        builder.Property(p => p.UpdatedAt)  .IsRequired();

        builder.HasIndex(p => p.CategoryId).HasDatabaseName("IX_Products_CategoryId");
        builder.HasIndex(p => p.Name)      .HasDatabaseName("IX_Products_Name");
    }
}
