using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class DealNotificationRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublicationId { get; set; }
    public Guid UserId { get; set; }

    public RecipientLocationSource LocationSource { get; set; }
    public string LocationVersion { get; set; } = string.Empty;
    public DealNotificationRecipientStatus Status { get; set; } = DealNotificationRecipientStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }

    public User User { get; set; } = null!;
}
