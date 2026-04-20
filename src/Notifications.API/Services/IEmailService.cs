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
}
