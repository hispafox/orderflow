using MassTransit;
using OrderFlow.Contracts.Commands;
using OrderFlow.Contracts.Events.Products;
using Products.API.Domain.Interfaces;

namespace Products.API.Consumers;

/// <summary>
/// Consumer de compensación: libera el stock reservado cuando el pago falla.
/// Es la acción inversa del ReserveStockConsumer.
/// </summary>
public class ReleaseStockConsumer : IConsumer<ReleaseStock>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ReleaseStockConsumer> _logger;

    public ReleaseStockConsumer(
        IProductRepository           repository,
        ILogger<ReleaseStockConsumer> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseStock> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Releasing {Quantity} units of {ProductId} for order {OrderId}",
            msg.Quantity, msg.ProductId, msg.OrderId);

        var product = await _repository.GetByIdAsync(msg.ProductId, context.CancellationToken);

        if (product is null)
        {
            _logger.LogError(
                "Cannot release stock: product {ProductId} not found for order {OrderId}",
                msg.ProductId, msg.OrderId);
            return;
        }

        product.UpdateStock(product.Stock + msg.Quantity);
        await _repository.SaveAsync(product, context.CancellationToken);

        await context.Publish(new StockReleased
        {
            OrderId          = msg.OrderId,
            ProductId        = msg.ProductId,
            ReleasedQuantity = msg.Quantity
        }, context.CancellationToken);

        _logger.LogInformation(
            "Released {Quantity} units of {ProductId} for order {OrderId}",
            msg.Quantity, msg.ProductId, msg.OrderId);
    }
}
