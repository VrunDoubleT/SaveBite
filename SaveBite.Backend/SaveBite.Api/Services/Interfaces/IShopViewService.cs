using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IShopViewService
{
    Task<List<NearbyShopResponse>> GetNearbyShopsAsync(NearbyShopsRequest request, CancellationToken cancellationToken = default);
    Task<ShopProfileResponse> GetShopProfileAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<StoreReviewsSummaryResponse> GetStoreReviewsAsync(Guid shopId, StoreReviewsQueryRequest request, CancellationToken cancellationToken = default);
}
