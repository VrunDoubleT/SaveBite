using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

public class FlashDealResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopLogoUrl { get; set; }
    public string ShopAddress { get; set; } = string.Empty;
    public double? DistanceInKm { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public List<string> ProductImageUrls { get; set; } = new();
    public string? CategoryName { get; set; }
    public string? Description { get; set; }
    public DateTime SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    public DateTime ShopClosingTime { get; set; }
    public FlashDealStatus Status { get; set; }
    public decimal MinDealPrice { get; set; }
    public decimal MaxOriginalPrice { get; set; }
    public decimal MaxDiscountPercent { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public List<DealAttributeGroupResponse> AttributeGroups { get; set; } = new();
    public List<FlashDealVariantResponse> Variants { get; set; } = new();
}

public class DealAttributeGroupResponse
{
    public string Name { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}

public class FlashDealVariantResponse
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string? Sku { get; set; }
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int AvailableQuantity => Math.Max(0, TotalQuantity - SoldQuantity);
    public FlashDealVariantStatus Status { get; set; }
}