using MediatR;
using Orders.API.Application.Behaviors;

namespace Orders.API.Application.Commands;

public record ConfirmOrderCommand(Guid OrderId) : IRequest, ITransactional;
