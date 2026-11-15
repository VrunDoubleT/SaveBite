using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Helpers;

public static class ProductImages
{
    public static string? Primary(Product? product) => product?.Images
        .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
        .Select(i => i.ImageUrl).FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

    public static string? ForVariant(ProductVariant? variant, Product? product = null) => variant?.VariantValues
        .Where(v => v.AttributeValue != null)
        .OrderBy(v => v.AttributeValue.Attribute?.SortOrder ?? int.MaxValue)
        .ThenBy(v => v.AttributeValue.AttributeId)
        .ThenBy(v => v.AttributeValue.SortOrder).ThenBy(v => v.AttributeValueId)
        .Select(v => v.AttributeValue.ImageUrl).FirstOrDefault(url => !string.IsNullOrWhiteSpace(url))
        ?? Primary(product ?? variant?.Product);
}
