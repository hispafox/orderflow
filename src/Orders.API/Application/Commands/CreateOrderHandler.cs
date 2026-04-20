using MediatR;
using Orders.API.API.DTOs.Responses;
using Orders.API.API.Mappings;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Interfaces;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Application.Commands;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;

    public CreateOrderHandler(IOrderRepository repository) => _repository = repository;

    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var items = command.Items.Select(i => (
            ProductId:   i.ProductId,
            ProductName: i.ProductName,
            Quantity:    i.Quantity,
            UnitPrice:   new Money(i.UnitPrice, i.Currency)
        ));

        var address = new Address(
            command.ShippingAddress.Street,
            command.ShippingAddress.City,
            command.ShippingAddress.ZipCode,
            command.ShippingAddress.Country);

        var order = Order.Create(command.CustomerId, items, address);
        await _repository.SaveAsync(order, ct);

        return order.ToDto();
    }
}
