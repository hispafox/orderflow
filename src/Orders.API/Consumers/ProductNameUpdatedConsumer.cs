using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using OrderFlow.Contracts.Events.Products;

namespace Orders.API.Consumers;

/// <summary>
/// Invalida la caché del ProductsClient cuando Products.API cambia el nombre de un producto.
/// Los pedidos ya completados conservan el nombre histórico — solo la caché se invalida.
/// </summary>
public class ProductNameUpdatedConsumer : IConsumer<ProductUpdated>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductNameUpdatedConsumer> _logger;

    public ProductNameUpdatedConsumer(IMemoryCache cache, ILogger<ProductNameUpdatedConsumer> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ProductUpdated> context)
    {
        var productId = context.Message.ProductId;

        _cache.Remove($"product:{productId}");

        _logger.LogInformation(
            "Cache invalidated for product {ProductId} after name update",
            productId);

        return Task.CompletedTask;
    }
}
