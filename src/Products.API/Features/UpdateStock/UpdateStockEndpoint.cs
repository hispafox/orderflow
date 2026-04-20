using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Products.API.Domain.Interfaces;

namespace Products.API.Features.UpdateStock;

public static class UpdateStockEndpoint
{
    public static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        Guid                           id,
        UpdateStockRequest             request,
        IValidator<UpdateStockRequest> validator,
        IProductRepository             repository,
        ILoggerFactory                 loggerFactory,
        CancellationToken              ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var product = await repository.GetByIdAsync(id, ct);
        if (product is null)
            return TypedResults.NotFound();

        product.UpdateStock(request.NewStock);
        await repository.SaveAsync(product, ct);

        loggerFactory.CreateLogger(nameof(UpdateStockEndpoint))
            .LogInformation("Stock updated for product {ProductId}. New stock: {Stock}", id, request.NewStock);

        return TypedResults.NoContent();
    }
}
