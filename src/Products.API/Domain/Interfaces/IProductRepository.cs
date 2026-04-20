namespace Products.API.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        string? category, string? search,
        int page, int pageSize,
        CancellationToken ct = default);
    Task SaveAsync(Product product, CancellationToken ct = default);
}
