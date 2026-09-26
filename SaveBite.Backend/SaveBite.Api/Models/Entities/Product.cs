namespace SaveBite.Backend.Models.Entities;

public class Product
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<FlashDeal> FlashDeals { get; set; } = new List<FlashDeal>();
    public ICollection<ProductContentRevision> ContentRevisions { get; set; } = new List<ProductContentRevision>();
    public ICollection<ProductFeedback> Feedbacks { get; set; } = new List<ProductFeedback>();
}
