using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Common;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICloudinaryService _images;
    private readonly TimeProvider _clock;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repository, ICloudinaryService images,
        TimeProvider clock, ILogger<ProductService> logger)
        => (_repository, _images, _clock, _logger) = (repository, images, clock, logger);

    public async Task<ProductSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct)
        => await _repository.GetSummaryAsync(await RequireShopAsync(userId, ct), ct);

    public Task<ProductOptionsResponse> GetOptionsAsync(Guid userId, CancellationToken ct)
        => _repository.GetOptionsAsync(userId, ct);

    public async Task<ProductResponse> CreateAsync(Guid userId,
        CreateProductRequest request, CancellationToken ct)
    {
        var shopId = await RequireShopAsync(userId, ct);
        await RequireCategoryAsync(request.CategoryId, ct);
        EnsureUniqueNames(request.Attributes.Select(x => x.Name), "Attribute names");
        var newValues = request.Attributes.SelectMany(x => x.Values).ToArray();
        await RequireNewValueIdsAsync(newValues, ct);
        var now = Now();
        var product = new Product
        {
            Id = Guid.NewGuid(), ShopId = shopId, CategoryId = request.CategoryId,
            Name = request.Name.Trim(), Description = request.Description?.Trim(),
            Status = ProductStatus.Active, CreatedAt = now, UpdatedAt = now
        };
        var uploads = new List<(ProductAttributeValue Value, IFormFile File)>();
        foreach (var input in request.Attributes)
        {
            EnsureUniqueNames(input.Values.Select(x => x.Value), "Attribute values");
            var attribute = new ProductAttribute
            {
                Id = Guid.NewGuid(), ProductId = product.Id,
                Name = input.Name.Trim(), SortOrder = product.Attributes.Count
            };
            foreach (var value in input.Values)
            {
                var entity = NewValue(attribute, value);
                attribute.Values.Add(entity);
                if (value.Image is not null) uploads.Add((entity, value.Image));
            }
            product.Attributes.Add(attribute);
        }

        IReadOnlyList<ProductVariantPriceRequest> prices = request.Variants;
        if (product.Attributes.Count == 0)
        {
            if (prices.Count != 1 || prices[0].ValueIds.Count != 0)
                throw AppException.BadRequest("Without attributes, provide one variant price with an empty valueIds list.");
            var attribute = new ProductAttribute
            {
                Id = Guid.NewGuid(), ProductId = product.Id, Name = "Type"
            };
            var value = new ProductAttributeValue
            {
                Id = Guid.NewGuid(), AttributeId = attribute.Id, Value = "Default"
            };
            attribute.Values.Add(value);
            product.Attributes.Add(attribute);
            prices = new[] { new ProductVariantPriceRequest
            {
                ValueIds = new() { value.Id }, OriginalPrice = prices[0].OriginalPrice,
                DealPrice = prices[0].DealPrice
            } };
        }
        product.Variants = ProductVariantBuilder.Create(product.Id,
            ProductVariantBuilder.Combinations(product.Attributes), prices, now).ToList();

        await PersistWithUploadsAsync(async uploaded =>
        {
            foreach (var file in request.Images)
                product.Images.Add(await UploadGalleryImageAsync(product.Id, file, product.Images.Count, uploaded, ct));
            foreach (var (value, file) in uploads)
                await SetValueImageAsync(value, file, uploaded, ct);
            _repository.Add(product);
        }, ct);
        return await GetAsync(userId, product.Id, ct);
    }

    public async Task<PagedResult<ProductListItemResponse>> ListAsync(Guid userId,
        ProductListRequest request, CancellationToken ct)
    {
        var shopId = await RequireShopAsync(userId, ct);
        var (products, total) = await _repository.ListAsync(shopId, request, ct);
        return PagedResult<ProductListItemResponse>.Create(products.Select(x => new ProductListItemResponse(
            x.Id, x.CategoryId, x.Category.Name, x.Name, x.Status, x.Variants.Count,
            x.Variants.Select(v => (decimal?)v.OriginalPrice).Min(),
            x.Variants.Select(v => (decimal?)v.DealPrice).Min(),
            x.Variants.Select(v => (decimal?)v.DealPrice).Max(), x.UpdatedAt, MapImages(x))).ToArray(),
            request.Page, request.PageSize, total);
    }

    public async Task<ProductResponse> GetAsync(Guid userId, Guid productId, CancellationToken ct)
        => Map(await RequireProductAsync(userId, productId, false, ct));

    public async Task<ProductResponse> UpdateAsync(Guid userId, Guid productId,
        UpdateProductRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(userId, productId, false, ct);
        await RequireCategoryAsync(request.CategoryId, ct);
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.CategoryId = request.CategoryId;
        Touch(product);
        await _repository.SaveChangesAsync(ct);
        return await GetAsync(userId, productId, ct);
    }

    public async Task DeleteAsync(Guid userId, Guid productId, CancellationToken ct)
    {
        var product = await RequireProductAsync(userId, productId, true, ct);
        if (product.DeletedAt.HasValue) return;
        product.DeletedAt = Now();
        product.Status = ProductStatus.Hidden;
        Touch(product);
        // Preserve variants, images and deal/order relationships for history.
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<ProductResponse> UpdateImagesAsync(Guid userId, Guid productId,
        UpdateProductImagesRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(userId, productId, false, ct);
        if (request.RetainedImageIds.Count + request.Images.Count > ProductLimits.Images)
            throw AppException.BadRequest("At most 10 gallery images are allowed.");
        var existing = product.Images.ToDictionary(x => x.Id);
        if (request.RetainedImageIds.Any(id => !existing.ContainsKey(id)))
            throw AppException.BadRequest("Every retained image must belong to this product.");
        var removed = product.Images.Where(x => !request.RetainedImageIds.Contains(x.Id)).ToArray();
        await PersistWithUploadsAsync(async uploaded =>
        {
            var added = new List<ProductImage>();
            foreach (var file in request.Images)
                added.Add(await UploadGalleryImageAsync(product.Id, file,
                    request.RetainedImageIds.Count + added.Count, uploaded, ct));
            for (var index = 0; index < request.RetainedImageIds.Count; index++)
                existing[request.RetainedImageIds[index]].SortOrder = index;
            _repository.RemoveImages(removed);
            foreach (var image in removed) product.Images.Remove(image);
            foreach (var image in added) product.Images.Add(image);
            _repository.AddImages(added);
            Touch(product);
        }, ct);
        return Map(product);
    }

    public async Task<ProductResponse> AddValuesAsync(Guid userId, Guid productId,
        AddProductValuesRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(userId, productId, false, ct);
        if (request.Attributes.Select(x => x.AttributeId).Distinct().Count() != request.Attributes.Count)
            throw AppException.BadRequest("Each attribute may appear only once.");
        var newValues = request.Attributes.SelectMany(x => x.Values).ToArray();
        await RequireNewValueIdsAsync(newValues, ct);
        var uploads = new List<(ProductAttributeValue Value, IFormFile File)>();
        var added = new List<ProductAttributeValue>();
        foreach (var input in request.Attributes)
        {
            var attribute = product.Attributes.SingleOrDefault(x => x.Id == input.AttributeId)
                ?? throw AppException.BadRequest("Values can only be added to existing attributes of this product.");
            EnsureUniqueNames(attribute.Values.Select(x => x.Value).Concat(input.Values.Select(x => x.Value)),
                "Attribute values");
            foreach (var value in input.Values)
            {
                var entity = NewValue(attribute, value);
                attribute.Values.Add(entity);
                added.Add(entity);
                if (value.Image is not null) uploads.Add((entity, value.Image));
            }
        }
        var existing = product.Variants.Select(x => ProductVariantBuilder.CombinationKey(
            x.VariantValues.Select(v => v.AttributeValueId))).ToHashSet(StringComparer.Ordinal);
        var combinations = ProductVariantBuilder.Combinations(product.Attributes)
            .Where(x => !existing.Contains(ProductVariantBuilder.CombinationKey(x))).ToArray();
        if (product.Variants.Count + combinations.Length > ProductLimits.Variants)
            throw AppException.BadRequest("The product cannot exceed 100 variants.");
        var variants = ProductVariantBuilder.Create(product.Id, combinations, request.Variants, Now());
        await PersistWithUploadsAsync(async uploaded =>
        {
            foreach (var (value, file) in uploads) await SetValueImageAsync(value, file, uploaded, ct);
            _repository.AddValues(added);
            foreach (var variant in variants) product.Variants.Add(variant);
            _repository.AddVariants(variants);
            Touch(product);
        }, ct);
        return Map(product);
    }

    public async Task<ProductResponse> UpdateValueImageAsync(Guid userId, Guid productId,
        Guid valueId, UpdateProductValueImageRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(userId, productId, false, ct);
        var value = product.Attributes.SelectMany(x => x.Values).SingleOrDefault(x => x.Id == valueId)
            ?? throw AppException.NotFound("Product value was not found.");
        await PersistWithUploadsAsync(async uploaded =>
        {
            await SetValueImageAsync(value, request.Image, uploaded, ct);
            Touch(product);
        }, ct);
        return Map(product);
    }

    public async Task<PagedResult<ProductFlashDealResponse>> ListFlashDealsAsync(Guid userId,
        Guid productId, ProductFlashDealListRequest request, CancellationToken ct)
    {
        var shopId = await RequireShopAsync(userId, ct);
        // Owners can still trace deals after soft deletion.
        await RequireProductAsync(userId, productId, true, ct);
        var (deals, total) = await _repository.ListFlashDealsAsync(shopId, productId, request, ct);
        return PagedResult<ProductFlashDealResponse>.Create(deals.Select(x => new ProductFlashDealResponse(
            x.Id, x.Status, x.SaleStartTime, x.OrderEndTime, x.ShopClosingTime, x.CreatedAt,
            x.Variants.OrderBy(v => v.Id).Select(v => new ProductFlashDealVariantResponse(
                v.Id, v.VariantId, v.Variant.Sku,
                v.Variant.VariantValues.OrderBy(a => a.AttributeValue.Attribute.SortOrder)
                    .ThenBy(a => a.AttributeValue.AttributeId)
                    .Select(a => new ProductVariantSelectionResponse(a.AttributeValue.AttributeId,
                        a.AttributeValue.Attribute.Name, a.AttributeValueId, a.AttributeValue.Value)).ToArray(),
                v.OriginalPrice, v.DealPrice, v.DiscountPercent,
                v.TotalQuantity, v.ReservedQuantity, v.SoldQuantity,
                v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity, v.Status)).ToArray())).ToArray(),
            request.Page, request.PageSize, total);
    }

    private async Task<Guid> RequireShopAsync(Guid userId, CancellationToken ct)
        => (await _repository.GetAvailableShopAsync(userId, ct))?.Id
            ?? throw AppException.NotFound("Shop was not found or is unavailable.");

    private async Task<Product> RequireProductAsync(Guid userId, Guid productId,
        bool includeDeleted, CancellationToken ct)
    {
        var shopId = await RequireShopAsync(userId, ct);
        return await _repository.GetOwnedAsync(userId, shopId, productId, includeDeleted, ct)
            ?? throw AppException.NotFound("Product was not found or is unavailable.");
    }

    private async Task RequireCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        if (!await _repository.ActiveCategoryExistsAsync(categoryId, ct))
            throw AppException.BadRequest("An active category is required.");
    }

    private async Task RequireNewValueIdsAsync(IReadOnlyCollection<CreateProductValueRequest> values,
        CancellationToken ct)
    {
        var ids = values.Select(x => x.Id).ToArray();
        if (ids.Distinct().Count() != ids.Length)
            throw AppException.BadRequest("Every new value requires a unique ID.");
        if (ids.Length > 0 && await _repository.AnyValueIdsExistAsync(ids, ct))
            throw AppException.Conflict("A supplied value ID already exists.");
    }

    private static void EnsureUniqueNames(IEnumerable<string> names, string label)
    {
        var normalized = names.Select(x => x.Trim()).ToArray();
        if (normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length)
            throw AppException.BadRequest($"{label} must be unique, ignoring case and surrounding whitespace.");
    }

    private static ProductAttributeValue NewValue(ProductAttribute attribute, CreateProductValueRequest input)
        => new()
        {
            Id = input.Id, AttributeId = attribute.Id, Value = input.Value.Trim(),
            SortOrder = attribute.Values.Select(x => x.SortOrder).DefaultIfEmpty(-1).Max() + 1
        };

    private async Task<ProductImage> UploadGalleryImageAsync(Guid productId, IFormFile file,
        int order, List<string> uploaded, CancellationToken ct)
    {
        var result = await UploadAsync(file, uploaded, ct);
        return new ProductImage
        {
            Id = Guid.NewGuid(), ProductId = productId, ImageUrl = result.Url,
            PublicId = result.PublicId, SortOrder = order
        };
    }

    private async Task SetValueImageAsync(ProductAttributeValue value, IFormFile file,
        List<string> uploaded, CancellationToken ct)
    {
        var result = await UploadAsync(file, uploaded, ct);
        value.ImageUrl = result.Url;
        value.PublicId = result.PublicId;
    }

    private async Task<CloudinaryUploadResult> UploadAsync(IFormFile file, List<string> uploaded, CancellationToken ct)
    {
        try
        {
            var result = await _images.UploadImageAsync(file, ct);
            uploaded.Add(result.PublicId);
            return result;
        }
        catch (ArgumentException)
        {
            throw AppException.BadRequest("The image is empty, too large, unsupported, or has an invalid file signature.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Product image upload failed.");
            throw AppException.ServiceUnavailable("Image upload is temporarily unavailable.");
        }
    }

    private async Task PersistWithUploadsAsync(Func<List<string>, Task> prepare, CancellationToken ct)
    {
        var uploaded = new List<string>();
        try
        {
            await prepare(uploaded);
            // A single EF SaveChanges transaction persists the entire aggregate.
            await _repository.SaveChangesAsync(ct);
        }
        catch
        {
            foreach (var publicId in uploaded)
            {
                try
                {
                    if (!await _images.DeleteImageAsync(publicId))
                        _logger.LogWarning("Could not clean up uncommitted product image {PublicId}.", publicId);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Could not clean up uncommitted product image {PublicId}.", publicId);
                }
            }
            throw;
        }
        // Previously saved assets are retained: historical moderation records may reference their URLs.
    }

    private DateTime Now() => _clock.GetUtcNow().UtcDateTime;
    private void Touch(Product product)
    {
        product.UpdatedAt = Now();
        product.Version = Guid.NewGuid();
    }
    

    private static IReadOnlyList<ProductImageResponse> MapImages(Product product)
        => product.Images.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new ProductImageResponse(x.Id, x.ImageUrl, "product", null, null, null, null))
            .Concat(product.Attributes.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .SelectMany(a => a.Values.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                    .Where(v => v.ImageUrl is not null)
                    .Select(v => new ProductImageResponse(v.Id, v.ImageUrl!, "value", a.Id, v.Id, a.Name, v.Value))))
            .ToArray();

    private static ProductResponse Map(Product product)
    {
        var review = product.ContentRevisions.OrderByDescending(x => x.RevisionNumber).FirstOrDefault();
        return new ProductResponse(product.Id, product.ShopId, product.CategoryId, product.Category.Name,
            product.Name, product.Description, product.Status, product.CreatedAt, product.UpdatedAt,
            MapImages(product),
            product.Attributes.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .Select(a => new ProductAttributeResponse(a.Id, a.Name,
                    a.Values.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                        .Select(v => new ProductValueResponse(v.Id, v.Value, v.ImageUrl)).ToArray())).ToArray(),
            product.Variants.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                .Select(v => new ProductVariantResponse(v.Id, v.Sku, v.OriginalPrice, v.DealPrice,
                    v.IsActive, v.VariantValues.Select(x => x.AttributeValueId).OrderBy(id => id).ToArray())).ToArray(),
            review is null ? null : new ProductReviewResponse(review.Status, review.ReviewNote, review.ReviewedAt));
    }
    
    public async Task<ProductResponse> UpdateVariantAsync(Guid userId, Guid productId,
        Guid variantId, UpdateProductVariantRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(userId, productId, false, ct);
        var variant = product.Variants.SingleOrDefault(v => v.Id == variantId)
                      ?? throw AppException.NotFound("Product variant was not found.");
        if (!request.IsActive && product.Status == ProductStatus.Active &&
            !product.Variants.Any(v => v.Id != variantId && v.IsActive))
        {
            throw AppException.BadRequest("An active product must have at least one active variant.");
        }
        var maxDealPrice = await _repository.GetMaxActiveFlashDealPriceForVariantAsync(variantId, ct);
        if (maxDealPrice.HasValue && request.OriginalPrice <= maxDealPrice.Value)
        {
            throw AppException.BadRequest(
                $"Original price cannot be less than or equal to the deal price ({maxDealPrice.Value:N0} đ) of an active or pending flash deal.");
        }
        variant.OriginalPrice = request.OriginalPrice;
        variant.DealPrice = request.DealPrice;
        variant.IsActive = request.IsActive;
        Touch(product);
        await _repository.SaveChangesAsync(ct);
        return Map(product);
    }
    
    public async Task<ProductResponse> UpdateStatusAsync(Guid userId, Guid productId,
        UpdateProductStatusRequest request, CancellationToken ct)
    {
        var product = await RequireProductAsync(userId, productId, true, ct);
        if (request.Status == ProductStatus.Active)
        {
            await RequireCategoryAsync(product.CategoryId, ct);
            if (!product.Variants.Any(v => v.IsActive))
            {
                throw AppException.BadRequest("Cannot activate a product without any active variants.");
            }
            product.DeletedAt = null;
            product.Status = ProductStatus.Active;
        }
        else
        {
            product.Status = ProductStatus.Hidden;
        }
        Touch(product);
        await _repository.SaveChangesAsync(ct);
        return Map(product);
    }
}

