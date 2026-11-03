using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Push best-effort view refreshes after commit. Persistent notifications are staged in the order transaction.
public sealed class ShopOrderEventService : IShopOrderEventService
{
    private readonly IShopOrderRepository _shops;
    private readonly IRealtimePublisher _realtime;
    private readonly ILogger<ShopOrderEventService> _logger;

    public ShopOrderEventService(
        ICheckoutRepository orders,
        IShopOrderRepository shops,
        IRealtimePublisher realtime,
        INotificationQueue notifications,
        ILogger<ShopOrderEventService> logger)
    {
        _shops = shops;
        _realtime = realtime;
        _logger = logger;
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
            await TrySendRealtimeAsync(recipient, RealtimeEvents.ShopOrdersChanged, new { ShopId = shopId, OrderId = orderId, Reason = reason }, ct);
            if (pendingChanged)
                await TrySendRealtimeAsync(recipient, RealtimeEvents.ShopPendingOrdersChanged, new { ShopId = shopId }, ct);
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
                {
                    await TrySendRealtimeAsync(customerId.Value, RealtimeEvents.CustomerOrdersChanged,
                        new { ShopId = shopId, OrderId = orderId, Reason = reason }, ct);
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { _logger.LogWarning(ex, "Could not notify the customer for order {OrderId}.", orderId); }
        }
    }
    // A view-refresh delivery failure must not prevent publishing the customer's persistent notification.
    private async Task TrySendRealtimeAsync(Guid userId, string eventType, object payload, CancellationToken ct)
    {
        try { await _realtime.SendToUserAsync(userId, eventType, payload); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        { _logger.LogWarning(ex, "Could not send order realtime event {EventType} to user {UserId}.", eventType, userId); }
    }

}
