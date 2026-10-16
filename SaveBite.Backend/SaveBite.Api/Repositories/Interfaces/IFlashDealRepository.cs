using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IFlashDealRepository
{
    // Existing customer/guest 
    Task<FlashDeal?> GetActiveDealByIdAsync(Guid dealId, CancellationToken cancellationToken = default);
    Task<List<FlashDeal>> GetActiveDealsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<List<FlashDeal>> GetAllActiveDealsAsync(CancellationToken cancellationToken = default);
    Task<(List<FlashDeal> Deals, bool HasOlder)> GetDealsByCursorAsync(
        DateTime? cursor1,
        DateTime? cursor2,
        CursorMode mode,
        int limit,
        CancellationToken cancellationToken = default);

    // Store Owner 
    Task<PagedResult<FlashDeal>> GetShopDealsAsync(Guid shopId, ShopFlashDealQueryRequest request, CancellationToken cancellationToken = default);
    Task<FlashDeal?> GetShopDealByIdAsync(Guid shopId, Guid dealId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingDealAsync(Guid shopId, Guid productId, DateTime startTime, DateTime endTime, Guid? excludeDealId = null, CancellationToken cancellationToken = default);
    Task<Product?> GetProductForDealCreationAsync(Guid shopId, Guid productId, CancellationToken cancellationToken = default);
    Task<List<Product>> GetShopEligibleProductsAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task AddAsync(FlashDeal deal, CancellationToken cancellationToken = default);
    Task UpdateAsync(FlashDeal deal, CancellationToken cancellationToken = default);
}