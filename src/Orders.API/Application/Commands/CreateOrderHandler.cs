using MassTransit;
using MediatR;
using OrderFlow.Contracts.Events.Orders;
using Orders.API.API.DTOs.Responses;
using Orders.API.API.Mappings;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Exceptions;
using Orders.API.Domain.Interfaces;
using Orders.API.Domain.ValueObjects;
using Orders.API.Infrastructure.Http;

namespace Orders.API.Application.Commands;

/// <summary>
/// Handler que crea un pedido y publica el Integration Event OrderCreated a RabbitMQ.
/// IEventPublisher (stub) reemplazado por IPublishEndpoint (MassTransit real).
/// </summary>
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository  _repository;
    private readonly ProductsClient    _productsClient;
    private readonly IPublishEndpoint  _publishEndpoint;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository            repository,
        ProductsClient              productsClient,
        IPublishEndpoint            publishEndpoint,
        ILogger<CreateOrderHandler> logger)
    {
        _repository      = repository;
        _productsClient  = productsClient;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task<OrderDto> Handle(
        CreateOrderCommand command,
        CancellationToken  ct)
    {
        _logger.LogInformation(
            "Creating order for customer {CustomerId} ({Email})",
            command.CustomerId, command.CustomerEmail);

        var validatedItems = new List<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)>();
        foreach (var item in command.Items)
        {
            var product = await _productsClient.GetProductAsync(item.ProductId, ct);

            if (product is null)
                throw new DomainException(
                    $"Product {item.ProductId} is not available or could not be verified");

            if (!product.IsActive)
                throw new DomainException($"Product '{product.Name}' is no longer active");

            if (product.Stock < item.Quantity)
                throw new DomainException(
                    $"Insufficient stock for '{product.Name}'. " +
                    $"Available: {product.Stock}, Requested: {item.Quantity}");

            validatedItems.Add((
                item.ProductId,
                product.Name,
                item.Quantity,
                new Money(product.Price, product.Currency)));
        }

        var address = new Address(
            command.ShippingAddress.Street, command.ShippingAddress.City,
            command.ShippingAddress.ZipCode, command.ShippingAddress.Country);

        var order = Order.Create(command.CustomerId, validatedItems, address);

        await _repository.SaveAsync(order, ct);

        var orderCreated = new OrderCreated
        {
            OrderId       = order.Id,
            CustomerId    = order.CustomerId,
            CustomerEmail = command.CustomerEmail,
            Total         = order.Total.Amount,
            Currency      = order.Total.Currency,
            CreatedAt     = order.CreatedAt,
            Items = order.Lines.Select(l => new OrderCreatedItem(
                l.ProductId, l.ProductName,
                l.Quantity, l.UnitPrice.Amount)).ToList(),
            ShippingAddress = new OrderCreatedAddress(
                order.ShippingAddress.Street, order.ShippingAddress.City,
                order.ShippingAddress.ZipCode, order.ShippingAddress.Country)
        };

        await _publishEndpoint.Publish(orderCreated, ct);

        _logger.LogInformation(
            "Order {OrderId} created and OrderCreated event published",
            order.Id);

        order.ClearDomainEvents();
        return order.ToDto();
    }
}
