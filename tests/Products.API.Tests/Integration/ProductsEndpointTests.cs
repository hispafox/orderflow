using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Products.API.Features.CreateProduct;
using Products.API.Features.GetProduct;
using Products.API.Features.ListProducts;
using Products.API.Tests.Shared.Fixtures;

namespace Products.API.Tests.Integration;

[Collection(ProductsApiCollection.Name)]
[Trait("Category", "Integration")]
public class ProductsEndpointTests
{
    private readonly HttpClient _client;

    public ProductsEndpointTests(ProductsApiFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task ListProducts_Returns200WithPagedResult()
    {
        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedProductResult>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProduct_NonExisting_Returns404()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_Returns201WithLocation()
    {
        var request = new CreateProductRequest
        {
            Name         = "MacBook Pro 16\" M4",
            Description  = "Portátil de alto rendimiento",
            Price        = 3999.99m,
            Currency     = "EUR",
            InitialStock = 50,
            CategoryId   = Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/products/");

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Name.Should().Be(request.Name);
        product.Stock.Should().Be(50);
    }

    [Fact]
    public async Task CreateProduct_InvalidRequest_Returns400()
    {
        var request = new CreateProductRequest
        {
            Name         = "A",          // demasiado corto
            Price        = -5,           // negativo
            Currency     = "INVALID",    // no ISO
            InitialStock = -1,           // negativo
            CategoryId   = Guid.Empty    // vacío
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReserveStock_ProductNotFound_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/products/{Guid.NewGuid()}/reserve",
            new { orderId = Guid.NewGuid(), quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
