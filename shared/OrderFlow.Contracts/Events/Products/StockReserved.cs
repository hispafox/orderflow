namespace OrderFlow.Contracts.Events.Products;

public record StockReserved
{
    public Guid OrderId   { get; init; }
    public Guid ProductId { get; init; }
    public int  Quantity  { get; init; }
}

public record StockInsufficient
{
    public Guid OrderId   { get; init; }
    public Guid ProductId { get; init; }
    public int  Requested { get; init; }
    public int  Available { get; init; }
}

public record StockReleased
{
    public Guid OrderId   { get; init; }
    public Guid ProductId { get; init; }
    public int  Quantity  { get; init; }
}
