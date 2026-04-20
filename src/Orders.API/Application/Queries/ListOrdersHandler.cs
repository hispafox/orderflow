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
        ListOrdersQuery   query,
        CancellationToken ct)
    {
        var (orders, total) = await _repository.ListAsync(
            status:     query.Status,
            customerId: query.CustomerId,
            page:       query.Page,
            pageSize:   query.PageSize,
            ct:         ct);

        return new PagedResult<OrderSummaryDto>
        {
            Items      = orders.Select(o => o.ToSummaryDto()).ToList(),
            TotalCount = total,
            Page       = query.Page,
            PageSize   = query.PageSize
        };
    }
}
