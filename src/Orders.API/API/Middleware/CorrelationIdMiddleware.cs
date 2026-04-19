using System.Diagnostics;
using Serilog.Context;

namespace Orders.API.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader]
                                .FirstOrDefault()
                            ?? Activity.Current?.TraceId.ToString()
                            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Items["CorrelationId"] = correlationId;
            await _next(context);
        }
    }
}
