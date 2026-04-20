namespace Products.API.Features.CreateProduct;

public record CreateProductRequest
{
    public string  Name         { get; init; } = string.Empty;
    public string  Description  { get; init; } = string.Empty;
    public decimal Price        { get; init; }
    public string  Currency     { get; init; } = "EUR";
    public int     InitialStock { get; init; }
    public Guid    CategoryId   { get; init; }
}
