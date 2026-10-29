using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
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
    private readonly INotificationQueue _notifications;
    private readonly ILogger<ShopOrderEventService> _logger;

    public ShopOrderEventService(
        ICheckoutRepository orders,
        IShopOrderRepository shops,
        IShopOrderConfirmationScheduler scheduler,
        IRealtimePublisher realtime,
        INotificationQueue notifications,
        ILogger<ShopOrderEventService> logger)
    {
        _orders = orders;
        _shops = shops;
        _scheduler = scheduler;
        _realtime = realtime;
        _notifications = notifications;
        _logger = logger;
    }

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
            await TrySendRealtimeAsync(recipient, RealtimeEvents.ShopOrdersChanged, new { ShopId = shopId, OrderId = orderId, Reason = reason }, ct);
            if (pendingChanged)
                await TrySendRealtimeAsync(recipient, RealtimeEvents.ShopPendingOrdersChanged, new { ShopId = shopId }, ct);
        }

        if (reason == RealtimeEvents.OrderReasons.Pending)
        {
            // Separate targets preserve the correct workspace link for each role; one failed publish must not skip the other.
            await TryQueueNotificationAsync(
                NotificationTarget.ShopOwner(shopId),
                new CreateNotificationCommand(NotificationType.OrderCreated,
                    "New Order Awaiting Confirmation",
                    "A customer has placed a new order. Please confirm it within 5 minutes.",
                    ActionUrl: NotificationActionUrls.StoreOwnerOrder(orderId),
                    DeduplicationKey: $"shop-order-pending:{orderId}"), orderId, ct);
            await TryQueueNotificationAsync(
                NotificationTarget.ShopStaff(shopId),
                new CreateNotificationCommand(NotificationType.OrderCreated,
                    "New Order Awaiting Confirmation",
                    "A customer has placed a new order. Please confirm it within 5 minutes.",
                    ActionUrl: NotificationActionUrls.ShopStaffOrder(shopId, orderId),
                    DeduplicationKey: $"shop-order-pending:{orderId}"), orderId, ct);
        }

        if (reason is RealtimeEvents.OrderReasons.Pending or RealtimeEvents.OrderReasons.PaymentSucceeded
            or RealtimeEvents.OrderReasons.Confirm or RealtimeEvents.OrderReasons.Reject
            or RealtimeEvents.OrderReasons.Preparing or RealtimeEvents.OrderReasons.ReadyForPickup
            or RealtimeEvents.OrderReasons.Complete or RealtimeEvents.OrderReasons.ConfirmationExpired
            or RealtimeEvents.OrderReasons.NoShow)
        {
            try
            {
                var customerId = await _shops.CustomerRecipientAsync(shopId, orderId, ct);
                if (customerId.HasValue)
                {
                    await TrySendRealtimeAsync(customerId.Value, RealtimeEvents.CustomerOrdersChanged,
                        new { ShopId = shopId, OrderId = orderId, Reason = reason }, ct);

                    var customerCommand = reason switch
                    {
                        RealtimeEvents.OrderReasons.PaymentSucceeded => new CreateNotificationCommand(
                            NotificationType.PaymentSucceeded,
                            "Payment Succeeded",
                            "Your payment was successful and your order is awaiting store confirmation.",
                            ActionUrl: NotificationActionUrls.CustomerOrder(orderId),
                            DeduplicationKey: $"order-paid:{orderId}"),
                        RealtimeEvents.OrderReasons.Confirm => new CreateNotificationCommand(
                            NotificationType.OrderAccepted,
                            "Order Confirmed",
                            "The store has accepted your order. Open order details to follow its progress.",
                            ActionUrl: NotificationActionUrls.CustomerOrder(orderId),
                            DeduplicationKey: $"order-confirm:{orderId}"),
                        RealtimeEvents.OrderReasons.Reject or RealtimeEvents.OrderReasons.ConfirmationExpired => new CreateNotificationCommand(
                            NotificationType.OrderRejected,
                            "Order Rejected",
                            "The store could not accept your order. Please check your order details.",
                            ActionUrl: NotificationActionUrls.CustomerOrder(orderId),
                            DeduplicationKey: $"order-reject:{orderId}"),
                        RealtimeEvents.OrderReasons.ReadyForPickup => new CreateNotificationCommand(
                            NotificationType.PickupReminder,
                            "Your Order is Ready for Pickup!",
                            "Please visit the store to pick up your order.",
                            ActionUrl: NotificationActionUrls.CustomerOrder(orderId),
                            DeduplicationKey: $"order-ready:{orderId}"),
                        RealtimeEvents.OrderReasons.Complete => new CreateNotificationCommand(
                            NotificationType.OrderCompleted,
                            "Order Completed",
                            "Thank you for using SaveBite! Feel free to leave a review for your meal.",
                            ActionUrl: NotificationActionUrls.CustomerOrder(orderId),
                            DeduplicationKey: $"order-complete:{orderId}"),
                        RealtimeEvents.OrderReasons.NoShow => new CreateNotificationCommand(
                            NotificationType.OrderNoShow,
                            "Order Pickup Missed",
                            "You did not pick up your order before the deadline.",
                            ActionUrl: NotificationActionUrls.CustomerOrder(orderId),
                            DeduplicationKey: $"order-noshow:{orderId}"),
                        _ => null
                    };

                    if (customerCommand != null)
                    {
                        await TryQueueNotificationAsync(
                            NotificationTarget.OrderCustomer(orderId), customerCommand, orderId, ct);
                    }
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

    // Publishing is best effort after commit; broker failures never undo an already completed order operation.
    private async Task TryQueueNotificationAsync(NotificationTarget target, CreateNotificationCommand command, Guid orderId, CancellationToken ct)
    {
        try { await _notifications.EnqueueAsync(target, command, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        { _logger.LogWarning(ex, "Could not publish {NotificationType} notification for order {OrderId}.", command.Type, orderId); }
    }

}
