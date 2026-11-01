using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IProductRepository
{
    Task<SaveBite.Backend.Models.Responses.ProductOptionsResponse> GetOptionsAsync(Guid userId, CancellationToken ct);
    Task<SaveBite.Backend.Models.Responses.ProductShopOption?> GetAvailableShopAsync(Guid userId, CancellationToken ct);
    Task<SaveBite.Backend.Models.Responses.ProductSummaryResponse> GetSummaryAsync(Guid shopId, CancellationToken ct);
    Task<bool> ActiveCategoryExistsAsync(Guid categoryId, CancellationToken ct);
    Task<bool> AnyValueIdsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<Product?> GetOwnedAsync(Guid userId, Guid shopId, Guid productId, bool includeDeleted, CancellationToken ct);
    Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(Guid shopId, ProductListRequest request, CancellationToken ct);
    Task<(IReadOnlyList<FlashDeal> Items, int Total)> ListFlashDealsAsync(Guid shopId, Guid productId, ProductFlashDealListRequest request, CancellationToken ct);
    void Add(Product product);
    void AddValues(IEnumerable<ProductAttributeValue> values);
    void AddVariants(IEnumerable<ProductVariant> variants);
    void AddImages(IEnumerable<ProductImage> images);
    void RemoveImages(IEnumerable<ProductImage> images);
    Task<decimal?> GetMaxActiveFlashDealPriceForVariantAsync(Guid variantId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    
    // Manage Products.
    Task<(IReadOnlyList<Product> Items, int Total)> GetProductListAsync(string? keyword, int page, int pageSize, CancellationToken ct);
    Task<Product?> GetProductContentAsync(Guid productId, CancellationToken ct);
    Task<Product?> GetProductForViolationRemovalAsync(Guid productId, CancellationToken ct);
    Task RemoveProductForViolationAndCancelDealsAsync(Product product, CancellationToken ct);
    Task<ProductContentRevision?> GetLatestRevisionForReviewAsync(Guid productId, CancellationToken ct);
    void AddContentRevision(ProductContentRevision revision);
}
