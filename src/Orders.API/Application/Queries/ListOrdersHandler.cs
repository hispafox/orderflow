using MediatR;
using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Responses;
using Orders.API.API.Mappings;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Queries;

public class ListOrdersHandler : IRequestHandler<ListOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderRepository _repository;

    public ListOrdersHandler(IOrderRepository repository) => _repository = repository;

    public async Task<PagedResult<OrderSummaryDto>> Handle(
        ListOrdersQuery query,
        CancellationToken ct)
    {
        IReadOnlyList<Domain.Entities.Order> orders;

        if (query.CustomerId.HasValue)
            orders = await _repository.GetByCustomerIdAsync(query.CustomerId.Value, ct);
        else
            orders = await _repository.GetPendingAsync(ct);

        var pagedItems = orders
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => o.ToSummaryDto())
            .ToList();

        return new PagedResult<OrderSummaryDto>
        {
            Items      = pagedItems,
            TotalCount = orders.Count,
            Page       = query.Page,
            PageSize   = query.PageSize
        };
    }
}
