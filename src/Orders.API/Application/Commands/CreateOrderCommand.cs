using MediatR;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Behaviors;
using Orders.API.Application.Interfaces;

namespace Orders.API.Application.Commands;

public record CreateOrderCommand(
    Guid                              CustomerId,
    string                            CustomerEmail,
    IReadOnlyList<CreateOrderItemDto> Items,
    CreateOrderAddressDto             ShippingAddress)
    : IRequest<OrderDto>, ITransactional, IAuditable
{
    public Guid   ActorId      => CustomerId;
    public string ActorEmail   => CustomerEmail;
    public string ResourceType => "Order";
    public Guid?  ResourceId   => null;
}

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
