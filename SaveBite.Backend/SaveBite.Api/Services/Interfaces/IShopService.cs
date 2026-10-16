using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IShopService
{
    Task<PagedResult<NearbyShopResponse>> GetNearbyShopsAsync(NearbyShopsRequest request, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ShopProfileResponse> GetShopProfileAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<StoreReviewsSummaryResponse> GetStoreReviewsAsync(Guid shopId, StoreReviewsQueryRequest request, CancellationToken cancellationToken = default);
    Task<ShopPaymentConfigResponse> GetOwnerPaymentConfigAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<ShopPaymentConfigResponse> UpdateOwnerPaymentConfigAsync(Guid ownerUserId, UpdateShopPaymentConfigRequest request, CancellationToken cancellationToken = default);
}
