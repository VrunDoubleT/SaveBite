using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class ProductContentRevision
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public int RevisionNumber { get; set; }

    public Guid SubmittedByUserId { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid CategoryId { get; set; }
    
    public string ImagesJson { get; set; } = "[]";

    public ProductReviewStatus Status { get; set; } = ProductReviewStatus.Pending;

    public Guid? ReviewedByUserId { get; set; }
    
    public string? ReviewNote { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Product Product { get; set; } = null!;
    public User SubmittedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public Category Category { get; set; } = null!;
}
