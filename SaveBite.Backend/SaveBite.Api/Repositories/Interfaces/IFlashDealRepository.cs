using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IFlashDealRepository
{
    Task<FlashDeal?> GetActiveDealByIdAsync(Guid dealId, CancellationToken cancellationToken = default);
    Task<List<FlashDeal>> GetActiveDealsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<List<FlashDeal>> GetAllActiveDealsAsync(CancellationToken cancellationToken = default);
    Task<(List<FlashDeal> Deals, bool HasOlder)> GetDealsByCursorAsync(
        DateTime? cursor1,
        DateTime? cursor2,
        CursorMode mode,
        int limit,
        CancellationToken cancellationToken = default);
}
