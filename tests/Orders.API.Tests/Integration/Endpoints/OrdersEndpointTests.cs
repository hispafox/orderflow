using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Orders.API.Tests.Shared;
using Orders.API.Tests.Shared.Fixtures;

namespace Orders.API.Tests.Integration.Endpoints;

[Trait("Category", "Integration")]
[Collection("OrdersApiCollection")]
public class OrdersEndpointTests
{
    private readonly HttpClient _client;

    public OrdersEndpointTests(OrdersApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CustomerToken());
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithEmptyItems()
    {
        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetById_NonExistingId_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ValidRequest_ShouldReturn201()
    {
        var request = new
        {
            customerEmail = "test@example.com",
            items = new[]
            {
                new
                {
                    productId   = Guid.NewGuid(),
                    productName = "MacBook Pro 16",
                    quantity    = 1,
                    unitPrice   = 1999.99m,
                    currency    = "EUR"
                }
            },
            shippingAddress = new
            {
                street  = "Gran Vía 28",
                city    = "Madrid",
                zipCode = "28013",
                country = "ES"
            }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
}
