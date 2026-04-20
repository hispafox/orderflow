using Orders.API.Domain.Events;
using Orders.API.Domain.Exceptions;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Domain.Entities;

public class Order
{
    public Guid        Id              { get; private set; }
    public Guid        CustomerId      { get; private set; }
    public OrderStatus Status          { get; private set; } = null!;
    public Address     ShippingAddress { get; private set; } = null!;
    public DateTime    CreatedAt       { get; private set; }
    public DateTime?   ConfirmedAt     { get; private set; }
    public DateTime?   CancelledAt     { get; private set; }

    // C# 14 field-backed property — validación en setter sin campo de respaldo explícito
    public string? CancellationReason
    {
        get;
        private set
        {
            if (value is not null && value.Length > 500)
                throw new DomainException("Cancellation reason cannot exceed 500 characters");
            field = value;
        }
    }

    private readonly List<OrderLine> _lines = [];
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total => Lines.Any()
        ? Lines.Skip(1).Aggregate(Lines[0].LineTotal, (acc, line) => acc.Add(line.LineTotal))
        : Money.Zero("EUR");

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Order() { }

    public static Order Create(
        Guid customerId,
        IEnumerable<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)> items,
        Address shippingAddress)
    {
        var itemList = items.ToList();

        if (!itemList.Any())
            throw new DomainException("Order must have at least one item");

        if (itemList.Count > 50)
            throw new DomainException("Order cannot have more than 50 lines");

        if (itemList.Any(i => i.UnitPrice.Currency != "EUR"))
            throw new DomainException("TechShop only accepts EUR prices");

        var order = new Order
        {
            Id              = Guid.NewGuid(),
            CustomerId      = customerId,
            Status          = OrderStatus.Pending,
            ShippingAddress = Guard.NotNull(shippingAddress, nameof(shippingAddress)),
            CreatedAt       = DateTime.UtcNow
        };

        foreach (var item in itemList)
            order._lines.Add(OrderLine.Create(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice));

        if (order.Total.Amount < 10.00m)
            throw new DomainException("Minimum order amount is 10.00 EUR");

        order._domainEvents.Add(new OrderCreatedEvent(order.Id, order.CustomerId, order.Total));

        return order;
    }

    public void Confirm()
    {
        Status      = Status.TransitionTo(OrderStatus.Confirmed);
        ConfirmedAt = DateTime.UtcNow;
        _domainEvents.Add(new OrderConfirmedEvent(Id, CustomerId, Total));
    }

    public void Cancel(string reason)
    {
        Guard.NotNullOrEmpty(reason, nameof(reason));
        Status             = Status.TransitionTo(OrderStatus.Cancelled);
        CancelledAt        = DateTime.UtcNow;
        CancellationReason = reason;
        _domainEvents.Add(new OrderCancelledEvent(Id, CustomerId, reason));
    }

    public void Ship()
    {
        Status = Status.TransitionTo(OrderStatus.Shipped);
        _domainEvents.Add(new OrderShippedEvent(Id));
    }

    public void Deliver()
    {
        Status = Status.TransitionTo(OrderStatus.Delivered);
        _domainEvents.Add(new OrderDeliveredEvent(Id));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
