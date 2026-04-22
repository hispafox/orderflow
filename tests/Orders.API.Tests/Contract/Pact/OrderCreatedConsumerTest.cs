using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Orders.API.Tests.Contract.Pact;

/// <summary>
/// Consumer contract (Notifications.API perspective).
/// Define qué campos/tipos espera recibir en el evento OrderCreated
/// y escribe el pact file que luego verifica el Provider test.
/// </summary>
[Trait("Category", "Contract")]
public class OrderCreatedConsumerTest
{
    private static readonly string PactDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "pacts");

    private static readonly string PactFile = Path.Combine(PactDir, "notifications-api-orders-api.json");

    [Fact]
    public void OrderCreated_contract_expected_by_notifications_api()
    {
        Directory.CreateDirectory(PactDir);

        var contract = new
        {
            consumer = new { name = "notifications-api" },
            provider = new { name = "orders-api" },
            messages = new object[]
            {
                new
                {
                    description = "an OrderCreated event",
                    providerStates = new[] { new { name = "a new order was created" } },
                    metadata = new { contentType = "application/json" },
                    contents = new
                    {
                        // Shape esperado por el consumer
                        orderId       = "<guid>",
                        customerId    = "<guid>",
                        customerEmail = "<string>",
                        total         = "<decimal>",
                        currency      = "<string-3>",
                        createdAt     = "<datetime>",
                        items         = new[]
                        {
                            new
                            {
                                productId   = "<guid>",
                                productName = "<string>",
                                quantity    = "<int>",
                                unitPrice   = "<decimal>"
                            }
                        },
                        shippingAddress = new
                        {
                            street  = "<string>",
                            city    = "<string>",
                            zipCode = "<string>",
                            country = "<string-2>"
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(contract, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(PactFile, json);

        File.Exists(PactFile).Should().BeTrue();
    }
}
