using MassTransit;
using Notifications.API.Services;
using OrderFlow.Contracts.Events.Orders;

namespace Notifications.API.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(
        IEmailService                    emailService,
        ILogger<OrderConfirmedConsumer>  logger)
    {
        _emailService = emailService;
        _logger       = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var msg = context.Message;

        _logger.LogInformation(
            "Processing OrderConfirmed for order {OrderId}. Total: {Total} {Currency}",
            msg.OrderId, msg.Total, msg.Currency);

        await _emailService.SendPaymentConfirmationAsync(
            msg.CustomerEmail, msg.OrderId,
            msg.PaymentId, msg.Total, msg.Currency,
            context.CancellationToken);
    }
}
