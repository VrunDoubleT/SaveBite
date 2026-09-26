using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class FlashDealVariant
{
    public Guid Id { get; set; }
    public Guid FlashDealId { get; set; }
    public Guid VariantId { get; set; }

    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public decimal DiscountPercent { get; set; }

    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int SoldQuantity { get; set; }

    public FlashDealVariantStatus Status { get; set; } = FlashDealVariantStatus.Active;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public FlashDeal FlashDeal { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
