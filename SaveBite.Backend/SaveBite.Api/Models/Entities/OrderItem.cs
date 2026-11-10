namespace SaveBite.Backend.Models.Entities;

public class OrderItem
{
    public string? ProductImageUrlSnapshot { get; set; }
    public DateTime? SnapshotCapturedAt { get; set; }

    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid FlashDealVariantId { get; set; }

    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? VariantNameSnapshot { get; set; }
    public decimal OriginalPriceSnapshot { get; set; }
    public decimal DealPriceSnapshot { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal? Subtotal { get; set; }

    public SaveBite.Backend.Models.Enums.ResaleDecisionStatus ResaleDecisionStatus { get; set; }
    public int ResaleQuantity { get; set; }
    public int UnsellableQuantity { get; set; }
    public DateTime? ResaleDecidedAt { get; set; }
    public Guid? ResaleDecidedBy { get; set; }

    public Order Order { get; set; } = null!;
    public FlashDealVariant FlashDealVariant { get; set; } = null!;
    public ProductFeedback? Feedback { get; set; }
}
