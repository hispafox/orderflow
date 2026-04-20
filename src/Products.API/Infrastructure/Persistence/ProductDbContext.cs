using Microsoft.EntityFrameworkCore;
using Products.API.Domain;

namespace Products.API.Infrastructure.Persistence;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product>  Products   => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("products");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name)       .HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price)      .HasPrecision(18, 2).IsRequired();
            entity.Property(p => p.Currency)   .HasMaxLength(3).IsRequired();
            entity.Property(p => p.Stock)      .IsRequired();
            entity.Property(p => p.IsActive)   .IsRequired();
            entity.Property(p => p.CreatedAt)  .IsRequired();
            entity.Property(p => p.UpdatedAt)  .IsRequired();
            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.Name);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
        });
    }
}
