namespace SaveBite.Backend.Models.Entities;

public class OrderItem
{
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

    public Order Order { get; set; } = null!;
    public FlashDealVariant FlashDealVariant { get; set; } = null!;
    public ProductFeedback? Feedback { get; set; }
}
