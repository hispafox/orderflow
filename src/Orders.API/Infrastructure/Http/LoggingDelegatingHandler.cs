using System.Diagnostics;

namespace Orders.API.Infrastructure.Http;

/// <summary>
/// DelegatingHandler que loggea cada petición HTTP saliente.
/// Registra método, URI, status code y duración.
/// Loggea errores de red como Error (no solo Warning).
/// </summary>
public class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler> _logger;

    public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
        => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken  ct)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "HTTP {Method} {Uri}",
            request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, ct);
            sw.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Uri} → {StatusCode} ({ElapsedMs}ms)",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "HTTP {Method} {Uri} FAILED after {ElapsedMs}ms",
                request.Method, request.RequestUri, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
