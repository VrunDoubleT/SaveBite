using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

public sealed record ProductImageResponse(
    Guid Id, string Url, string Source, Guid? AttributeId, Guid? ValueId,
    string? AttributeName, string? Value);
public sealed record ProductValueResponse(Guid Id, string Value, string? ImageUrl);
public sealed record ProductAttributeResponse(
    Guid Id, string Name, IReadOnlyList<ProductValueResponse> Values);
public sealed record ProductVariantResponse(
    Guid Id, string? Sku, decimal OriginalPrice, decimal DealPrice,
    bool IsActive, IReadOnlyList<Guid> ValueIds);
public sealed record ProductReviewResponse(
    ProductReviewStatus Status, string? Note, DateTime? ReviewedAt, Guid? RevisionId = null);
public sealed record ProductResponse(
    Guid Id, Guid ShopId, Guid CategoryId, string CategoryName,
    string Name, string? Description, ProductStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<ProductImageResponse> Images,
    IReadOnlyList<ProductAttributeResponse> Attributes,
    IReadOnlyList<ProductVariantResponse> Variants,
    ProductReviewResponse? LatestReview);
public sealed record ProductListItemResponse(
    Guid Id, Guid CategoryId, string CategoryName, string Name, ProductStatus Status,
    int VariantCount, decimal? MinOriginalPrice, decimal? MinDealPrice, decimal? MaxDealPrice,
    DateTime UpdatedAt, IReadOnlyList<ProductImageResponse> Images);
public sealed record ProductVariantSelectionResponse(
    Guid AttributeId, string AttributeName, Guid ValueId, string Value);
public sealed record ProductFlashDealVariantResponse(
    Guid Id, Guid VariantId, string? Sku, IReadOnlyList<ProductVariantSelectionResponse> Values,
    decimal OriginalPrice, decimal DealPrice, decimal DiscountPercent,
    int TotalQuantity, int ReservedQuantity, int SoldQuantity, int AvailableQuantity,
    FlashDealVariantStatus Status);
public sealed record ProductFlashDealResponse(
    Guid Id, FlashDealStatus Status, DateTime SaleStartTime, DateTime OrderEndTime,
    DateTime ShopClosingTime, DateTime CreatedAt,
    IReadOnlyList<ProductFlashDealVariantResponse> Variants);

public sealed record ProductShopOption(Guid Id, string Name);
public sealed record ProductCategoryOption(Guid Id, string Name);
public sealed record ProductOptionsResponse(
    ProductShopOption? Shop, IReadOnlyList<ProductCategoryOption> Categories);

public sealed record ProductSummaryResponse(int TotalProducts, int ActiveProducts, int HiddenProducts, int TotalVariants);

// Manage Products.
public class ProductListResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }
    public int VariantCount { get; set; }
    public decimal? MinOriginalPrice { get; set; }
    public decimal? MinDealPrice { get; set; }
    public decimal? MaxDealPrice { get; set; }
    public IReadOnlyList<ProductImageResponse> Images { get; set; } = Array.Empty<ProductImageResponse>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ProductReviewResponse? LatestReview { get; set; }
}

// Manage Products.
public class ProductContentResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductStatus Status { get; set; }
    public IReadOnlyList<ProductImageResponse> Images { get; set; } = Array.Empty<ProductImageResponse>();
    public IReadOnlyList<ProductAttributeResponse> Attributes { get; set; } = Array.Empty<ProductAttributeResponse>();
    public IReadOnlyList<ProductVariantResponse> Variants { get; set; } = Array.Empty<ProductVariantResponse>();
    public ProductReviewResponse? LatestReview { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}