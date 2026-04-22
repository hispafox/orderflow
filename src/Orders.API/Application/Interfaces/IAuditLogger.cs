using Orders.API.Infrastructure.Audit;

namespace Orders.API.Application.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(AuditEvent auditEvent, CancellationToken ct = default);
}
