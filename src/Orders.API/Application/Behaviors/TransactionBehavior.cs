using MediatR;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Application.Behaviors;

/// <summary>
/// Behavior de transacción que solo aplica a Commands marcados con ITransactional.
/// Las Queries pasan directamente al handler sin overhead de transacción.
/// Si el handler lanza excepción → Rollback automático.
/// Si el handler termina bien → Commit automático.
/// </summary>
public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactional
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        OrderDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();
            await transaction.CommitAsync(ct);
            _logger.LogDebug(
                "Transaction committed for {Request}",
                typeof(TRequest).Name);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(
                "Transaction rolled back for {Request}",
                typeof(TRequest).Name);
            throw;
        }
    }
}
