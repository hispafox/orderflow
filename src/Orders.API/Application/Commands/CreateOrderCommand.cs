using MediatR;
using Orders.API.API.DTOs.Responses;

namespace Orders.API.Application.Commands;

public record CreateOrderCommand(
    Guid                      CustomerId,
    IList<CreateOrderItemDto> Items,
    CreateOrderAddressDto     ShippingAddress) : IRequest<OrderDto>;

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
