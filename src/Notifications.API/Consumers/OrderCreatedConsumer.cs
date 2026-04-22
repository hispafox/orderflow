using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.API.Domain;
using Notifications.API.Infrastructure;
using Notifications.API.Services;
using OrderFlow.Contracts.Events.Orders;

namespace Notifications.API.Consumers;

/// <summary>
/// Consumer de OrderCreated. Envía email de confirmación al cliente.
/// Idempotencia básica: verifica si ya procesamos este OrderId + Type.
/// Si el mismo mensaje llega dos veces (at-least-once), el segundo se ignora.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly IEmailService         _emailService;
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(
        IEmailService                  emailService,
        NotificationDbContext          dbContext,
        ILogger<OrderCreatedConsumer>  logger)
    {
        _emailService = emailService;
        _dbContext    = dbContext;
        _logger       = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing OrderCreated for order {OrderId}. " +
            "Customer: {Email}. Total: {Total} {Currency}",
            message.OrderId, message.CustomerEmail,
            message.Total, message.Currency);

        var alreadyProcessed = await _dbContext.Notifications.AnyAsync(
            n => n.OrderId == message.OrderId && n.Type == "OrderConfirmation",
            context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogWarning(
                "Duplicate OrderCreated for {OrderId} — skipping",
                message.OrderId);
            return;
        }

        await _emailService.SendOrderConfirmationAsync(
            message.CustomerEmail,
            message.OrderId,
            message.Total,
            message.Currency,
            message.Items,
            context.CancellationToken);

        _dbContext.Notifications.Add(Notification.Create(
            message.OrderId,
            "OrderConfirmation",
            message.CustomerEmail,
            success: true));

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Order confirmation notification sent and recorded for order {OrderId}",
            message.OrderId);
    }
}
