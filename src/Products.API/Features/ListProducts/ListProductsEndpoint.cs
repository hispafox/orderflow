using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Products.API.Domain.Interfaces;

namespace Products.API.Features.ListProducts;

public static class ListProductsEndpoint
{
    public static async Task<Ok<PagedProductResult>> Handle(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        IProductRepository  repository = null!,
        CancellationToken   ct         = default)
    {
        var (items, totalCount) = await repository.ListAsync(
            category, search, page, pageSize, ct);

        var result = new PagedProductResult(
            Items:      items.Select(p => p.ToSummaryResponse()).ToList(),
            TotalCount: totalCount,
            Page:       page,
            PageSize:   pageSize);

        return TypedResults.Ok(result);
    }
}
