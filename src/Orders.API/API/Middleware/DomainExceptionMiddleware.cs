using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Orders.API.Application.Exceptions;
using Orders.API.Domain.Exceptions;

namespace Orders.API.API.Middleware;

/// <summary>
/// Captura excepciones del dominio y de la capa de aplicación
/// y las convierte en respuestas HTTP semánticas.
/// Debe ser el primer middleware del pipeline.
///
/// - ValidationException    → 422 con detalles de cada campo
/// - DomainException        → 422 con el mensaje de la regla de negocio
/// - OrderNotFoundException → 404
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
        catch (ValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed: {Errors}",
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await context.Response.WriteAsJsonAsync(
                new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title  = "Validation failed",
                    Type   = "https://orderflow.api/errors/validation"
                });
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
