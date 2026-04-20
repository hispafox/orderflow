using MediatR;
using Orders.API.Application.Behaviors;

namespace Orders.API.Application.Commands;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest, ITransactional;
