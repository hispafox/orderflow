using MassTransit;
using OrderFlow.Contracts.Commands;
using OrderFlow.Contracts.Events.Products;
using Products.API.Domain.Interfaces;

namespace Products.API.Consumers;

/// <summary>
/// Consumer del comando ReserveStock enviado por la Saga de Orders.
/// Verifica disponibilidad y reserva el stock en la BD de Products.
/// Publica StockReserved o StockInsufficient según disponibilidad.
/// </summary>
public class ReserveStockConsumer : IConsumer<ReserveStock>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ReserveStockConsumer> _logger;

    public ReserveStockConsumer(
        IProductRepository            repository,
        ILogger<ReserveStockConsumer> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Reserving {Quantity} units of {ProductId} for order {OrderId}",
            msg.Quantity, msg.ProductId, msg.OrderId);

        var product = await _repository.GetByIdAsync(msg.ProductId, context.CancellationToken);

        if (product is null)
        {
            _logger.LogWarning(
                "Product {ProductId} not found for order {OrderId}",
                msg.ProductId, msg.OrderId);
            await context.Publish(new StockInsufficient
            {
                OrderId           = msg.OrderId,
                ProductId         = msg.ProductId,
                RequestedQuantity = msg.Quantity,
                AvailableQuantity = 0
            }, context.CancellationToken);
            return;
        }

        if (!product.HasSufficientStock(msg.Quantity))
        {
            _logger.LogWarning(
                "Insufficient stock for {ProductId}: requested {Requested}, available {Available}",
                msg.ProductId, msg.Quantity, product.Stock);
            await context.Publish(new StockInsufficient
            {
                OrderId           = msg.OrderId,
                ProductId         = msg.ProductId,
                RequestedQuantity = msg.Quantity,
                AvailableQuantity = product.Stock
            }, context.CancellationToken);
            return;
        }

        product.ReserveStock(msg.Quantity, msg.OrderId);
        await _repository.SaveAsync(product, context.CancellationToken);

        await context.Publish(new StockReserved
        {
            OrderId          = msg.OrderId,
            ProductId        = msg.ProductId,
            ReservedQuantity = msg.Quantity
        }, context.CancellationToken);

        _logger.LogInformation(
            "Stock reserved: {Quantity} units of {ProductId} for order {OrderId}",
            msg.Quantity, msg.ProductId, msg.OrderId);
    }
}
