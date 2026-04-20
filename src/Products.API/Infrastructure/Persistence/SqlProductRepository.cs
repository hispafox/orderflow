using Microsoft.EntityFrameworkCore;
using Products.API.Domain;
using Products.API.Domain.Interfaces;

namespace Products.API.Infrastructure.Persistence;

public class SqlProductRepository : IProductRepository
{
    private readonly ProductDbContext _dbContext;

    public SqlProductRepository(ProductDbContext dbContext) => _dbContext = dbContext;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbContext.Products.FindAsync([id], ct);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        string? category, string? search,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbContext.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) || p.Description.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task SaveAsync(Product product, CancellationToken ct = default)
    {
        if (_dbContext.Entry(product).State == EntityState.Detached)
            _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(ct);
    }
}
