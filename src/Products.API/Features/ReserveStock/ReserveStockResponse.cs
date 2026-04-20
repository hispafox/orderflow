namespace Products.API.Features.ReserveStock;

public record ReserveStockResponse(
    Guid ProductId,
    int  ReservedQuantity,
    int  RemainingStock);
