using MediatR;
using Orders.API.API.DTOs.Responses;

namespace Orders.API.Application.Queries;

public record GetOrderByIdQuery(
    Guid  OrderId,
    Guid? RequestingUserId = null,
    bool  IsAdmin          = false) : IRequest<OrderDto?>;
