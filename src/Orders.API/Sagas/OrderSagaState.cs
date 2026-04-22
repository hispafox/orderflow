using MassTransit;

namespace Orders.API.Sagas;

/// <summary>
/// Estado persistido de la Saga de pedidos.
/// CorrelationId = OrderId — permite cargar la instancia correcta al llegar un evento.
/// RowVersion garantiza concurrencia optimista: dos procesadores del mismo mensaje
/// solo uno puede hacer COMMIT — el otro reintenta.
/// </summary>
public class OrderSagaState : SagaStateMachineInstance
{
    public Guid   CorrelationId { get; set; }  // = OrderId
    public string CurrentState  { get; set; } = string.Empty;

    public Guid    CustomerId    { get; set; }
    public string  CustomerEmail { get; set; } = string.Empty;
    public Guid    ProductId     { get; set; }
    public int     Quantity      { get; set; }
    public decimal Amount        { get; set; }
    public string  Currency      { get; set; } = "EUR";

    public Guid?    PaymentId     { get; set; }
    public string?  FailureReason { get; set; }
    public DateTime CreatedAt     { get; set; }
    public DateTime? CompletedAt  { get; set; }

    public Guid? ReservationTimeoutTokenId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
