using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Expire an unconfirmed pending order when its scheduled confirmation check runs.
public sealed class ShopOrderConfirmationJob
{
    private readonly IShopOrderService _orders;
    public ShopOrderConfirmationJob(IShopOrderService orders) { _orders = orders; }

    // Process only this order; existing settlement rules release inventory and create the required refund once.
    public Task ExecuteAsync(Guid orderId, CancellationToken ct) => _orders.ExpireConfirmationAsync(orderId, ct);
}
