namespace OrderFlow.Contracts.Events.Orders;

/// <summary>
/// Integration Event publicado por Orders.API cuando se crea un pedido.
/// Consumers en M4.2: Notifications.API (email de confirmación)
/// Consumers en M4.3: Products.API (reserva de stock via Saga)
/// </summary>
public record OrderCreated
{
    public Guid     OrderId       { get; init; }
    public Guid     CustomerId    { get; init; }
    public string   CustomerEmail { get; init; } = string.Empty;
    public decimal  Total         { get; init; }
    public string   Currency      { get; init; } = "EUR";
    public DateTime CreatedAt     { get; init; }
    public IList<OrderCreatedItem>    Items           { get; init; } = [];
    public OrderCreatedAddress        ShippingAddress { get; init; } = null!;
}

public record OrderCreatedItem(
    Guid    ProductId,
    string  ProductName,
    int     Quantity,
    decimal UnitPrice);

public record OrderCreatedAddress(
    string Street,
    string City,
    string ZipCode,
    string Country);
