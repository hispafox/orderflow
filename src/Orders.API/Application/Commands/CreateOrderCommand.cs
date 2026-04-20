using MediatR;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Behaviors;

namespace Orders.API.Application.Commands;

public record CreateOrderCommand(
    Guid                              CustomerId,
    string                            CustomerEmail,
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
