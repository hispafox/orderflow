namespace OrderFlow.Contracts.Events.Orders;

/// <summary>
/// Evento publicado por la Saga cuando el flujo completo termina con éxito.
/// Consumers: Notifications.API (email de confirmación de pago)
/// </summary>
public record OrderConfirmed
{
    public Guid     OrderId       { get; init; }
    public Guid     CustomerId    { get; init; }
    public string   CustomerEmail { get; init; } = string.Empty;
    public Guid     PaymentId     { get; init; }
    public decimal  Total         { get; init; }
    public string   Currency      { get; init; } = "EUR";
    public DateTime ConfirmedAt   { get; init; }
}
