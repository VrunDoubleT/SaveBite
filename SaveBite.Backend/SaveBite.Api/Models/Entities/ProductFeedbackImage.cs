namespace SaveBite.Backend.Models.Entities;

public class ProductFeedbackImage
{
    public Guid Id { get; set; }

    public Guid ProductFeedbackId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string? PublicId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ProductFeedback ProductFeedback { get; set; } = null!;
}
