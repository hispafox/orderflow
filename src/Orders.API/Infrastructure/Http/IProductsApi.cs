using Orders.API.API.DTOs;
using Refit;

namespace Orders.API.Infrastructure.Http;

/// <summary>
/// Interfaz Refit para Products.API.
/// Refit genera la implementación automáticamente en tiempo de compilación.
/// OrderFlow usa ProductsClient (manual) por mayor control y visibilidad.
/// Esta interfaz se incluye como referencia del patrón declarativo.
///
/// Para activar Refit en lugar de ProductsClient:
///   Program.cs: AddRefitClient&lt;IProductsApi&gt;() en vez de AddHttpClient&lt;ProductsClient&gt;()
///   Handler: inyectar IProductsApi en vez de ProductsClient
/// </summary>
public interface IProductsApi
{
    [Get("/api/products/{id}")]
    Task<ProductDetailDto?> GetProductAsync(
        Guid id,
        CancellationToken ct = default);

    [Get("/api/products")]
    Task<PagedResult<ProductDetailDto>> ListProductsAsync(
        [AliasAs("page")]     int page     = 1,
        [AliasAs("pageSize")] int pageSize = 20,
        CancellationToken       ct         = default);

    [Post("/api/products/{id}/reserve")]
    Task<ReserveStockResponse> ReserveStockAsync(
        Guid              id,
        [Body] ReserveStockRequest request,
        CancellationToken ct = default);
}

public record ReserveStockRequest(Guid OrderId, int Quantity);
public record ReserveStockResponse(bool Success, int RemainingStock, string? ErrorMessage);
