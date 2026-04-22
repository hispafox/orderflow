namespace Payments.API.Domain;

/// <summary>
/// Aggregate de pago. Dominio simple y enfocado: solo gestiona el ciclo de vida del cobro.
/// No comparte contexto con Orders ni Products — base de datos propia (PaymentsDb).
/// </summary>
public class Payment
{
    public Guid          Id            { get; private set; } = Guid.NewGuid();
    public Guid          OrderId       { get; private set; }
    public Guid          CustomerId    { get; private set; }
    public decimal       Amount        { get; private set; }
    public string        Currency      { get; private set; } = string.Empty;
    public PaymentStatus Status        { get; private set; }
    public string?       TransactionId { get; private set; }
    public string?       FailureReason { get; private set; }
    public DateTime      CreatedAt     { get; private set; } = DateTime.UtcNow;
    public DateTime?     ProcessedAt   { get; private set; }

    private Payment() { }

    public static Payment Create(
        Guid    orderId,
        Guid    customerId,
        decimal amount,
        string  currency) => new()
    {
        OrderId    = orderId,
        CustomerId = customerId,
        Amount     = amount,
        Currency   = currency,
        Status     = PaymentStatus.Pending
    };

    public void MarkAsProcessed(string transactionId)
    {
        Status        = PaymentStatus.Processed;
        TransactionId = transactionId;
        ProcessedAt   = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? reason)
    {
        Status        = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessedAt   = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        if (Status != PaymentStatus.Processed)
            throw new InvalidOperationException("Only processed payments can be refunded");
        Status      = PaymentStatus.Refunded;
        ProcessedAt = DateTime.UtcNow;
    }
}
