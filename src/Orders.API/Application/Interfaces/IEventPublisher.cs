namespace Orders.API.Application.Interfaces;

/// <summary>
/// Puerto de salida para publicar integration events.
/// Implementación real con MassTransit se añade en M4.2.
/// En desarrollo, FakeEventPublisher registra los eventos sin enviarlos.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default)
        where T : class;
}
