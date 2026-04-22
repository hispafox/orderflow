namespace OrderFlow.Contracts.Events.Orders;

/// <summary>
/// Evento publicado por la Saga cuando el flujo falla (stock insuficiente o pago rechazado).
/// Consumers: Notifications.API (email de disculpa al cliente)
/// </summary>
public record OrderFailed
{
    public Guid   OrderId { get; init; }
    public string Reason  { get; init; } = string.Empty;
}
