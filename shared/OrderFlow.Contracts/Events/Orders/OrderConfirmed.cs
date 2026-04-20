namespace OrderFlow.Contracts.Events.Orders;

public record OrderConfirmed
{
    public Guid     OrderId       { get; init; }
    public Guid     CustomerId    { get; init; }
    public string   CustomerEmail { get; init; } = string.Empty;
    public DateTime ConfirmedAt   { get; init; }
}
