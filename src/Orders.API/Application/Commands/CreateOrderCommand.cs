using MediatR;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Behaviors;

namespace Orders.API.Application.Commands;

/// <summary>
/// Command para crear un nuevo pedido.
/// ITransactional → TransactionBehavior envuelve la ejecución en una transacción EF Core.
/// </summary>
public record CreateOrderCommand(
    Guid                              CustomerId,
    IReadOnlyList<CreateOrderItemDto> Items,
    CreateOrderAddressDto             ShippingAddress)
    : IRequest<OrderDto>, ITransactional;

public record CreateOrderItemDto(
    Guid    ProductId,
    string  ProductName,
    int     Quantity,
    decimal UnitPrice,
    string  Currency);

public record CreateOrderAddressDto(
    string Street,
    string City,
    string ZipCode,
    string Country);
