using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Schedule pending-order confirmation and publish authorized order notifications.
public sealed class ShopOrderEventService : IShopOrderEventService
{
    private readonly ICheckoutRepository _orders;
    private readonly IShopOrderRepository _shops;
    private readonly IShopOrderConfirmationScheduler _scheduler;
    private readonly IRealtimePublisher _realtime;
    private readonly ILogger<ShopOrderEventService> _logger;

    public ShopOrderEventService(ICheckoutRepository orders, IShopOrderRepository shops,
        IShopOrderConfirmationScheduler scheduler, IRealtimePublisher realtime, ILogger<ShopOrderEventService> logger)
    { _orders = orders; _shops = shops; _scheduler = scheduler; _realtime = realtime; _logger = logger; }

    // Schedule one confirmation timeout for a paid pending order when its broker event is consumed.
    public async Task HandlePendingAsync(Guid orderId, Guid shopId, CancellationToken ct = default)
    {
        await _orders.WithOrderLockAsync(orderId, async () =>
        {
            var order = await _orders.GetOrderAsync(orderId, null, ct);
            if (order == null || order.ShopId != shopId || order.Status != OrderStatus.Pending) return false;
            var payment = CheckoutOrderMapper.RequiredPayment(order);
            if (payment?.Status != PaymentStatus.Succeeded || order.PaymentReviewRequired) return false;
            // Serialize duplicate deliveries with confirmation; a delayed event never extends the five-minute window.
            if (order.ConfirmationExpirationJobId == null)
            {
                order.ConfirmationExpirationJobId = _scheduler.Schedule(order.Id, (payment.PaidAt ?? order.CreatedAt).AddMinutes(5));
                await _orders.SaveAsync(ct);
            }
            return true;
        }, ct);
    }

    // Notify eligible shop operators and the owning customer after an order change commits.
    public async Task NotifyAsync(Guid shopId, Guid orderId, string reason, CancellationToken ct = default)
    {
        IReadOnlyList<Guid> recipients;
        try
        {
            recipients = await _shops.RealtimeRecipientsAsync(shopId, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Could not find realtime recipients for shop {ShopId}, order {OrderId}.", shopId, orderId);
            recipients = Array.Empty<Guid>();
        }
        var pendingChanged = reason is RealtimeEvents.OrderReasons.Pending or RealtimeEvents.OrderReasons.Confirm
            or RealtimeEvents.OrderReasons.Reject or RealtimeEvents.OrderReasons.Cancelled
            or RealtimeEvents.OrderReasons.ConfirmationExpired;
        foreach (var recipient in recipients)
        {
            await _realtime.SendToUserAsync(recipient, RealtimeEvents.ShopOrdersChanged, new { ShopId = shopId, OrderId = orderId, Reason = reason });
            if (pendingChanged)
                await _realtime.SendToUserAsync(recipient, RealtimeEvents.ShopPendingOrdersChanged, new { ShopId = shopId });
        }
        if (reason is RealtimeEvents.OrderReasons.Pending or RealtimeEvents.OrderReasons.PaymentSucceeded
            or RealtimeEvents.OrderReasons.Confirm or RealtimeEvents.OrderReasons.Reject
            or RealtimeEvents.OrderReasons.Preparing or RealtimeEvents.OrderReasons.ReadyForPickup
            or RealtimeEvents.OrderReasons.Complete or RealtimeEvents.OrderReasons.ConfirmationExpired
            or RealtimeEvents.OrderReasons.NoShow or RealtimeEvents.OrderReasons.RefundChanged)
        {
            try
            {
                var customerId = await _shops.CustomerRecipientAsync(shopId, orderId, ct);
                if (customerId.HasValue)
                    await _realtime.SendToUserAsync(customerId.Value, RealtimeEvents.CustomerOrdersChanged,
                        new { ShopId = shopId, OrderId = orderId, Reason = reason });
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { _logger.LogWarning(ex, "Could not notify the customer for order {OrderId}.", orderId); }
        }
    }
}
