using System.ComponentModel.DataAnnotations;

namespace Orders.API.API.DTOs;

public record OrderFilterParams
{
    public string? Status     { get; init; }
    public Guid?   CustomerId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page     { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo   { get; init; }
}
