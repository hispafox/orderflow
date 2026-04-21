using Microsoft.EntityFrameworkCore;
using Products.API.Domain;
using Products.API.Infrastructure.Persistence;

namespace Products.API.Infrastructure.Persistence.Seeds;

public static class CategorySeed
{
    public static readonly Guid LaptopsId     = Guid.Parse("c1b2a3d4-e5f6-0123-4567-89abcdef0123");
    public static readonly Guid SmartphonesId = Guid.Parse("c2b3a4d5-e6f7-1234-5678-9abcdef01234");
    public static readonly Guid TabletsId     = Guid.Parse("c3b4a5d6-e7f8-2345-6789-abcdef012345");
    public static readonly Guid AccessoriesId = Guid.Parse("c4b5a6d7-e8f9-3456-789a-bcdef0123456");

    public static async Task InitializeAsync(ProductDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        context.Categories.AddRange(
            Category.ForSeed(LaptopsId,     "Portátiles"),
            Category.ForSeed(SmartphonesId, "Smartphones"),
            Category.ForSeed(TabletsId,     "Tablets"),
            Category.ForSeed(AccessoriesId, "Accesorios"));

        await context.SaveChangesAsync();
    }
}
