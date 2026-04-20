using MediatR;
using Orders.API.API.DTOs.Responses;

namespace Orders.API.Application.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;
