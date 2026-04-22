using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Infrastructure.Consumers.Projectors;

public class OrderCancelledProjector : IConsumer<OrderCancelled>
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderCancelledProjector> _logger;

    public OrderCancelledProjector(
        OrderDbContext                    dbContext,
        ILogger<OrderCancelledProjector>  logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelled> context)
    {
        var msg     = context.Message;
        var summary = await _dbContext.OrderSummaries
            .FirstOrDefaultAsync(s => s.OrderId == msg.OrderId, context.CancellationToken);

        if (summary is null || summary.Status == "Cancelled") return;

        summary.Status             = "Cancelled";
        summary.CancelledAt        = msg.CancelledAt;
        summary.CancellationReason = msg.Reason;

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation(
            "[PROJECTOR] OrderSummary cancelled for {OrderId}", msg.OrderId);
    }
}
