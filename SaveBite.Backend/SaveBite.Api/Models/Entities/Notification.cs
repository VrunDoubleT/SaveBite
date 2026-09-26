using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class Notification
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? ActionUrl { get; set; }

    public string? DeduplicationKey { get; set; }

    public DateTime? ReadAt { get; set; }
    
    public DateTime? DismissedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
