namespace OrderFlow.Contracts.Events.Products;

/// <summary>
/// Publicado por Products.API cuando se actualiza el nombre u otros datos del producto.
/// Orders.API lo consume para invalidar su caché de productos.
/// </summary>
public record ProductUpdated
{
    public Guid     ProductId  { get; init; }
    public string   NewName    { get; init; } = string.Empty;
    public decimal  NewPrice   { get; init; }
    public string   Currency   { get; init; } = "EUR";
    public DateTime UpdatedAt  { get; init; }
}
