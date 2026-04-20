namespace Orders.API.Application.Interfaces;

/// <summary>
/// Marker interface para Commands que deben generar audit log.
/// Si un Command implementa IAuditable, el AuditBehavior lo intercepta
/// y genera automáticamente el registro de auditoría.
/// </summary>
public interface IAuditable
{
    Guid    ActorId      { get; }
    string  ActorEmail   { get; }
    string  ResourceType { get; }
    Guid?   ResourceId   { get; }
}
