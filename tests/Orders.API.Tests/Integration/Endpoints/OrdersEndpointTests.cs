using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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
    }

    [Fact]
    public async Task GetAll_ShouldReturn200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<object>>();
        orders.Should().NotBeNull().And.BeEmpty();
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
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = Guid.NewGuid(), quantity = 2 } }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
}
