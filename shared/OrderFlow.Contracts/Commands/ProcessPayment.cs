namespace OrderFlow.Contracts.Commands;

/// <summary>
/// Comando que la Saga envía a Payments.API para procesar el cobro.
/// Point-to-point: Payments es el único receptor.
/// </summary>
public record ProcessPayment
{
    public Guid    OrderId    { get; init; }
    public Guid    CustomerId { get; init; }
    public decimal Amount     { get; init; }
    public string  Currency   { get; init; } = "EUR";
}
