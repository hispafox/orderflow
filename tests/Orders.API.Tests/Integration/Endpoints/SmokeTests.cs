using System.Net;
using FluentAssertions;
using Orders.API.Tests.Shared.Fixtures;

namespace Orders.API.Tests.Integration.Endpoints;

[Trait("Category", "Integration")]
[Collection("OrdersApiCollection")]
public class SmokeTests
{
    private readonly HttpClient _client;

    public SmokeTests(OrdersApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LivenessEndpoint_ShouldReturn200()
    {
        var response = await _client.GetAsync("/alive");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldRespond()
    {
        var response = await _client.GetAsync("/health");
        // Healthy (200) o Degraded (200) — solo verificamos que responde sin crash
        ((int)response.StatusCode).Should().BeOneOf(200, 503);
    }

    [Fact]
    public async Task OpenApiSpec_ShouldBeAccessible()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("openapi");
    }
}
