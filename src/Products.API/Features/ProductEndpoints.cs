using Products.API.Features.CreateProduct;
using Products.API.Features.GetProduct;
using Products.API.Features.ListProducts;
using Products.API.Features.ReserveStock;
using Products.API.Features.UpdateStock;

namespace Products.API.Features;

/// <summary>
/// Route Group de Products — equivalente a un Controller sin herencia de clase.
/// Prefijo /api/products + tag OpenAPI compartido para todos los endpoints.
/// </summary>
public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", ListProductsEndpoint.Handle)
            .WithName("ListProducts")
            .WithSummary("Lista productos del catálogo");

        group.MapGet("/{id:guid}", GetProductEndpoint.Handle)
            .WithName("GetProduct")
            .WithSummary("Obtiene el detalle de un producto");

        group.MapPost("/", CreateProductEndpoint.Handle)
            .WithName("CreateProduct")
            .WithSummary("Crea un nuevo producto en el catálogo");

        group.MapPut("/{id:guid}/stock", UpdateStockEndpoint.Handle)
            .WithName("UpdateStock")
            .WithSummary("Actualiza el stock de un producto");

        group.MapPost("/{id:guid}/reserve", ReserveStockEndpoint.Handle)
            .WithName("ReserveStock")
            .WithSummary("Reserva stock para un pedido");

        return app;
    }
}
