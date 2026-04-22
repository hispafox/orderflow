using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Queries;

namespace Orders.API.Application.Interfaces;

public interface IOrderReadRepository
{
    Task<OrderDto?> GetByIdAsync(
        Guid orderId,
        Guid requestingUserId,
        bool isAdmin,
        CancellationToken ct = default);

    Task<PagedResult<OrderSummaryDto>> ListAsync(
        Guid?   customerId,
        string? status,
        int     page,
        int     pageSize,
        CancellationToken ct = default);

    Task<PagedResult<OrderSummaryDto>> SearchAsync(
        OrderSearchQuery  query,
        CancellationToken ct = default);

    Task<OrderStatsDto> GetStatsByCustomerAsync(
        Guid customerId,
        CancellationToken ct = default);
}
