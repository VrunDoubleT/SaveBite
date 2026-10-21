using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Apply the scheduled no-show check through the shop order workflow.
public sealed class ShopOrderNoShowJob
{
    private readonly IShopOrderService _orders;
    public ShopOrderNoShowJob(IShopOrderService orders) { _orders = orders; }

    // Process only this order; the service records NoShow and publishes its lifecycle event.
    public Task ExecuteAsync(Guid orderId, CancellationToken ct) => _orders.MarkNoShowAsync(orderId, ct);
}
