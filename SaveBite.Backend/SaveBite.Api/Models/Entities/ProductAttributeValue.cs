namespace SaveBite.Backend.Models.Entities;

public class ProductAttributeValue
{
    public Guid Id { get; set; }
    public Guid AttributeId { get; set; }

    public string Value { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public ProductAttribute Attribute { get; set; } = null!;
    public ICollection<ProductVariantValue> VariantValues { get; set; } = new List<ProductVariantValue>();
}
