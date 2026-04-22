using MediatR;
using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Interfaces;

namespace Orders.API.Application.Queries;

public class ListOrdersHandler : IRequestHandler<ListOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderReadRepository _readRepository;

    public ListOrdersHandler(IOrderReadRepository readRepository)
        => _readRepository = readRepository;

    public Task<PagedResult<OrderSummaryDto>> Handle(
        ListOrdersQuery   query,
        CancellationToken ct)
    {
        var customerIdFilter = query.IsAdmin
            ? query.CustomerId
            : (query.UserId is not null ? Guid.Parse(query.UserId) : query.CustomerId);

        return _readRepository.ListAsync(
            customerId: customerIdFilter,
            status:     query.Status,
            page:       query.Page,
            pageSize:   query.PageSize,
            ct:         ct);
    }
}
