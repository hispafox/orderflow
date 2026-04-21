using Microsoft.EntityFrameworkCore;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Infrastructure.Outbox;

public class OutboxCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly ILogger<OutboxCleanupJob> _logger;

    public OutboxCleanupJob(
        IServiceScopeFactory      scopeFactory,
        ILogger<OutboxCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            using var scope   = _scopeFactory.CreateScope();
            var dbContext     = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var cutoff        = DateTime.UtcNow.AddHours(-24);
            var stuckCutoff   = DateTime.UtcNow.AddMinutes(-10);

            var deleted = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM orders.OutboxMessage WHERE OutboxId IS NOT NULL AND SentTime < {cutoff}",
                ct);

            if (deleted > 0)
                _logger.LogInformation(
                    "Outbox cleanup: deleted {Count} processed messages older than {Cutoff}",
                    deleted, cutoff);

            var stuck = await dbContext.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM orders.OutboxMessage WHERE OutboxId IS NULL AND SentTime < @p0",
                    stuckCutoff)
                .FirstOrDefaultAsync(ct);

            if (stuck > 0)
                _logger.LogWarning(
                    "[OUTBOX ALERT] {Count} messages stuck for > 10 minutes. Check RabbitMQ connectivity.",
                    stuck);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Outbox cleanup failed");
        }
    }
}
