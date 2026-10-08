using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IFlashDealRepository
{
    Task<FlashDeal?> GetActiveDealByIdAsync(Guid dealId, CancellationToken cancellationToken = default);
    Task<List<FlashDeal>> GetActiveDealsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<List<FlashDeal>> GetAllActiveDealsAsync(CancellationToken cancellationToken = default);
}
