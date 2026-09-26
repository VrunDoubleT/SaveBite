namespace SaveBite.Backend.Models.Entities;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FlashDealVariantId { get; set; }

    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }

    public User User { get; set; } = null!;
    public FlashDealVariant FlashDealVariant { get; set; } = null!;
}
