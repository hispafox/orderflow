using OrderFlow.Contracts.Events.Orders;

namespace Notifications.API.Services;

/// <summary>
/// Implementación fake del email service para el curso.
/// Loggea el email que se enviaría en producción.
/// En producción: reemplazar por SendGrid, Amazon SES o SMTP.
/// </summary>
public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger)
        => _logger = logger;

    public Task SendOrderConfirmationAsync(
        string                  email,
        Guid                    orderId,
        decimal                 total,
        string                  currency,
        IList<OrderCreatedItem> items,
        CancellationToken       ct = default)
    {
        _logger.LogInformation(
            "[EMAIL SIMULADO] Para: {Email} | Pedido: {OrderId} | " +
            "Total: {Total} {Currency} | Artículos: {ItemCount}",
            email, orderId, total, currency, items.Count);

        foreach (var item in items)
            _logger.LogInformation(
                "[EMAIL] - {Qty}x {Name} a {Price} {Currency}",
                item.Quantity, item.ProductName, item.UnitPrice, currency);

        return Task.CompletedTask;
    }
}
