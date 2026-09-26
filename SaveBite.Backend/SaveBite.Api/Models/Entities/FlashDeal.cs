using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class FlashDeal
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }

    public DateTime SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    public DateTime ShopClosingTime { get; set; }

    public FlashDealStatus Status { get; set; } = FlashDealStatus.Pending;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<FlashDealVariant> Variants { get; set; } = new List<FlashDealVariant>();
}
