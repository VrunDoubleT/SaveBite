using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class FlashDealStatusHistory
{
    public Guid Id { get; set; }
    public Guid FlashDealId { get; set; }
    public FlashDealStatus? FromStatus { get; set; }
    public FlashDealStatus ToStatus { get; set; }
    public string ChangedByType { get; set; } = string.Empty;
    public Guid? ChangedByUserId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public FlashDeal FlashDeal { get; set; } = null!;
}
