namespace Orders.API.API.DTOs.Responses;

public record OrderDto
{
    public Guid      Id                 { get; init; }
    public Guid      CustomerId         { get; init; }
    public string    Status             { get; init; } = string.Empty;
    public decimal   Total              { get; init; }
    public string    Currency           { get; init; } = string.Empty;
    public DateTime  CreatedAt          { get; init; }
    public DateTime? ConfirmedAt        { get; init; }
    public DateTime? CancelledAt        { get; init; }
    public string?   CancellationReason { get; init; }
    public IList<OrderLineDto> Lines           { get; init; } = [];
    public OrderAddressDto     ShippingAddress { get; init; } = null!;
}

public record OrderSummaryDto
{
    public Guid     Id         { get; init; }
    public Guid     CustomerId { get; init; }
    public string   Status     { get; init; } = string.Empty;
    public decimal  Total      { get; init; }
    public string   Currency   { get; init; } = string.Empty;
    public int      LineCount  { get; init; }
    public DateTime CreatedAt  { get; init; }
}

public record OrderLineDto
{
    public Guid    Id          { get; init; }
    public Guid    ProductId   { get; init; }
    public string  ProductName { get; init; } = string.Empty;
    public int     Quantity    { get; init; }
    public decimal UnitPrice   { get; init; }
    public decimal LineTotal   { get; init; }
}

public record OrderAddressDto
{
    public string Street  { get; init; } = string.Empty;
    public string City    { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}
