namespace Payments.API.Services;

/// <summary>
/// Implementación fake del gateway de pagos.
/// 90% de éxito, 10% de fallo — para ejercitar el flujo de compensación.
/// En producción: reemplazar por Stripe, Redsys, PayPal, etc.
/// </summary>
public class FakePaymentGateway : IPaymentGateway
{
    private static readonly Random _random = new();
    private readonly ILogger<FakePaymentGateway> _logger;

    public FakePaymentGateway(ILogger<FakePaymentGateway> logger)
        => _logger = logger;

    public async Task<PaymentResult> ProcessAsync(
        Guid              orderId,
        decimal           amount,
        string            currency,
        CancellationToken ct = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(200), ct);

        var success = _random.NextDouble() > 0.1;

        if (success)
        {
            var transactionId = Guid.NewGuid().ToString("N");
            _logger.LogInformation(
                "[PAGO SIMULADO] OrderId={OrderId} Amount={Amount} {Currency} " +
                "TransactionId={TransactionId}",
                orderId, amount, currency, transactionId);
            return PaymentResult.Succeeded(transactionId, orderId, amount, currency);
        }

        var reason = "Insufficient funds (simulated)";
        _logger.LogWarning(
            "[PAGO SIMULADO FALLIDO] OrderId={OrderId} Amount={Amount} {Currency} Reason={Reason}",
            orderId, amount, currency, reason);
        return PaymentResult.Failed(orderId, reason);
    }
}
