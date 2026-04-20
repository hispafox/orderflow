using MediatR;
using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Responses;

namespace Orders.API.Application.Queries;

public record ListOrdersQuery(
    string? Status,
    int     Page,
    int     PageSize,
    Guid?   CustomerId = null) : IRequest<PagedResult<OrderSummaryDto>>;
