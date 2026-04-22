using System.Text.Json;
using FluentAssertions;
using OrderFlow.Contracts.Events.Orders;
using Xunit;

namespace Orders.API.Tests.Contract.Pact;

/// <summary>
/// Provider verification: Orders.API al serializar OrderCreated
/// debe incluir todos los campos del contrato definido por el consumer.
/// </summary>
[Trait("Category", "Contract")]
public class OrderCreatedProviderTest
{
    private static readonly string PactFile = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "pacts", "notifications-api-orders-api.json");

    [Fact]
    public void OrderCreated_published_by_Orders_satisfies_consumer_contract()
    {
        if (!File.Exists(PactFile))
        {
            // Ejecuta OrderCreatedConsumerTest primero para generar el pact.
            return;
        }

        // Cargar contrato del consumer
        var pactJson     = File.ReadAllText(PactFile);
        var pact         = JsonDocument.Parse(pactJson);
        var expected     = pact.RootElement
            .GetProperty("messages")[0]
            .GetProperty("contents");

        // Producir un mensaje real tal como lo emite Orders.API
        var orderCreated = new OrderCreated
        {
            OrderId       = Guid.NewGuid(),
            CustomerId    = Guid.NewGuid(),
            CustomerEmail = "john@techshop.es",
            Total         = 999.99m,
            Currency      = "EUR",
            CreatedAt     = DateTime.UtcNow,
            Items         = [new OrderCreatedItem(
                Guid.NewGuid(), "MacBook Pro", 1, 999.99m)],
            ShippingAddress = new OrderCreatedAddress(
                "Gran Vía 28", "Madrid", "28013", "ES")
        };

        var actualJson = JsonSerializer.Serialize(orderCreated, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var actual = JsonDocument.Parse(actualJson).RootElement;

        // Verificar que el mensaje real contiene todos los campos esperados
        foreach (var expectedField in expected.EnumerateObject())
        {
            actual.TryGetProperty(expectedField.Name, out _)
                .Should().BeTrue($"Orders.API debe publicar el campo '{expectedField.Name}' según el contrato");
        }

        // Campos anidados: shippingAddress
        var expectedAddress = expected.GetProperty("shippingAddress");
        var actualAddress   = actual.GetProperty("shippingAddress");
        foreach (var field in expectedAddress.EnumerateObject())
        {
            actualAddress.TryGetProperty(field.Name, out _)
                .Should().BeTrue($"shippingAddress debe contener '{field.Name}'");
        }

        // Items es array → verificar un elemento
        var expectedItem = expected.GetProperty("items")[0];
        var actualItem   = actual.GetProperty("items")[0];
        foreach (var field in expectedItem.EnumerateObject())
        {
            actualItem.TryGetProperty(field.Name, out _)
                .Should().BeTrue($"items[] debe contener '{field.Name}'");
        }
    }
}
