using SaveBite.Backend.Audit;

namespace SaveBite.Backend.Models.Entities;

[NoAudit]
public class ProductVariantValue
{
    public Guid VariantId { get; set; }
    public Guid AttributeValueId { get; set; }

    public ProductVariant Variant { get; set; } = null!;
    public ProductAttributeValue AttributeValue { get; set; } = null!;
}
