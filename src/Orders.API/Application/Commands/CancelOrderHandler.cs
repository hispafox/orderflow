using MediatR;
using Orders.API.Application.Exceptions;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Commands;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _repository;

    public CancelOrderHandler(IOrderRepository repository) => _repository = repository;

    public async Task Handle(CancelOrderCommand command, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, ct)
            ?? throw new OrderNotFoundException(command.OrderId);

        order.Cancel(command.Reason);
        await _repository.SaveAsync(order, ct);
    }
}
