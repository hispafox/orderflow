using Orders.API.Domain.ValueObjects;

namespace Orders.API.Domain.Events;

public sealed record OrderConfirmedEvent(
    Guid  OrderId,
    Guid  CustomerId,
    Money Total) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
