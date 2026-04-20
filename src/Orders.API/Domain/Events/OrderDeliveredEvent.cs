namespace Orders.API.Domain.Events;

public sealed record OrderDeliveredEvent(Guid OrderId) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
