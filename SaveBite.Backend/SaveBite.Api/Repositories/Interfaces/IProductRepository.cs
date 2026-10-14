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
    Task SaveChangesAsync(CancellationToken ct);
}
