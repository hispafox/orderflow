using OrderFlow.Contracts.Events.Orders;

namespace Notifications.API.Services;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(
        string                  email,
        Guid                    orderId,
        decimal                 total,
        string                  currency,
        IList<OrderCreatedItem> items,
        CancellationToken       ct = default);

    Task SendPaymentConfirmationAsync(
        string            email,
        Guid              orderId,
        Guid              paymentId,
        decimal           total,
        string            currency,
        CancellationToken ct = default);

    Task SendOrderFailedAsync(
        string            email,
        Guid              orderId,
        string            reason,
        CancellationToken ct = default);
}
