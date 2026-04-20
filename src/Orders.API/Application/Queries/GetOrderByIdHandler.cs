using MediatR;
using Orders.API.API.DTOs.Responses;
using Orders.API.API.Mappings;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Queries;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _repository;

    public GetOrderByIdHandler(IOrderRepository repository) => _repository = repository;

    public async Task<OrderDto?> Handle(GetOrderByIdQuery query, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(query.OrderId, ct);
        return order?.ToDto();
    }
}
