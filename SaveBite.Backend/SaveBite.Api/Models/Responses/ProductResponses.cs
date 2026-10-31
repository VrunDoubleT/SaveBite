using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Helpers;
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
    ProductReviewStatus Status, string? Note, DateTime? ReviewedAt, Guid? RevisionId = null,
    Guid? WarningId = null, bool CorrectionSubmitted = false, DateTime? SubmittedAt = null)
{
    public static ProductReviewResponse FromRevision(ProductContentRevision revision, ProductWarning? warning)
        => new(revision.Status, revision.ReviewNote, revision.ReviewedAt, revision.Id,
            revision.WarningId,
            revision.WarningId != null && warning?.Id == revision.WarningId && warning.FirstEditedAt != null,
            revision.SubmittedAt);
}
public sealed record ProductResponse(
    Guid Id, Guid ShopId, Guid CategoryId, string CategoryName,
    string Name, string? Description, ProductStatus Status,
    DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<ProductImageResponse> Images,
    IReadOnlyList<ProductAttributeResponse> Attributes,
    IReadOnlyList<ProductVariantResponse> Variants,
    ProductReviewResponse? LatestReview, ActiveWarningResponse? ActiveWarning = null);
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
    public WarningComparisonResponse? WarningComparison { get; set; }
    public IReadOnlyList<ProductRevisionHistoryResponse> RevisionHistory { get; set; } = [];
}
public sealed class WarningComparisonResponse
{
    public Guid WarningId { get; set; }
    public string Reason { get; set; }
    public string RequiredCorrection { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? FirstEditedAt { get; set; }
    public bool DealBlocked { get; set; }
    public ContentSnapshot Before { get; set; }
    public ContentSnapshot After { get; set; }

    public WarningComparisonResponse(
        Guid warningId,
        string reason,
        string requiredCorrection,
        DateTime dueAt,
        DateTime? firstEditedAt,
        bool dealBlocked,
        ContentSnapshot before,
        ContentSnapshot after)
    {
        WarningId = warningId;
        Reason = reason;
        RequiredCorrection = requiredCorrection;
        DueAt = dueAt;
        FirstEditedAt = firstEditedAt;
        DealBlocked = dealBlocked;
        Before = before;
        After = after;
    }
}
public sealed class ContentSnapshot
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; }
    public IReadOnlyList<string> Images { get; set; }
    public IReadOnlyList<AttributeValueContent>? AttributeValues { get; set; }

    public ContentSnapshot(
        string name,
        string? description,
        Guid categoryId,
        string categoryName,
        IReadOnlyList<string> images,
        IReadOnlyList<AttributeValueContent>? attributeValues)
    {
        Name = name;
        Description = description;
        CategoryId = categoryId;
        CategoryName = categoryName;
        Images = images;
        AttributeValues = attributeValues;
    }
}

public sealed class ActiveWarningResponse
{
    public Guid Id { get; set; }
    public string RequiredCorrection { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? FirstEditedAt { get; set; }
    public bool DealBlocked { get; set; }

    public ActiveWarningResponse(
        Guid id,
        string requiredCorrection,
        DateTime dueAt,
        DateTime? firstEditedAt,
        bool dealBlocked)
    {
        Id = id;
        RequiredCorrection = requiredCorrection;
        DueAt = dueAt;
        FirstEditedAt = firstEditedAt;
        DealBlocked = dealBlocked;
    }
}

public sealed class ProductRevisionHistoryResponse
{
    public Guid Id { get; set; }
    public int RevisionNumber { get; set; }
    public bool IsLatest { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; }
    public DateTime SubmittedAt { get; set; }
    public ProductReviewStatus Status { get; set; }
    public string? ReviewNote { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? WarningId { get; set; }
    public string? WarningReason { get; set; }
    public string? RequiredCorrection { get; set; }
    public ContentSnapshot? Before { get; set; }
    public ContentSnapshot After { get; set; }
    public DateTime? WarningDueAt { get; set; }

    public ProductRevisionHistoryResponse(
        Guid id,
        int revisionNumber,
        bool isLatest,
        Guid submittedByUserId,
        string submittedByName,
        DateTime submittedAt,
        ProductReviewStatus status,
        string? reviewNote,
        Guid? reviewedByUserId,
        string? reviewedByName,
        DateTime? reviewedAt,
        Guid? warningId,
        string? warningReason,
        string? requiredCorrection,
        ContentSnapshot? before,
        ContentSnapshot after,
        DateTime? warningDueAt = null)
    {
        Id = id;
        RevisionNumber = revisionNumber;
        IsLatest = isLatest;
        SubmittedByUserId = submittedByUserId;
        SubmittedByName = submittedByName;
        SubmittedAt = submittedAt;
        Status = status;
        ReviewNote = reviewNote;
        ReviewedByUserId = reviewedByUserId;
        ReviewedByName = reviewedByName;
        ReviewedAt = reviewedAt;
        WarningId = warningId;
        WarningReason = warningReason;
        RequiredCorrection = requiredCorrection;
        Before = before;
        After = after;
        WarningDueAt = warningDueAt;
    }
    public static IReadOnlyList<ProductRevisionHistoryResponse> FromProduct(Product product)
    {
        var revisions = product.ContentRevisions.OrderByDescending(r => r.RevisionNumber).ToArray();
        var warnings = product.Warnings.ToDictionary(w => w.Id);
        return revisions.Select((revision, index) =>
        {
            var warning = revision.WarningId is Guid warningId ? warnings.GetValueOrDefault(warningId) : null;
            return new ProductRevisionHistoryResponse(revision.Id, revision.RevisionNumber, index == 0,
                revision.SubmittedByUserId, revision.SubmittedByUser.FullName, revision.SubmittedAt, revision.Status,
                revision.ReviewNote, revision.ReviewedByUserId, revision.ReviewedByUser?.FullName, revision.ReviewedAt,
                revision.WarningId, warning?.Reason, warning?.RequiredCorrection,
                index + 1 < revisions.Length ? Snapshot(revisions[index + 1]) : null, Snapshot(revision), warning?.DueAt);
        }).ToArray();
    }

    public static ContentSnapshot Snapshot(ProductContentRevision revision)
        => new(revision.Name, revision.Description, revision.CategoryId, revision.Category.Name,
            System.Text.Json.JsonSerializer.Deserialize<string[]>(revision.ImagesJson) ?? [],
            ProductAttributeContentHelper.Read(revision.AttributeValuesJson));
}
