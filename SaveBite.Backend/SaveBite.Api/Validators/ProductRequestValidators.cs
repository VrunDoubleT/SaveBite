using FluentValidation;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class ProductImageFileValidator : AbstractValidator<IFormFile>
{
    public ProductImageFileValidator(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        RuleFor(x => x.Length).GreaterThan(0).LessThanOrEqualTo(settings.MaxFileSizeBytes);
        RuleFor(x => x.ContentType).Must(type => settings.AllowedContentTypes.Contains(
            type, StringComparer.OrdinalIgnoreCase)).WithMessage("Unsupported image type.");
    }
}

public sealed class CreateProductValueRequestValidator : AbstractValidator<CreateProductValueRequest>
{
    public CreateProductValueRequestValidator(ProductImageFileValidator images)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Value).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Image!).SetValidator(images).When(x => x.Image is not null);
    }
}

public sealed class ProductVariantPriceRequestValidator : AbstractValidator<ProductVariantPriceRequest>
{
    public ProductVariantPriceRequestValidator()
    {
        RuleFor(x => x.ValueIds).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count <= ProductLimits.Attributes && x.Distinct().Count() == x.Count)
            .WithMessage("Variant value IDs must be unique and within the attribute limit.");
        RuleForEach(x => x.ValueIds).NotEmpty();
        RuleFor(x => x.OriginalPrice).GreaterThan(0).PrecisionScale(12, 2, false);
        RuleFor(x => x.DealPrice).GreaterThanOrEqualTo(0)
            .LessThan(x => x.OriginalPrice).PrecisionScale(12, 2, false);
    }
}

public sealed class CreateProductAttributeRequestValidator : AbstractValidator<CreateProductAttributeRequest>
{
    public CreateProductAttributeRequestValidator(CreateProductValueRequestValidator values)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Values).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count is > 0 and <= ProductLimits.ValuesPerAttribute)
            .WithMessage($"Provide 1–{ProductLimits.ValuesPerAttribute} values per attribute.");
        RuleForEach(x => x.Values).NotNull().SetValidator(values);
    }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator(ProductImageFileValidator images,
        CreateProductAttributeRequestValidator attributes, ProductVariantPriceRequestValidator variants)
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(ProductLimits.DescriptionLength);
        RuleFor(x => x.Images).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count <= ProductLimits.Images).WithMessage("At most 10 gallery images are allowed.");
        RuleForEach(x => x.Images).NotNull().SetValidator(images);
        RuleFor(x => x.Attributes).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count <= ProductLimits.Attributes).WithMessage("At most 3 attributes are allowed.");
        RuleForEach(x => x.Attributes).NotNull().SetValidator(attributes);
        RuleFor(x => x.Variants).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count is > 0 and <= ProductLimits.Variants)
            .WithMessage("Provide prices for 1–100 variants.");
        RuleForEach(x => x.Variants).NotNull().SetValidator(variants);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(ProductLimits.DescriptionLength);
    }
}

public sealed class UpdateProductImagesRequestValidator : AbstractValidator<UpdateProductImagesRequest>
{
    public UpdateProductImagesRequestValidator(ProductImageFileValidator images)
    {
        RuleFor(x => x.RetainedImageIds).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count <= ProductLimits.Images && x.Distinct().Count() == x.Count)
            .WithMessage("Retained images must be unique, with at most 10 images.");
        RuleForEach(x => x.RetainedImageIds).NotEmpty();
        RuleFor(x => x.Images).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count <= ProductLimits.Images).WithMessage("At most 10 gallery images are allowed.");
        RuleForEach(x => x.Images).NotNull().SetValidator(images);
    }
}

public sealed class AddProductAttributeValuesRequestValidator : AbstractValidator<AddProductAttributeValuesRequest>
{
    public AddProductAttributeValuesRequestValidator(CreateProductValueRequestValidator values)
    {
        RuleFor(x => x.AttributeId).NotEmpty();
        RuleFor(x => x.Values).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count is > 0 and <= ProductLimits.ValuesPerAttribute)
            .WithMessage("Provide 1–20 new values.");
        RuleForEach(x => x.Values).NotNull().SetValidator(values);
    }
}

public sealed class AddProductValuesRequestValidator : AbstractValidator<AddProductValuesRequest>
{
    public AddProductValuesRequestValidator(AddProductAttributeValuesRequestValidator attributes,
        ProductVariantPriceRequestValidator variants)
    {
        RuleFor(x => x.Attributes).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count is > 0 and <= ProductLimits.Attributes)
            .WithMessage("Provide values for 1–3 existing attributes.");
        RuleForEach(x => x.Attributes).NotNull().SetValidator(attributes);
        RuleFor(x => x.Variants).Cascade(CascadeMode.Stop).NotNull()
            .Must(x => x.Count is > 0 and <= ProductLimits.Variants)
            .WithMessage("Provide prices for all new combinations, up to 100 variants.");
        RuleForEach(x => x.Variants).NotNull().SetValidator(variants);
    }
}

public sealed class UpdateProductValueImageRequestValidator : AbstractValidator<UpdateProductValueImageRequest>
{
    public UpdateProductValueImageRequestValidator(ProductImageFileValidator images)
        => RuleFor(x => x.Image).NotNull().SetValidator(images);
}

public sealed class ProductListRequestValidator : AbstractValidator<ProductListRequest>
{
    public ProductListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.CategoryId).NotEmpty().When(x => x.CategoryId.HasValue);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class ProductFlashDealListRequestValidator : AbstractValidator<ProductFlashDealListRequest>
{
    public ProductFlashDealListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum();
    }
}
public sealed class UpdateProductVariantRequestValidator : AbstractValidator<UpdateProductVariantRequest>
{
    public UpdateProductVariantRequestValidator()
    {
        RuleFor(x => x.OriginalPrice).GreaterThan(0).PrecisionScale(12, 2, false)
            .WithMessage("Original price must be greater than 0.");
        RuleFor(x => x.DealPrice).GreaterThanOrEqualTo(0)
            .LessThan(x => x.OriginalPrice).PrecisionScale(12, 2, false)
            .WithMessage("Deal price must be greater than or equal to 0 and less than original price.");
    }
}
public sealed class UpdateProductStatusRequestValidator : AbstractValidator<UpdateProductStatusRequest>
{
    public UpdateProductStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}