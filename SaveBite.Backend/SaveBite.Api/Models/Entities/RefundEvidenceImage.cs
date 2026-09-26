using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class RefundEvidenceImage
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public RefundEvidenceType ImageType { get; set; }

    public Guid UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Refund Refund { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
