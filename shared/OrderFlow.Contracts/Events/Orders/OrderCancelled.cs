namespace OrderFlow.Contracts.Events.Orders;

public record OrderCancelled
{
    public Guid     OrderId       { get; init; }
    public Guid     CustomerId    { get; init; }
    public string   CustomerEmail { get; init; } = string.Empty;
    public string   Reason        { get; init; } = string.Empty;
    public DateTime CancelledAt   { get; init; }
}
