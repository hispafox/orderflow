using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.Infrastructure.Persistence;
using Orders.API.Infrastructure.Persistence.ReadModel;

namespace Orders.API.Infrastructure.Consumers.Projectors;

public class OrderSummaryProjector : IConsumer<OrderCreated>
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderSummaryProjector> _logger;

    public OrderSummaryProjector(
        OrderDbContext                  dbContext,
        ILogger<OrderSummaryProjector>  logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var msg = context.Message;

        var exists = await _dbContext.OrderSummaries
            .AnyAsync(s => s.OrderId == msg.OrderId, context.CancellationToken);

        if (exists)
        {
            _logger.LogWarning(
                "OrderSummary for {OrderId} already exists — skipping duplicate",
                msg.OrderId);
            return;
        }

        _dbContext.OrderSummaries.Add(new OrderSummary
        {
            OrderId       = msg.OrderId,
            CustomerId    = msg.CustomerId,
            CustomerEmail = msg.CustomerEmail,
            Status        = "Pending",
            TotalAmount   = msg.Total,
            Currency      = msg.Currency,
            LinesCount    = msg.Items.Count,
            FirstItemName = msg.Items.FirstOrDefault()?.ProductName,
            CreatedAt     = msg.CreatedAt,
            ShippingCity  = msg.ShippingAddress?.City
        });

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "[PROJECTOR] OrderSummary created for order {OrderId}", msg.OrderId);
    }
}
