using MediatR;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Interfaces;

namespace Orders.API.Application.Queries;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderReadRepository _readRepository;

    public GetOrderByIdHandler(IOrderReadRepository readRepository)
        => _readRepository = readRepository;

    public Task<OrderDto?> Handle(GetOrderByIdQuery query, CancellationToken ct)
    {
        // Si no se provee user context (llamada interna), permitir acceso total
        var isAdmin = query.IsAdmin || query.RequestingUserId is null;
        var userId  = query.RequestingUserId ?? Guid.Empty;

        return _readRepository.GetByIdAsync(query.OrderId, userId, isAdmin, ct);
    }
}
