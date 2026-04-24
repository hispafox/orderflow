using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Infrastructure.Consumers.Projectors;

/// <summary>
/// Actualiza el read model (OrderSummaries) cuando el Saga termina en Failed
/// (por stock insuficiente, timeout o PaymentFailed tras compensar stock).
///
/// Nota: reutiliza la columna CancellationReason para guardar el motivo del
/// fallo y evitar una migración. Semánticamente un fallo no es exactamente
/// una cancelación, pero desde el punto de vista del cliente el resultado
/// es similar (pedido no completado) y la UI puede diferenciarlos por el
/// Status.
/// </summary>
public class OrderFailedProjector : IConsumer<OrderFailed>
{
    private readonly OrderDbContext                _dbContext;
    private readonly ILogger<OrderFailedProjector> _logger;

    public OrderFailedProjector(
        OrderDbContext                dbContext,
        ILogger<OrderFailedProjector> logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task Consume(ConsumeContext<OrderFailed> context)
    {
        var msg     = context.Message;
        var summary = await _dbContext.OrderSummaries
            .FirstOrDefaultAsync(s => s.OrderId == msg.OrderId, context.CancellationToken);

        if (summary is null)
        {
            _logger.LogWarning(
                "OrderFailed para {OrderId} pero no existe el OrderSummary — posible race con el proyector de OrderCreated", msg.OrderId);
            return;
        }

        if (summary.Status == "Failed")
        {
            _logger.LogWarning(
                "OrderSummary {OrderId} ya está Failed — skipping", msg.OrderId);
            return;
        }

        summary.Status             = "Failed";
        summary.CancellationReason = msg.Reason;  // reutilizado: ver <summary>

        await _dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation(
            "[PROJECTOR] OrderSummary failed for {OrderId} — {Reason}", msg.OrderId, msg.Reason);
    }
}
