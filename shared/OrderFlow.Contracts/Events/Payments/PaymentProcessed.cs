namespace OrderFlow.Contracts.Events.Payments;

public record PaymentProcessed
{
    public Guid     OrderId     { get; init; }
    public Guid     PaymentId   { get; init; }
    public Guid     CustomerId  { get; init; }
    public decimal  Amount      { get; init; }
    public string   Currency    { get; init; } = "EUR";
    public DateTime ProcessedAt { get; init; }
}

public record PaymentFailed
{
    public Guid   OrderId { get; init; }
    public string Reason  { get; init; } = string.Empty;
}
