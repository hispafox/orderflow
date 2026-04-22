namespace OrderFlow.Contracts.Events.Products;

public record StockReserved
{
    public Guid OrderId          { get; init; }
    public Guid ProductId        { get; init; }
    public int  ReservedQuantity { get; init; }
}

public record StockInsufficient
{
    public Guid OrderId            { get; init; }
    public Guid ProductId          { get; init; }
    public int  RequestedQuantity  { get; init; }
    public int  AvailableQuantity  { get; init; }
}

public record StockReleased
{
    public Guid OrderId          { get; init; }
    public Guid ProductId        { get; init; }
    public int  ReleasedQuantity { get; init; }
}
