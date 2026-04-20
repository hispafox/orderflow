using MediatR;
using Orders.API.Application.Exceptions;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Commands;

public class ConfirmOrderHandler : IRequestHandler<ConfirmOrderCommand>
{
    private readonly IOrderRepository _repository;

    public ConfirmOrderHandler(IOrderRepository repository) => _repository = repository;

    public async Task Handle(ConfirmOrderCommand command, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(command.OrderId, ct)
            ?? throw new OrderNotFoundException(command.OrderId);

        order.Confirm();
        await _repository.SaveAsync(order, ct);
    }
}
