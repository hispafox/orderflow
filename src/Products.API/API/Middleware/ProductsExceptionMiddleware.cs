using Microsoft.AspNetCore.Mvc;

namespace Products.API.API.Middleware;

public class ProductsExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProductsExceptionMiddleware> _logger;

    public ProductsExceptionMiddleware(RequestDelegate next, ILogger<ProductsExceptionMiddleware> logger)
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient stock"))
        {
            _logger.LogWarning("Insufficient stock: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 422,
                Title  = "Insufficient stock",
                Detail = ex.Message,
                Type   = "https://orderflow.api/errors/insufficient-stock"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 422,
                Title  = "Invalid operation",
                Detail = ex.Message,
                Type   = "https://orderflow.api/errors/invalid-argument"
            });
        }
    }
}
