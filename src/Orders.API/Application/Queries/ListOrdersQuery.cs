using MediatR;
using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Responses;

namespace Orders.API.Application.Queries;

public record ListOrdersQuery(
    string? Status,
    int     Page,
    int     PageSize,
    Guid?   CustomerId = null,
    string? UserId     = null,
    bool    IsAdmin    = false)
    : IRequest<PagedResult<OrderSummaryDto>>;
