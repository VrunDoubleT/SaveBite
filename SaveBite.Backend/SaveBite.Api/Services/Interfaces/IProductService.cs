using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Common;

public interface IProductService
{
    Task<ProductSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct);
    Task<ProductOptionsResponse> GetOptionsAsync(Guid userId, CancellationToken ct);
    Task<ProductResponse> CreateAsync(Guid userId, CreateProductRequest request, CancellationToken ct);
    Task<PagedResult<ProductListItemResponse>> ListAsync(Guid userId, ProductListRequest request, CancellationToken ct);
    Task<ProductResponse> GetAsync(Guid userId, Guid productId, CancellationToken ct);
    Task<ProductResponse> UpdateAsync(Guid userId, Guid productId, UpdateProductRequest request, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid productId, CancellationToken ct);
    Task<PagedResult<ProductFlashDealResponse>> ListFlashDealsAsync(Guid userId, Guid productId, ProductFlashDealListRequest request, CancellationToken ct);
    Task<ProductResponse> UpdateImagesAsync(Guid userId, Guid productId, UpdateProductImagesRequest request, CancellationToken ct);
    Task<ProductResponse> AddValuesAsync(Guid userId, Guid productId, AddProductValuesRequest request, CancellationToken ct);
    Task<ProductResponse> UpdateVariantAsync(Guid userId, Guid productId, Guid variantId, UpdateProductVariantRequest request, CancellationToken ct);
    Task<ProductResponse> UpdateStatusAsync(Guid userId, Guid productId, UpdateProductStatusRequest request, CancellationToken ct);
    Task<ProductResponse> UpdateValueImageAsync(Guid userId, Guid productId, Guid valueId, UpdateProductValueImageRequest request, CancellationToken ct);
}   
