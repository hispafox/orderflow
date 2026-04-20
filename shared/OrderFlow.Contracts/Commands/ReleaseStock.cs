namespace OrderFlow.Contracts.Commands;

/// <summary>
/// Comando de compensación: Saga pide a Products liberar el stock reservado.
/// Se envía cuando el pago falla — deshace la reserva de stock.
/// </summary>
public record ReleaseStock
{
    public Guid OrderId   { get; init; }
    public Guid ProductId { get; init; }
    public int  Quantity  { get; init; }
}
