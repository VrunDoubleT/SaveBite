using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class ProductFeedback
{
    public Guid Id { get; set; }
    
    public Guid OrderItemId { get; set; }

    public Guid ProductId { get; set; }

    public Guid UserId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public FeedbackStatus Status { get; set; } = FeedbackStatus.Visible;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? HiddenAt { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
    public ProductFeedbackReply? Reply { get; set; }
    public ICollection<ProductFeedbackModerationLog> ModerationLogs { get; set; } = new List<ProductFeedbackModerationLog>();
}
