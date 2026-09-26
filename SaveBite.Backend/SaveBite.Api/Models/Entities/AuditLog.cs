using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public AuditActorType ActorType { get; set; }
    
    public string Action { get; set; } = string.Empty;
    
    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public string? Reason { get; set; }

    public Guid? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? ActorUser { get; set; }
}
