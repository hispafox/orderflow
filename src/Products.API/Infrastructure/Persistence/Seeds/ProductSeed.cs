using Microsoft.EntityFrameworkCore;
using Products.API.Domain;
using Products.API.Infrastructure.Persistence;

namespace Products.API.Infrastructure.Persistence.Seeds;

public static class ProductSeed
{
    public static readonly Guid MacBookProId = Guid.Parse("a4b5c6d7-e8f9-0123-4567-89abcdef0123");
    public static readonly Guid IpadProId    = Guid.Parse("b5c6d7e8-f901-2345-6789-abcdef012345");

    public static async Task InitializeAsync(ProductDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        context.Products.AddRange(
            Product.ForSeed(MacBookProId, "MacBook Pro 16\" M4", "Apple Silicon M4 Pro, 24GB RAM",
                3999.99m, "EUR", 25, CategorySeed.LaptopsId),
            Product.ForSeed(IpadProId, "iPad Pro 13\" M4", "Apple M4, 256GB",
                1299.99m, "EUR", 50, CategorySeed.TabletsId));

        await context.SaveChangesAsync();
    }
}
