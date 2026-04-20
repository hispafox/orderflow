using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Interfaces;

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

    public async Task SaveAsync(Order order, CancellationToken ct = default)
    {
        var entry = _dbContext.Entry(order);
        if (entry.State == EntityState.Detached)
            _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(ct);
    }
}
