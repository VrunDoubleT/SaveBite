using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IShopFollowService
{
    Task<ShopFollowResponse> FollowAsync(Guid customerId, Guid shopId, CancellationToken ct = default);
    Task UnfollowAsync(Guid customerId, Guid shopId, CancellationToken ct = default);
    Task<ShopFollowResponse> UpdateSettingsAsync(Guid customerId, Guid shopId, ShopNotificationStatus status, CancellationToken ct = default);
    Task<PagedResult<ShopFollowResponse>> GetFollowedShopsAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default);
}
