namespace Payments.API.Services;

public record PaymentResult
{
    public bool    IsSuccess     { get; init; }
    public string? TransactionId { get; init; }
    public Guid    OrderId       { get; init; }
    public decimal Amount        { get; init; }
    public string  Currency      { get; init; } = "EUR";
    public string? FailureReason { get; init; }

    public static PaymentResult Succeeded(
        string transactionId, Guid orderId,
        decimal amount, string currency) => new()
    {
        IsSuccess     = true,
        TransactionId = transactionId,
        OrderId       = orderId,
        Amount        = amount,
        Currency      = currency
    };

    public static PaymentResult Failed(Guid orderId, string reason) => new()
    {
        IsSuccess     = false,
        OrderId       = orderId,
        FailureReason = reason
    };
}
