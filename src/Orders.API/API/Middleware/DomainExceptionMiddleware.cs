using Microsoft.AspNetCore.Mvc;
using Orders.API.Application.Exceptions;
using Orders.API.Domain.Exceptions;

namespace Orders.API.API.Middleware;

/// <summary>
/// Captura excepciones del dominio y las convierte en respuestas HTTP semánticas.
/// - DomainException        → 422 Unprocessable Entity
/// - OrderNotFoundException → 404 Not Found
/// </summary>
public class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    public DomainExceptionMiddleware(
        RequestDelegate next,
        ILogger<DomainExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OrderNotFoundException ex)
        {
            _logger.LogWarning("Order not found: {OrderId}", ex.OrderId);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "Order not found",
                Detail = ex.Message,
                Type   = "https://orderflow.api/errors/not-found"
            });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title  = "Business rule violation",
                Detail = ex.Message,
                Type   = "https://orderflow.api/errors/domain-rule-violation"
            });
        }
    }
}
