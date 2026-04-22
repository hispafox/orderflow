namespace Orders.API.Infrastructure.Audit;

/// <summary>
/// Record inmutable que describe un evento de auditoría.
/// Separado del logging de aplicación (Serilog):
///   - El audit log es para compliance y respuesta a incidentes
///   - El logging de app es para debugging y operaciones
/// </summary>
public record AuditEvent
{
    public string  Action        { get; init; } = string.Empty;
    public string  UserId        { get; init; } = string.Empty;
    public string  UserEmail     { get; init; } = string.Empty;
    public string  ResourceType  { get; init; } = string.Empty;
    public string? ResourceId    { get; init; }
    public string? IpAddress     { get; init; }
    public Dictionary<string, object>? Details { get; init; }
    public DateTime Timestamp    { get; init; } = DateTime.UtcNow;
    public bool    Success       { get; init; } = true;
    public string? FailureReason { get; init; }
}
