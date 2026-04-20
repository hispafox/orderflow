using Orders.API.Domain.Entities;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Tests.Shared.Builders;

public class OrderBuilder
{
    private Guid _customerId = Guid.NewGuid();
    private readonly List<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)> _items = [];
    private Address _address = new("Calle Mayor 1", "Madrid", "28001", "ES");

    public OrderBuilder WithCustomer(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderBuilder WithAddress(string street, string city, string zip, string country)
    {
        _address = new Address(street, city, zip, country);
        return this;
    }

    public OrderBuilder WithLine(
        string  productName = "Test Product",
        int     quantity    = 1,
        decimal price       = 10.00m,
        string  currency    = "EUR")
    {
        _items.Add((Guid.NewGuid(), productName, quantity, new Money(price, currency)));
        return this;
    }

    public OrderBuilder WithManyLines(int count)
    {
        for (int i = 0; i < count; i++)
            WithLine($"Product {i + 1}");
        return this;
    }

    public Order Build()
    {
        if (_items.Count == 0) WithLine();
        return Order.Create(_customerId, _items, _address);
    }

    public Order BuildConfirmed()
    {
        var order = Build();
        order.Confirm();
        return order;
    }

    public Order BuildCancelled(string reason = "Test cancellation")
    {
        var order = Build();
        order.Cancel(reason);
        return order;
    }
}
