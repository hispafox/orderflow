using MassTransit;

namespace Orders.API.Sagas;

/// <summary>
/// Mensaje de timeout para la Saga.
/// Si Products.API no responde en 5 minutos → la Saga falla con timeout.
/// </summary>
public record OrderSagaTimeout : CorrelatedBy<Guid>
{
    public Guid OrderId       { get; init; }
    public Guid CorrelationId => OrderId;
}
