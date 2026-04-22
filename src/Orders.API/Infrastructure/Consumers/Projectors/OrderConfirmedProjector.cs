using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Infrastructure.Consumers.Projectors;

public class OrderConfirmedProjector : IConsumer<OrderConfirmed>
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderConfirmedProjector> _logger;

    public OrderConfirmedProjector(
        OrderDbContext                    dbContext,
        ILogger<OrderConfirmedProjector>  logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var msg     = context.Message;
        var summary = await _dbContext.OrderSummaries
            .FirstOrDefaultAsync(s => s.OrderId == msg.OrderId, context.CancellationToken);

        if (summary is null)
            throw new InvalidOperationException(
                $"OrderSummary for {msg.OrderId} not found — will retry");

        if (summary.Status == "Confirmed")
        {
            _logger.LogWarning(
                "OrderSummary {OrderId} already Confirmed — skipping", msg.OrderId);
            return;
        }

        summary.Status      = "Confirmed";
        summary.ConfirmedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation(
            "[PROJECTOR] OrderSummary confirmed for {OrderId}", msg.OrderId);
    }
}
