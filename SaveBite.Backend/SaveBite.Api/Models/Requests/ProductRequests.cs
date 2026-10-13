using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

// Multipart fields use the usual MVC indexed names, e.g. Attributes[0].Values[0].Image.
public sealed class CreateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<IFormFile> Images { get; set; } = new();
    public List<CreateProductAttributeRequest> Attributes { get; set; } = new();
    public List<ProductVariantPriceRequest> Variants { get; set; } = new();
}

public sealed class CreateProductAttributeRequest
{
    public string Name { get; set; } = string.Empty;
    public List<CreateProductValueRequest> Values { get; set; } = new();
}

public sealed class CreateProductValueRequest
{
    // Client-generated UUID correlates values with the price of each combination.
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}

public sealed class ProductVariantPriceRequest
{
    public List<Guid> ValueIds { get; set; } = new();
    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
}

public sealed class UpdateProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateProductImagesRequest
{
    // Complete retained gallery in desired order; omitted images are detached.
    public List<Guid> RetainedImageIds { get; set; } = new();
    public List<IFormFile> Images { get; set; } = new();
}

public sealed class AddProductValuesRequest
{
    public List<AddProductAttributeValuesRequest> Attributes { get; set; } = new();
    // Only newly generated combinations, using both existing and new value IDs.
    public List<ProductVariantPriceRequest> Variants { get; set; } = new();
}

public sealed class AddProductAttributeValuesRequest
{
    public Guid AttributeId { get; set; }
    public List<CreateProductValueRequest> Values { get; set; } = new();
}

public sealed class UpdateProductValueImageRequest
{
    public IFormFile Image { get; set; } = null!;
}

public class ProductPageRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ProductListRequest : ProductPageRequest
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public ProductStatus? Status { get; set; }
}

public sealed class ProductFlashDealListRequest : ProductPageRequest
{
    public FlashDealStatus? Status { get; set; }
}
