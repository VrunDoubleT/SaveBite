using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class ProductFeedbackModerationLog
{
    public Guid Id { get; set; }

    public Guid ProductFeedbackId { get; set; }

    public Guid AdminUserId { get; set; }

    public FeedbackModerationAction Action { get; set; }

    public FeedbackStatus PreviousStatus { get; set; }

    public FeedbackStatus NewStatus { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ProductFeedback ProductFeedback { get; set; } = null!;
    public User AdminUser { get; set; } = null!;
}
