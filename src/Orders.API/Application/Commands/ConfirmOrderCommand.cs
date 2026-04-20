using MediatR;

namespace Orders.API.Application.Commands;

public record ConfirmOrderCommand(Guid OrderId) : IRequest;
