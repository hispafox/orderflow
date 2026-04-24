using MassTransit;

namespace Orders.API.Infrastructure.Messaging;

/// <summary>
/// Observer de publicaciones MassTransit. Cada mensaje que publica Orders.API
/// (directamente o vía saga / outbox) se inserta como fila Direction=Published
/// en [orders].[DemoEventLog].
/// </summary>
public sealed class DemoEventPublishObserver : IPublishObserver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoEventPublishObserver> _logger;

    public DemoEventPublishObserver(
        IServiceScopeFactory                scopeFactory,
        ILogger<DemoEventPublishObserver>   logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public Task PrePublish<T>(PublishContext<T> context) where T : class => Task.CompletedTask;

    public async Task PostPublish<T>(PublishContext<T> context) where T : class
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDemoEventLogRepository>();
            await repo.InsertAsync(new DemoEventLog
            {
                OccurredAt         = DateTime.UtcNow,
                Direction          = "Published",
                MessageType        = typeof(T).FullName ?? typeof(T).Name,
                DestinationAddress = context.DestinationAddress?.ToString(),
                SourceAddress      = context.SourceAddress?.ToString(),
                MessageId          = context.MessageId,
                CorrelationId      = context.CorrelationId,
                ConversationId     = context.ConversationId,
                ServiceName        = "orders-api"
            });
        }
        catch (Exception ex)
        {
            // Fire-and-forget demo logger: nunca tumbar el bus real.
            _logger.LogWarning(ex,
                "No se pudo registrar DemoEventLog para Publish de {MessageType}", typeof(T).FullName);
        }
    }

    public Task PublishFault<T>(PublishContext<T> context, Exception exception) where T : class
        => Task.CompletedTask;
}
