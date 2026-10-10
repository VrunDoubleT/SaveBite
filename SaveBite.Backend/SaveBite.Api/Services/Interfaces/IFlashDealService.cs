using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IFlashDealService
{
    Task<List<FlashDealResponse>> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<FlashDealCursorListResponse> GetNearbyAsync(FlashDealCursorRequest request, CancellationToken cancellationToken = default);
    Task<FlashDealResponse> GetByIdAsync(Guid dealId, CancellationToken cancellationToken = default);
}