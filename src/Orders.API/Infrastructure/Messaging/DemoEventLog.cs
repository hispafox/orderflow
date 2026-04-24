namespace Orders.API.Infrastructure.Messaging;

/// <summary>
/// Fila de auditoría de eventos / comandos pasando por MassTransit en Orders.API.
/// Poblada por <see cref="DemoEventPublishObserver"/> y <see cref="DemoEventConsumeObserver"/>.
/// Mapeada con EF (ver <see cref="DemoEventLogConfiguration"/>) y consultada con Dapper
/// en <see cref="DemoEventLogRepository"/>.
///
/// Propósito: dar a la demo un historial PERSISTENTE de qué publicó y consumió
/// Orders.API, inmune a la limpieza agresiva del Outbox de MassTransit.
/// </summary>
public sealed record DemoEventLog
{
    public long      Id                 { get; init; }
    public DateTime  OccurredAt         { get; init; }
    public string    Direction          { get; init; } = string.Empty;   // Published | Consumed
    public string    MessageType        { get; init; } = string.Empty;
    public string?   DestinationAddress { get; init; }
    public string?   SourceAddress      { get; init; }
    public Guid?     MessageId          { get; init; }
    public Guid?     CorrelationId      { get; init; }
    public Guid?     ConversationId     { get; init; }
    public string    ServiceName        { get; init; } = "orders-api";
}
