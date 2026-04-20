namespace Products.API.Features.ReserveStock;

public record ReserveStockRequest(Guid OrderId, int Quantity);
