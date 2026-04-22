using System.Text.Json;
using Orders.API.Application.Interfaces;
using Orders.API.Infrastructure.Persistence;

namespace Orders.API.Infrastructure.Audit;

/// <summary>
/// Persiste audit events en orders.AuditLogs y los emite como logs estructurados
/// para Application Insights (con el prefijo [AUDIT] para facilitar el filtrado).
/// </summary>
public class DatabaseAuditLogger : IAuditLogger
{
    private readonly OrderDbContext              _dbContext;
    private readonly ILogger<DatabaseAuditLogger> _logger;

    public DatabaseAuditLogger(
        OrderDbContext               dbContext,
        ILogger<DatabaseAuditLogger> logger)
    {
        _dbContext = dbContext;
        _logger    = logger;
    }

    public async Task LogAsync(AuditEvent evt, CancellationToken ct = default)
    {
        var entry = new AuditLogEntry
        {
            Action        = evt.Action,
            UserId        = evt.UserId,
            UserEmail     = evt.UserEmail,
            ResourceType  = evt.ResourceType,
            ResourceId    = evt.ResourceId,
            IpAddress     = evt.IpAddress,
            Details       = evt.Details is not null
                ? JsonSerializer.Serialize(evt.Details)
                : null,
            Timestamp     = evt.Timestamp,
            Success       = evt.Success,
            FailureReason = evt.FailureReason
        };

        _dbContext.AuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[AUDIT] {Action} by {UserEmail} ({UserId}) on {ResourceType} " +
            "{ResourceId} from {IpAddress}. Success: {Success}",
            evt.Action, evt.UserEmail, evt.UserId,
            evt.ResourceType, evt.ResourceId ?? "N/A",
            evt.IpAddress ?? "unknown", evt.Success);
    }
}
