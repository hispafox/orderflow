using Microsoft.EntityFrameworkCore;
using Products.API.Domain;

namespace Products.API.Infrastructure.Persistence;

/// <summary>
/// DbContext exclusivo de Products.API.
/// Ningún otro servicio tiene referencia a este DbContext.
/// Schema: "products".
/// </summary>
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product>  Products   => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("products");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductDbContext).Assembly);
    }
}
