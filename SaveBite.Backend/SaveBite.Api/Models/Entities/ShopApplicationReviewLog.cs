using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class ShopApplicationReviewLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    
    public Guid? AdminId { get; set; }

    public ShopApplicationStatus? FromStatus { get; set; }
    public ShopApplicationStatus ToStatus { get; set; }

    public int RevisionNumber { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public ShopApplication Application { get; set; } = null!;
    public User? Admin { get; set; }
}
