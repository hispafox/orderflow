namespace Orders.API.Application.Queries;

public record OrderSearchQuery
{
    public Guid?     CustomerId   { get; init; }
    public string?   Status       { get; init; }
    public DateTime? From         { get; init; }
    public DateTime? To           { get; init; }
    public string?   ShippingCity { get; init; }
    public decimal?  MinTotal     { get; init; }
    public int       Page         { get; init; } = 1;
    public int       PageSize     { get; init; } = 20;
}
