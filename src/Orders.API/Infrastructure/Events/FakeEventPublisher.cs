using Orders.API.Application.Interfaces;

namespace Orders.API.Infrastructure.Events;

/// <summary>
/// Implementación Fake de IEventPublisher para desarrollo local.
/// Loggea los eventos sin enviarlos a ningún broker.
/// En M4.2 se reemplaza por MassTransitEventPublisher.
/// </summary>
public class FakeEventPublisher : IEventPublisher
{
    private readonly ILogger<FakeEventPublisher> _logger;

    public FakeEventPublisher(ILogger<FakeEventPublisher> logger)
        => _logger = logger;

    public Task PublishAsync<T>(T @event, CancellationToken ct = default)
        where T : class
    {
        _logger.LogInformation(
            "[FakeEventPublisher] Event published (stub): {EventType} {@Event}",
            typeof(T).Name, @event);
        return Task.CompletedTask;
    }
}
