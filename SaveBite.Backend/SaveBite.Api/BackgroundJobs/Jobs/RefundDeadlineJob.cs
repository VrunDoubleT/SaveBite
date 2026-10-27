using SaveBite.Backend.Constants;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public class RefundDeadlineJob
{
    private readonly IRefundService _refunds;
    private readonly IShopOrderEventService _events;
    public RefundDeadlineJob(IRefundService refunds, IShopOrderEventService events) { _refunds = refunds; _events = events; }
    // Notify customer and shop only after the timeout transition commits; retries are safe.
    public async Task ExecuteAsync(Guid refundId, DateTime deadline, CancellationToken ct)
    {
        var result = await _refunds.ExpireDeadlineAsync(refundId, deadline, ct);
        if (result != null) await _events.NotifyAsync(result.ShopId, result.OrderId, RealtimeEvents.OrderReasons.RefundChanged, ct);
    }
}
