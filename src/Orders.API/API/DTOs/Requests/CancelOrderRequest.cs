using System.ComponentModel.DataAnnotations;

namespace Orders.API.API.DTOs.Requests;

public record CancelOrderRequest
{
    [Required(ErrorMessage = "Cancellation reason is required")]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
