using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

// Response item for shop flash deals list view.
public class ShopFlashDealListItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string? CategoryName { get; set; }

    public DateTime SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    public DateTime ShopClosingTime { get; set; }

    public FlashDealStatus Status { get; set; }

    public decimal MinDealPrice { get; set; }
    public decimal MaxOriginalPrice { get; set; }
    public decimal MaxDiscountPercent { get; set; }

    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int AvailableQuantity => Math.Max(0, TotalQuantity - SoldQuantity - ReservedQuantity);

    public DateTime CreatedAt { get; set; }
}

// Detailed response for a specific shop flash deal.
public class ShopFlashDealDetailResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public List<string> ProductImageUrls { get; set; } = new();

    public DateTime SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    public DateTime ShopClosingTime { get; set; }

    public FlashDealStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ShopFlashDealVariantDetailResponse> Variants { get; set; } = new();
}

// Detailed response for each variant within a shop flash deal.
public class ShopFlashDealVariantDetailResponse
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
    public int ReservedQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int AvailableQuantity => Math.Max(0, TotalQuantity - SoldQuantity - ReservedQuantity);

    public FlashDealVariantStatus Status { get; set; }
}

// Product eligible for flash deal creation.
public class ShopEligibleProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public List<ShopEligibleProductVariantResponse> Variants { get; set; } = new();
}

public class ShopEligibleProductVariantResponse
{
    public Guid Id { get; set; }
    public string? Sku { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
}
