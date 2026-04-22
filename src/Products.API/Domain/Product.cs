namespace Products.API.Domain;

public class Product
{
    public Guid     Id          { get; private set; }
    public string   Name        { get; private set; } = null!;
    public string   Description { get; private set; } = null!;
    public decimal  Price       { get; private set; }
    public string   Currency    { get; private set; } = null!;
    public int      Stock       { get; private set; }
    public bool     IsActive    { get; private set; }
    public Guid     CategoryId  { get; private set; }
    public DateTime CreatedAt   { get; private set; }
    public DateTime UpdatedAt   { get; private set; }

    private Product() { }

    internal static Product ForSeed(
        Guid id, string name, string description,
        decimal price, string currency, int initialStock, Guid categoryId)
        => new()
        {
            Id          = id,
            Name        = name.Trim(),
            Description = description,
            Price       = price,
            Currency    = currency.ToUpperInvariant(),
            Stock       = initialStock,
            IsActive    = true,
            CategoryId  = categoryId,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

    public static Product Create(
        string name,
        string description,
        decimal price,
        string currency,
        int initialStock,
        Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required");
        if (price <= 0)
            throw new ArgumentException("Price must be positive");
        if (initialStock < 0)
            throw new ArgumentException("Stock cannot be negative");
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category is required");

        return new Product
        {
            Id          = Guid.NewGuid(),
            Name        = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price       = price,
            Currency    = currency.ToUpperInvariant(),
            Stock       = initialStock,
            IsActive    = true,
            CategoryId  = categoryId,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };
    }

    public bool HasSufficientStock(int quantity) => IsActive && Stock >= quantity;

    public void ReserveStock(int quantity, Guid orderId)
    {
        if (!HasSufficientStock(quantity))
            throw new InvalidOperationException(
                $"Insufficient stock: {Stock} available, {quantity} requested");
        Stock    -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int newStock)
    {
        if (newStock < 0)
            throw new ArgumentException("Stock cannot be negative");
        Stock     = newStock;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
