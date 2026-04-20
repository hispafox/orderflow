using MediatR;

namespace Orders.API.Application.Commands;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest;
