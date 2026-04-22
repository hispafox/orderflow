using MassTransit;
using Notifications.API.Services;
using OrderFlow.Contracts.Events.Orders;

namespace Notifications.API.Consumers;

public class OrderFailedConsumer : IConsumer<OrderFailed>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderFailedConsumer> _logger;

    public OrderFailedConsumer(
        IEmailService                 emailService,
        ILogger<OrderFailedConsumer>  logger)
    {
        _emailService = emailService;
        _logger       = logger;
    }

    public async Task Consume(ConsumeContext<OrderFailed> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Processing OrderFailed for order {OrderId}. Reason: {Reason}",
            msg.OrderId, msg.Reason);

        _logger.LogInformation(
            "[EMAIL SIMULADO] Pedido {OrderId} fallido. Razón: {Reason}",
            msg.OrderId, msg.Reason);
    }
}
