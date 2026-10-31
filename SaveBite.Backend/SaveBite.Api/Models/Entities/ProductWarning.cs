using SaveBite.Backend.Helpers;
using System.Text.Json;

namespace SaveBite.Backend.Models.Entities;

// A warning stays open across all shop edits until an administrator approves it.
public class ProductWarning
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public Guid IssuedByUserId { get; set; }
    public Guid BaselineRevisionId { get; set; }
    public string Reason { get; set; } = "";
    public string RequiredCorrection { get; set; } = "";
    public string ProductName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? FirstEditedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool DealBlocked { get; set; }

    public bool IsDealBlocked(DateTime now)
        => ResolvedAt == null && (DealBlocked ||
            (DueAt <= now && (FirstEditedAt == null || FirstEditedAt > DueAt)));

    public void RecordContentEdit(Product product, DateTime now, bool attributeContentChanged = false)
    {
        if (ResolvedAt != null || FirstEditedAt != null)
            return;

        var baseline = BaselineRevision;
        var images = product.Images.OrderBy(x => x.SortOrder).Select(x => x.ImageUrl);
        var originalImages = JsonSerializer.Deserialize<string[]>(baseline.ImagesJson) ?? [];
        var originalValues = ProductAttributeContentHelper.Read(baseline.AttributeValuesJson);
        var valuesChanged = originalValues == null
            ? attributeContentChanged
            : ProductAttributeContentHelper.HasChanges(originalValues, product);
        if (valuesChanged || baseline.Name != product.Name || baseline.Description != product.Description ||
            baseline.CategoryId != product.CategoryId || !originalImages.SequenceEqual(images))
            FirstEditedAt = now;
    }

    public ProductContentRevision BaselineRevision { get; set; } = null!;
}
