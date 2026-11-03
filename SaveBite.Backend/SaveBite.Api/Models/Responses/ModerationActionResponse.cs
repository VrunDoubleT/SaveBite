namespace SaveBite.Backend.Models.Responses;

public sealed class ModerationActionResponse
{
    public Guid TargetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}