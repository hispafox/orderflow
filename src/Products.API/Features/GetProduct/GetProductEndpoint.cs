using Microsoft.AspNetCore.Http.HttpResults;
using Products.API.Domain.Interfaces;

namespace Products.API.Features.GetProduct;

public static class GetProductEndpoint
{
    public static async Task<Results<Ok<ProductResponse>, NotFound>> Handle(
        Guid               id,
        IProductRepository repository,
        CancellationToken  ct = default)
    {
        var product = await repository.GetByIdAsync(id, ct);

        if (product is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(product.ToResponse());
    }
}
