namespace Orders.API.Domain.Events;

public sealed record OrderCancelledEvent(
    Guid   OrderId,
    Guid   CustomerId,
    string Reason) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
