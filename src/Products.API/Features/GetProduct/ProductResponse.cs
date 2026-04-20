namespace Products.API.Features.GetProduct;

public record ProductResponse(
    Guid     Id,
    string   Name,
    string   Description,
    decimal  Price,
    string   Currency,
    int      Stock,
    bool     IsActive,
    Guid     CategoryId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
