using Orders.API.API.DTOs.Responses;
using Orders.API.Domain.Entities;
using Orders.API.Domain.ValueObjects;

namespace Orders.API.API.Mappings;

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order) => new()
    {
        Id                 = order.Id,
        CustomerId         = order.CustomerId,
        Status             = order.Status.Value,
        Total              = order.Total.Amount,
        Currency           = order.Total.Currency,
        CreatedAt          = order.CreatedAt,
        ConfirmedAt        = order.ConfirmedAt,
        CancelledAt        = order.CancelledAt,
        CancellationReason = order.CancellationReason,
        Lines              = order.Lines.Select(l => l.ToDto()).ToList(),
        ShippingAddress    = order.ShippingAddress.ToDto()
    };

    public static OrderSummaryDto ToSummaryDto(this Order order) => new()
    {
        Id         = order.Id,
        CustomerId = order.CustomerId,
        Status     = order.Status.Value,
        Total      = order.Total.Amount,
        Currency   = order.Total.Currency,
        LineCount  = order.Lines.Count,
        CreatedAt  = order.CreatedAt
    };

    public static OrderLineDto ToDto(this OrderLine line) => new()
    {
        Id          = line.Id,
        ProductId   = line.ProductId,
        ProductName = line.ProductName,
        Quantity    = line.Quantity,
        UnitPrice   = line.UnitPrice.Amount,
        LineTotal   = line.LineTotal.Amount
    };

    public static OrderAddressDto ToDto(this Address address) => new()
    {
        Street  = address.Street,
        City    = address.City,
        ZipCode = address.ZipCode,
        Country = address.Country
    };
}
