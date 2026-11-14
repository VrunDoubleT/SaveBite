using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class FlashDeal
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }

    public DateTime SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    // Owner-selected final pickup cutoff; not the shop operating closing time.
    public DateTime ShopClosingTime { get; set; }

    public FlashDealStatus Status { get; set; } = FlashDealStatus.Pending;

    public DateTime FeedPublishedAt { get; set; }
    public long VisibilityVersion { get; set; } = 1;
    public Guid PublicationId { get; set; } = Guid.NewGuid();

    public string? OpenJobId { get; set; }
    public string? EndJobId { get; set; }
    public DateTime? LifecycleSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<FlashDealVariant> Variants { get; set; } = new List<FlashDealVariant>();
    public ICollection<FlashDealStatusHistory> StatusHistory { get; set; } = new List<FlashDealStatusHistory>();
}
