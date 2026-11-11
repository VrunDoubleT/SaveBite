using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Audit;

public interface IAuditContext
{
    Guid? ActorUserId { get; }
    AuditActorType ActorType { get; }
    Guid? CorrelationId { get; }
    string? Reason { get; set; }
    bool Enabled { get; set; }
    void SetActor(Guid? actorUserId, AuditActorType? actorType = null);
    void SetCorrelationId(Guid correlationId);
}
