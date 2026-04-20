using MediatR;
using Orders.API.API.DTOs.Responses;
using Orders.API.API.Mappings;
using Orders.API.Application.Interfaces;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Interfaces;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.Application.Commands;

/// <summary>
/// Handler que crea un pedido.
/// Orquesta: dominio → repositorio → domain events.
/// Sin lógica de negocio propia — la lógica vive en Order.Create().
/// </summary>
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IEventPublisher  _publisher;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository            repository,
        IEventPublisher             publisher,
        ILogger<CreateOrderHandler> logger)
    {
        _repository = repository;
        _publisher  = publisher;
        _logger     = logger;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        _logger.LogInformation(
            "Creating order for customer {CustomerId}",
            command.CustomerId);

        var items = command.Items.Select(i => (
            ProductId:   i.ProductId,
            ProductName: i.ProductName,
            Quantity:    i.Quantity,
            UnitPrice:   new Money(i.UnitPrice, i.Currency)));

        var address = new Address(
            command.ShippingAddress.Street,
            command.ShippingAddress.City,
            command.ShippingAddress.ZipCode,
            command.ShippingAddress.Country);

        var order = Order.Create(command.CustomerId, items, address);

        await _repository.SaveAsync(order, ct);

        foreach (var domainEvent in order.DomainEvents)
            await _publisher.PublishAsync(domainEvent, ct);

        order.ClearDomainEvents();

        _logger.LogInformation(
            "Order {OrderId} created. Total: {Total}",
            order.Id, order.Total);

        return order.ToDto();
    }
}
