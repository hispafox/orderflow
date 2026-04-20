using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Products.API.Domain;
using Products.API.Domain.Interfaces;
using Products.API.Features.GetProduct;

namespace Products.API.Features.CreateProduct;

public static class CreateProductEndpoint
{
    public static async Task<Results<Created<ProductResponse>, ValidationProblem>> Handle(
        CreateProductRequest             request,
        IValidator<CreateProductRequest> validator,
        IProductRepository               repository,
        LinkGenerator                    links,
        CancellationToken                ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.Currency,
            request.InitialStock,
            request.CategoryId);

        await repository.SaveAsync(product, ct);

        var location = links.GetPathByName("GetProduct", new { id = product.Id });
        return TypedResults.Created(location!, product.ToResponse());
    }
}
