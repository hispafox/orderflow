using MediatR;
using Microsoft.EntityFrameworkCore;
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
        // CreateExecutionStrategy wraps the transaction in the retry policy,
        // required when EnableRetryOnFailure is configured on the DbContext.
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(
            state: next,
            operation: async (_, nextDelegate, cancellationToken) =>
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var response = await nextDelegate();
                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogDebug("Transaction committed for {Request}", typeof(TRequest).Name);
                    return response;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogWarning("Transaction rolled back for {Request}", typeof(TRequest).Name);
                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken: ct);
    }
}
