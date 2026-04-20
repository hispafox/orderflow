using Orders.API.Domain.Entities;

namespace Orders.API.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?>              GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetPendingAsync(CancellationToken ct = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        string? status,
        Guid?   customerId,
        int     page,
        int     pageSize,
        CancellationToken ct = default);

    Task SaveAsync(Order order, CancellationToken ct = default);
}
