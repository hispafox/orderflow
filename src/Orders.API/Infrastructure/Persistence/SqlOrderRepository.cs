using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Interfaces;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Infrastructure.Persistence;

public class SqlOrderRepository : IOrderRepository
{
    private readonly OrderDbContext _dbContext;

    public SqlOrderRepository(OrderDbContext dbContext) => _dbContext = dbContext;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default)
        => await _dbContext.Orders
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetPendingAsync(CancellationToken ct = default)
        => await _dbContext.Orders
            .Include(o => o.Lines)
            .Where(o => o.Status == Domain.ValueObjects.OrderStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        string? status,
        Guid?   customerId,
        int     page,
        int     pageSize,
        CancellationToken ct = default)
    {
        var query = _dbContext.Orders.Include(o => o.Lines).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var orderStatus = OrderStatus.FromString(status);
            query = query.Where(o => o.Status == orderStatus);
        }

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task SaveAsync(Order order, CancellationToken ct = default)
    {
        var entry = _dbContext.Entry(order);
        if (entry.State == EntityState.Detached)
            _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);
    }
}
