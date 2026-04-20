using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Polly.CircuitBreaker;

namespace Orders.API.Infrastructure.Http;

/// <summary>
/// Cliente HTTP tipado para Products.API.
/// La resiliencia (Polly v8) está configurada en el HttpClient del DI container.
/// Implementa caché local con TTL de 5 min para precios (cambio lento).
/// El stock no se cachea — es un dato de alta volatilidad.
/// </summary>
public class ProductsClient
{
    private readonly HttpClient  _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductsClient> _logger;

    public ProductsClient(
        HttpClient               httpClient,
        IMemoryCache             cache,
        ILogger<ProductsClient>  logger)
    {
        _httpClient = httpClient;
        _cache      = cache;
        _logger     = logger;
    }

    /// <summary>
    /// Obtiene el detalle de un producto.
    /// Cachea precio y datos estáticos 5 min. Stock no se cachea.
    /// </summary>
    public virtual async Task<ProductDetailDto?> GetProductAsync(
        Guid productId,
        CancellationToken ct = default)
    {
        var cacheKey = $"product:{productId}";

        if (_cache.TryGetValue(cacheKey, out ProductDetailDto? cached))
        {
            _logger.LogDebug("Cache hit for product {ProductId}", productId);
            return cached;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/products/{productId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product {ProductId} not found in Products.API", productId);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return GetFromCacheOrNull(cacheKey, productId);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Products.API returned {StatusCode} for {ProductId}",
                    response.StatusCode, productId);
                return null;
            }

            var product = await response.Content
                .ReadFromJsonAsync<ProductDetailDto>(ct);

            if (product is not null)
            {
                _cache.Set(cacheKey, product, TimeSpan.FromMinutes(5));
                _logger.LogDebug(
                    "Product {ProductId} cached — {Price} {Currency}",
                    productId, product.Price, product.Currency);
            }

            return product;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex,
                "Circuit open — Products.API unavailable for {ProductId}. Using cache.",
                productId);
            return GetFromCacheOrNull(cacheKey, productId);
        }
    }

    /// <summary>
    /// Reserva stock para un pedido.
    /// No cacheable: operación de escritura, resultado siempre fresco.
    /// </summary>
    public virtual async Task<bool> ReserveStockAsync(
        Guid productId,
        Guid orderId,
        int  quantity,
        CancellationToken ct = default)
    {
        var request = new { OrderId = orderId, Quantity = quantity };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/products/{productId}/reserve", request, ct);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            _logger.LogWarning(
                "Insufficient stock for product {ProductId} (requested {Quantity})",
                productId, quantity);
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>
    /// Obtiene múltiples productos en una sola llamada.
    /// Evita el problema N+1 cuando el pedido tiene muchas líneas.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, ProductDetailDto>> GetProductsBatchAsync(
        IEnumerable<Guid> productIds,
        CancellationToken ct = default)
    {
        var ids    = productIds.Distinct().ToList();
        var result = new Dictionary<Guid, ProductDetailDto>();

        var missing = new List<Guid>();
        foreach (var id in ids)
        {
            if (_cache.TryGetValue($"product:{id}", out ProductDetailDto? cachedItem) && cachedItem is not null)
                result[id] = cachedItem;
            else
                missing.Add(id);
        }

        if (missing.Count == 0) return result;

        var idsQuery = string.Join(",", missing);
        var response = await _httpClient.GetAsync(
            $"/api/products/batch?ids={idsQuery}", ct);

        if (!response.IsSuccessStatusCode)
            return result;

        var products = await response.Content
            .ReadFromJsonAsync<List<ProductDetailDto>>(ct);

        if (products is null) return result;

        foreach (var product in products)
        {
            result[product.Id] = product;
            _cache.Set($"product:{product.Id}", product, TimeSpan.FromMinutes(5));
        }

        return result;
    }

    private ProductDetailDto? GetFromCacheOrNull(string cacheKey, Guid productId)
    {
        if (_cache.TryGetValue(cacheKey, out ProductDetailDto? cachedItem))
        {
            _logger.LogInformation(
                "Using cached data for product {ProductId} (may be stale up to 5 min)",
                productId);
            return cachedItem;
        }

        _logger.LogError(
            "Circuit open and no cache for product {ProductId} — cannot verify",
            productId);
        return null;
    }
}
