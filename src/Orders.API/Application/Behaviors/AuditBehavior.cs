using MediatR;
using Orders.API.Application.Interfaces;
using Orders.API.Infrastructure.Audit;

namespace Orders.API.Application.Behaviors;

/// <summary>
/// Pipeline behavior que intercepta todos los Commands que implementan IAuditable
/// y genera automáticamente el registro de auditoría.
/// Registra tanto el éxito como el fallo — ambos son relevantes para compliance.
/// </summary>
public class AuditBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditLogger         _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(
        IAuditLogger          auditLogger,
        IHttpContextAccessor  httpContextAccessor,
        ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditLogger         = auditLogger;
        _httpContextAccessor = httpContextAccessor;
        _logger              = logger;
    }

    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 ct)
    {
        if (request is not IAuditable auditable)
            return await next();

        var ip = _httpContextAccessor.HttpContext?
            .Connection.RemoteIpAddress?.ToString();

        try
        {
            var response = await next();

            await _auditLogger.LogAsync(new AuditEvent
            {
                Action       = typeof(TRequest).Name,
                UserId       = auditable.ActorId.ToString(),
                UserEmail    = auditable.ActorEmail,
                ResourceType = auditable.ResourceType,
                ResourceId   = auditable.ResourceId?.ToString(),
                IpAddress    = ip,
                Success      = true
            }, ct);

            return response;
        }
        catch (Exception ex)
        {
            await _auditLogger.LogAsync(new AuditEvent
            {
                Action        = $"{typeof(TRequest).Name}Failed",
                UserId        = auditable.ActorId.ToString(),
                UserEmail     = auditable.ActorEmail,
                ResourceType  = auditable.ResourceType,
                ResourceId    = auditable.ResourceId?.ToString(),
                IpAddress     = ip,
                Success       = false,
                FailureReason = ex.Message
            }, ct);

            throw;
        }
    }
}
