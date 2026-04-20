using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Orders.API.Infrastructure.Http;
using RichardSzalay.MockHttp;
using Xunit;

namespace Orders.API.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class ProductsClientTests
{
    private static readonly Guid ProductId = Guid.Parse("a4b5c6d7-e8f9-0123-4567-89abcdef0123");

    private static readonly string ProductJson = JsonSerializer.Serialize(new
    {
        id         = ProductId,
        name       = "MacBook Pro",
        price      = 1999.99,
        currency   = "EUR",
        stock      = 10,
        isActive   = true,
        categoryId = Guid.NewGuid()
    });

    // ─── GetProductAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProduct_SuccessOnFirstCall_ReturnsProductAndPopulatesCache()
    {
        var cache   = new MemoryCache(new MemoryCacheOptions());
        var handler = new MockHttpMessageHandler();
        handler.When($"/api/products/{ProductId}")
               .Respond("application/json", ProductJson);

        var client = BuildClient(handler, cache);
        var result = await client.GetProductAsync(ProductId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("MacBook Pro");
        result.Price.Should().Be(1999.99m);

        cache.TryGetValue($"product:{ProductId}", out ProductDetailDto? cached).Should().BeTrue();
        cached!.Name.Should().Be("MacBook Pro");
    }

    [Fact]
    public async Task GetProduct_CalledTwice_SecondCallUsesCache()
    {
        var callCount = 0;
        var handler   = new MockHttpMessageHandler();
        handler.When($"/api/products/{ProductId}")
               .Respond(_ =>
               {
                   callCount++;
                   return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                   {
                       Content = new StringContent(ProductJson,
                           System.Text.Encoding.UTF8, "application/json")
                   });
               });

        var client = BuildClient(handler);
        await client.GetProductAsync(ProductId);
        await client.GetProductAsync(ProductId);

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetProduct_ProductNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When($"/api/products/{ProductId}")
               .Respond(HttpStatusCode.NotFound);

        var result = await BuildClient(handler).GetProductAsync(ProductId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProduct_ServiceUnavailable_ReturnsCachedValue()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set($"product:{ProductId}",
            new ProductDetailDto(ProductId, "MacBook Pro", 1999.99m, "EUR", 10, true, Guid.Empty),
            TimeSpan.FromMinutes(5));

        var handler = new MockHttpMessageHandler();
        handler.When($"/api/products/{ProductId}")
               .Respond(HttpStatusCode.ServiceUnavailable);

        var result = await BuildClient(handler, cache).GetProductAsync(ProductId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("MacBook Pro");
    }

    [Fact]
    public async Task GetProduct_ServiceUnavailable_NoCache_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When($"/api/products/{ProductId}")
               .Respond(HttpStatusCode.ServiceUnavailable);

        var result = await BuildClient(handler).GetProductAsync(ProductId);

        result.Should().BeNull();
    }

    // ─── ReserveStockAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ReserveStock_Success_ReturnsTrue()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, $"/api/products/{ProductId}/reserve")
               .Respond(HttpStatusCode.OK);

        var result = await BuildClient(handler)
            .ReserveStockAsync(ProductId, Guid.NewGuid(), 1);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ReserveStock_InsufficientStock_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, $"/api/products/{ProductId}/reserve")
               .Respond(HttpStatusCode.UnprocessableEntity);

        var result = await BuildClient(handler)
            .ReserveStockAsync(ProductId, Guid.NewGuid(), 100);

        result.Should().BeFalse();
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static ProductsClient BuildClient(
        MockHttpMessageHandler handler,
        IMemoryCache?          cache = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://test")
        };
        return new ProductsClient(
            httpClient,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ProductsClient>.Instance);
    }
}
