namespace OrderFlow.Contracts.Commands;

/// <summary>
/// Comando que la Saga envía a Products.API para reservar stock.
/// Point-to-point: Products es el único receptor.
/// </summary>
public record ReserveStock
{
    public Guid OrderId   { get; init; }
    public Guid ProductId { get; init; }
    public int  Quantity  { get; init; }
}
