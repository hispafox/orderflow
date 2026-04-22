namespace Orders.API.Infrastructure.Audit;

/// <summary>
/// Fila de la tabla orders.AuditLogs.
/// Sin cascade delete — el audit log es inmutable por diseño.
/// Índices estratégicos: UserId (GDPR acceso), Timestamp (informes), ResourceType+Id (investigación).
/// </summary>
public class AuditLogEntry
{
    public long     Id            { get; set; }
    public string   Action        { get; set; } = string.Empty;
    public string   UserId        { get; set; } = string.Empty;
    public string   UserEmail     { get; set; } = string.Empty;
    public string   ResourceType  { get; set; } = string.Empty;
    public string?  ResourceId    { get; set; }
    public string?  IpAddress     { get; set; }
    public string?  Details       { get; set; }
    public DateTime Timestamp     { get; set; } = DateTime.UtcNow;
    public bool     Success       { get; set; }
    public string?  FailureReason { get; set; }
}
