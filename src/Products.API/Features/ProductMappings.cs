using Products.API.Domain;
using Products.API.Features.GetProduct;
using Products.API.Features.ListProducts;

namespace Products.API.Features;

public static class ProductMappings
{
    public static ProductSummaryResponse ToSummaryResponse(this Product p) => new(
        p.Id, p.Name, p.Price, p.Currency, p.Stock, p.IsActive, p.CategoryId);

    public static ProductResponse ToResponse(this Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.Currency,
        p.Stock, p.IsActive, p.CategoryId, p.CreatedAt, p.UpdatedAt);
}
