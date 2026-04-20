using System.ComponentModel.DataAnnotations;

namespace Orders.API.API.DTOs.Requests;

public record CreateOrderRequest
{
    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    [MaxLength(50, ErrorMessage = "Order cannot have more than 50 items")]
    public IList<CreateOrderItemRequest> Items { get; init; } = [];

    [Required]
    public CreateOrderShippingRequest ShippingAddress { get; init; } = null!;
}

public record CreateOrderItemRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; init; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int Quantity { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive")]
    public decimal UnitPrice { get; init; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code")]
    public string Currency { get; init; } = "EUR";
}

public record CreateOrderShippingRequest
{
    [Required]
    [MaxLength(200)]
    public string Street { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ZipCode { get; init; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a 2-letter ISO code")]
    public string Country { get; init; } = string.Empty;
}
