using Microsoft.Extensions.Caching.Memory;

namespace Gateway.API.Services;

/// Contador de fallos de autenticación en memoria.
/// Ventana deslizante de 1 minuto por IP.
/// En producción con múltiples instancias: reemplazar por Redis.
public class MemoryAuthFailureCounter : IAuthFailureCounter
{
    private readonly IMemoryCache _cache;

    public MemoryAuthFailureCounter(IMemoryCache cache) => _cache = cache;

    public Task<int> IncrementAsync(string key, CancellationToken ct = default)
    {
        var cacheKey = $"auth-fail:{key}";
        var count    = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });
        count++;
        _cache.Set(cacheKey, count, TimeSpan.FromMinutes(1));
        return Task.FromResult(count);
    }
}
