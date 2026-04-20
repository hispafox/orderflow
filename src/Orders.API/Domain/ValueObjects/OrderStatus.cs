using Orders.API.Domain.Exceptions;

namespace Orders.API.Domain.ValueObjects;

public sealed record OrderStatus
{
    public static readonly OrderStatus Pending   = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped   = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    public string Value { get; }

    private OrderStatus(string value) => Value = value;

    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        [Pending]   = [Confirmed, Cancelled],
        [Confirmed] = [Shipped,   Cancelled],
        [Shipped]   = [Delivered],
        [Delivered] = [],
        [Cancelled] = []
    };

    public bool CanTransitionTo(OrderStatus next)
        => ValidTransitions[this].Contains(next);

    public OrderStatus TransitionTo(OrderStatus next)
    {
        if (!CanTransitionTo(next))
            throw new DomainException($"Cannot transition from '{Value}' to '{next.Value}'");
        return next;
    }

    public static OrderStatus FromString(string value) => value switch
    {
        "Pending"   => Pending,
        "Confirmed" => Confirmed,
        "Shipped"   => Shipped,
        "Delivered" => Delivered,
        "Cancelled" => Cancelled,
        _ => throw new DomainException($"Unknown OrderStatus: '{value}'")
    };

    public override string ToString() => Value;
}
