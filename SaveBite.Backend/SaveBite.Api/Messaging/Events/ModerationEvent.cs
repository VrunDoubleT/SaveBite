namespace SaveBite.Backend.Messaging.Events;

public sealed class ModerationEvent
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string? Reason { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public DateTime CreatedAt { get; set; }
}