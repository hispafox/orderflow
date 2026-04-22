using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Infrastructure.Persistence.Seeds;

/// <summary>
/// Datos de prueba para el entorno de desarrollo.
/// GUIDs hardcoded para que los tests de integración puedan referenciarlos.
/// Idempotente — se puede ejecutar múltiples veces sin duplicar datos.
/// </summary>
public static class OrderSeed
{
    public static readonly Guid JohnCustomerId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    public static readonly Guid SeedOrder1Id   = Guid.Parse("7b3a2c1d-e4f5-6789-0abc-def012345678");
    public static readonly Guid SeedProductId  = Guid.Parse("a4b5c6d7-e8f9-0123-4567-89abcdef0123");

    public static async Task InitializeAsync(OrderDbContext context)
    {
        if (await context.Orders.AnyAsync()) return;

        var address = new Address("Gran Vía 28", "Madrid", "28013", "ES");
        var items = new List<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)>
        {
            (SeedProductId, "MacBook Pro 16\" M4", 1, new Money(3999.99m, "EUR"))
        };

        var order = Order.Create(JohnCustomerId, items, address);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }
}
