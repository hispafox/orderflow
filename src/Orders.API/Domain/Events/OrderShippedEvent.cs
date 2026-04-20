namespace Orders.API.Domain.Events;

public sealed record OrderShippedEvent(Guid OrderId) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
