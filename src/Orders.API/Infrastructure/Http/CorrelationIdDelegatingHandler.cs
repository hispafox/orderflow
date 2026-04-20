using System.Diagnostics;

namespace Orders.API.Infrastructure.Http;

/// <summary>
/// DelegatingHandler que propaga el X-Correlation-Id a todas las llamadas HTTP salientes.
/// Toma el ID del HttpContext (request entrante) o del Activity actual (OpenTelemetry).
/// Así los logs de Products.API están correlacionados con los de Orders.API.
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CorrelationIdDelegatingHandler> _logger;

    public CorrelationIdDelegatingHandler(
        IHttpContextAccessor                    httpContextAccessor,
        ILogger<CorrelationIdDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger              = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken  ct)
    {
        var correlationId =
            _httpContextAccessor.HttpContext?
                .Items["CorrelationId"]?.ToString()
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");

        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationId);

        _logger.LogDebug(
            "Propagating CorrelationId {CorrelationId} to {Uri}",
            correlationId, request.RequestUri);

        return await base.SendAsync(request, ct);
    }
}
