namespace Notifications.API.Domain;

/// <summary>
/// Registro de cada notificación enviada.
/// El índice único en OrderId + Type garantiza idempotencia:
/// si el mismo mensaje llega dos veces, lo detectamos.
/// </summary>
public class Notification
{
    public Guid     Id             { get; private set; } = Guid.NewGuid();
    public Guid     OrderId        { get; private set; }
    public string   Type           { get; private set; } = string.Empty;
    public string   RecipientEmail { get; private set; } = string.Empty;
    public DateTime ProcessedAt    { get; private set; } = DateTime.UtcNow;
    public bool     Success        { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid   orderId,
        string type,
        string email,
        bool   success) => new()
    {
        OrderId        = orderId,
        Type           = type,
        RecipientEmail = email,
        Success        = success
    };
}
