using Orders.API.Domain.Exceptions;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Domain.Entities;

public class OrderLine
{
    public Guid   Id          { get; private set; }
    public Guid   ProductId   { get; private set; }
    public string  ProductName { get; private set; } = null!;
    public string? ProductSku  { get; private set; }
    public int     Quantity    { get; private set; }
    public Money   UnitPrice   { get; private set; } = null!;

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderLine() { }

    internal static OrderLine Create(
        Guid   productId,
        string productName,
        int    quantity,
        Money  unitPrice)
    {
        if (quantity <= 0 || quantity > 1000)
            throw new DomainException($"Quantity must be between 1 and 1000. Got: {quantity}");

        return new OrderLine
        {
            Id          = Guid.NewGuid(),
            ProductId   = productId,
            ProductName = Guard.NotNullOrEmpty(productName, nameof(productName)),
            Quantity    = quantity,
            UnitPrice   = Guard.NotNull(unitPrice, nameof(unitPrice))
        };
    }

    internal void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new DomainException("Quantity must be positive");
        Quantity = newQuantity;
    }
}
