using System.ComponentModel.DataAnnotations;

namespace Orders.API.Application.Settings;

public class OrdersSettings
{
    public const string SectionName = "Orders";

    [Range(1, 100, ErrorMessage = "MaxLinesPerOrder debe estar entre 1 y 100")]
    public int MaxLinesPerOrder { get; set; } = 50;

    [Range(0.01, double.MaxValue, ErrorMessage = "MinOrderAmount debe ser mayor que 0")]
    public decimal MinOrderAmount { get; set; } = 10.00m;

    public bool AllowBackorders { get; set; } = false;

    [Range(1, 3650, ErrorMessage = "RetentionDays debe estar entre 1 y 3650 días")]
    public int RetentionDays { get; set; } = 365;

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency debe ser un código ISO de 3 letras")]
    public string Currency { get; set; } = "EUR";
}
