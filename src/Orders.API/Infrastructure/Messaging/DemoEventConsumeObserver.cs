using MassTransit;

namespace Orders.API.Infrastructure.Messaging;

/// <summary>
/// Observer de consumos MassTransit. Cada mensaje que Orders.API consume
/// (Saga, Projectors o Consumers) se registra como Direction=Consumed en
/// [orders].[DemoEventLog].
/// </summary>
public sealed class DemoEventConsumeObserver : IConsumeObserver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoEventConsumeObserver> _logger;

    public DemoEventConsumeObserver(
        IServiceScopeFactory                scopeFactory,
        ILogger<DemoEventConsumeObserver>   logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public Task PreConsume<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;

    public async Task PostConsume<T>(ConsumeContext<T> context) where T : class
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDemoEventLogRepository>();
            await repo.InsertAsync(new DemoEventLog
            {
                OccurredAt         = DateTime.UtcNow,
                Direction          = "Consumed",
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
            _logger.LogWarning(ex,
                "No se pudo registrar DemoEventLog para Consume de {MessageType}", typeof(T).FullName);
        }
    }

    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
        => Task.CompletedTask;
}
