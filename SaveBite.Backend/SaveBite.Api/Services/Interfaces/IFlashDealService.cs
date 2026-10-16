using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IFlashDealService
{
    // Customer/guest methods:
    Task<List<FlashDealResponse>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<FlashDealCursorListResponse> GetNearbyAsync(FlashDealCursorRequest request, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<FlashDealResponse> GetByIdAsync(Guid dealId, CancellationToken cancellationToken = default);

    // Store owner:
    Task<PagedResult<ShopFlashDealListItemResponse>> GetShopDealsAsync(Guid userId, ShopFlashDealQueryRequest request, CancellationToken cancellationToken = default);
    Task<ShopFlashDealDetailResponse> GetShopDealDetailAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default);
    Task<ShopFlashDealDetailResponse> CreateDealAsync(Guid userId, CreateFlashDealRequest request, CancellationToken cancellationToken = default);
    Task<ShopFlashDealDetailResponse> UpdateDealAsync(Guid userId, Guid dealId, UpdateFlashDealRequest request, CancellationToken cancellationToken = default);
    Task CancelDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default);
    Task HideDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default);
    Task UnhideDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default);
    Task<List<ShopEligibleProductResponse>> GetShopEligibleProductsAsync(Guid userId, CancellationToken cancellationToken = default);
}