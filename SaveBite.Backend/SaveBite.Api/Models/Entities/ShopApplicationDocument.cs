using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class ShopApplicationDocument
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }

    public ShopDocumentType DocumentType { get; set; }

    public string FileUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    public int RevisionNumber { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }

    public bool IsCurrent { get; set; } = true;

    public ShopApplication Application { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
