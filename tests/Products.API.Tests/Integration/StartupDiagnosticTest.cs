using System.Net;
using FluentAssertions;
using Products.API.Tests.Shared.Fixtures;

namespace Products.API.Tests.Integration;

[Collection(ProductsApiCollection.Name)]
public class StartupDiagnosticTest
{
    private readonly HttpClient _client;

    public StartupDiagnosticTest(ProductsApiFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Factory_ShouldStartAndReachHealthEndpoint()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
