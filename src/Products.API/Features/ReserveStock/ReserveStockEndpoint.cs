using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Products.API.Domain.Interfaces;

namespace Products.API.Features.ReserveStock;

public static class ReserveStockEndpoint
{
    public static async Task<Results<Ok<ReserveStockResponse>, NotFound, UnprocessableEntity<ProblemDetails>>> Handle(
        Guid                id,
        ReserveStockRequest request,
        IProductRepository  repository,
        ILoggerFactory      loggerFactory,
        CancellationToken   ct = default)
    {
        var product = await repository.GetByIdAsync(id, ct);
        if (product is null)
            return TypedResults.NotFound();

        if (!product.HasSufficientStock(request.Quantity))
            return TypedResults.UnprocessableEntity(new ProblemDetails
            {
                Title  = "Insufficient stock",
                Detail = $"Product has {product.Stock} units, requested {request.Quantity}",
                Status = 422
            });

        product.ReserveStock(request.Quantity, request.OrderId);
        await repository.SaveAsync(product, ct);

        loggerFactory.CreateLogger(nameof(ReserveStockEndpoint))
            .LogInformation("Reserved {Quantity} units of product {ProductId} for order {OrderId}",
                request.Quantity, id, request.OrderId);

        return TypedResults.Ok(new ReserveStockResponse(
            ProductId:        id,
            ReservedQuantity: request.Quantity,
            RemainingStock:   product.Stock));
    }
}
