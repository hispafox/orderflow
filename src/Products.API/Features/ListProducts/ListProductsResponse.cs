namespace Products.API.Features.ListProducts;

public record ProductSummaryResponse(
    Guid    Id,
    string  Name,
    decimal Price,
    string  Currency,
    int     Stock,
    bool    IsActive,
    Guid    CategoryId);

public record PagedProductResult(
    IList<ProductSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int  TotalPages      => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage     => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
