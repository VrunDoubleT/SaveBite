namespace SaveBite.Backend.Services.Interfaces;

// Handle pending-order scheduling and notify eligible order subscribers.
public interface IShopOrderEventService
{
    Task NotifyAsync(Guid shopId, Guid orderId, string reason, CancellationToken ct = default);
}
