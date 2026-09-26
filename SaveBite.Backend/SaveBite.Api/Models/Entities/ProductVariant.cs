namespace SaveBite.Backend.Models.Entities;

public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    public string? Sku { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductVariantValue> VariantValues { get; set; } = new List<ProductVariantValue>();
    public ICollection<FlashDealVariant> FlashDealVariants { get; set; } = new List<FlashDealVariant>();
}
