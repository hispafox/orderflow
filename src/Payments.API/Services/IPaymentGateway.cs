namespace Payments.API.Services;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessAsync(
        Guid              orderId,
        decimal           amount,
        string            currency,
        CancellationToken ct = default);
}
