namespace Orders.API.Infrastructure.Http;

/// <summary>
/// DTO con los datos que Orders.API necesita de Products.API.
/// Solo las propiedades que Orders usa — no el objeto completo de Products.
/// Patrón Anti-Corruption Layer: Orders define su propio contrato.
/// Si Products.API cambia su modelo, solo cambia este DTO.
/// </summary>
public record ProductDetailDto(
    Guid    Id,
    string  Name,
    decimal Price,
    string  Currency,
    int     Stock,
    bool    IsActive,
    Guid    CategoryId,
    string?  Brand         = null,
    decimal? OriginalPrice = null);
