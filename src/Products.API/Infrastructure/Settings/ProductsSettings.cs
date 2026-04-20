using System.ComponentModel.DataAnnotations;

namespace Products.API.Infrastructure.Settings;

public class ProductsSettings
{
    public const string SectionName = "Products";

    [Range(1, 1_000_000)]
    public int MaxStock { get; set; } = 100_000;

    [Range(0, 1_000)]
    public int LowStockThreshold { get; set; } = 10;

    public bool AllowBackorders { get; set; } = false;

    [Required, StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; set; } = "EUR";
}
