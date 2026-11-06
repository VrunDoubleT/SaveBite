using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopSuspensionCleanupService(
    IFlashDealService flashDeals,
    IShopOrderService shopOrders) : IShopSuspensionCleanupService
{
    public async Task CleanupAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        await flashDeals.CancelOpenDealsForSuspendedShopAsync(shopId, cancellationToken);
        await shopOrders.CancelPendingOrdersForSuspendedShopAsync(shopId, cancellationToken);
    }
}
