using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Audit;

public class HttpAuditContext(IHttpContextAccessor accessor) : IAuditContext
{
    private Guid? _correlationId;
    private Guid? _actorUserId;
    private AuditActorType? _actorType;

    public Guid? ActorUserId
    {
        get
        {
            if (_actorUserId.HasValue) return _actorUserId.Value;

            var sub = accessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? accessor.HttpContext?.User.FindFirst("sub")?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public AuditActorType ActorType
    {
        get
        {
            if (_actorType.HasValue) return _actorType.Value;
            return ActorUserId is null ? AuditActorType.System : AuditActorType.User;
        }
    }

    public Guid? CorrelationId => _correlationId ??= ResolveCorrelationId();

    public string? Reason { get; set; }

    public bool Enabled { get; set; } = true;

    public void SetActor(Guid? actorUserId, AuditActorType? actorType = null)
    {
        _actorUserId = actorUserId;
        _actorType = actorType ?? (actorUserId is null ? AuditActorType.System : AuditActorType.User);
    }

    public void SetCorrelationId(Guid correlationId)
    {
        _correlationId = correlationId;
    }

    private Guid ResolveCorrelationId()
    {
        var h = accessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        return Guid.TryParse(h, out var id) ? id : Guid.NewGuid();
    }
}
