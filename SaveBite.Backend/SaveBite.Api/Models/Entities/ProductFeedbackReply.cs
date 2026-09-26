namespace SaveBite.Backend.Models.Entities;

public class ProductFeedbackReply
{
    public Guid Id { get; set; }

    public Guid ProductFeedbackId { get; set; }
    
    public Guid RepliedByUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ProductFeedback ProductFeedback { get; set; } = null!;
    public User RepliedByUser { get; set; } = null!;
}
