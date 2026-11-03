using System.Text.Json;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Services.Implementations;

// Called before the order transaction commits, never as a best-effort post-commit action.
public static class OrderNotificationOutbox
{
    public static void Stage(AppDbContext db, Order order, OrderStatus? before)
    {
        void Add(NotificationTarget target, CreateNotificationCommand command, string recipient)
        {
            var key = $"order-notification:{order.Id}:{order.Status}:{recipient}";
            if (db.Set<OrderEventOutbox>().Local.Any(m => m.EventKey == key)) return;
            var id = Guid.NewGuid();
            db.Add(new OrderEventOutbox { Id = id, OrderId = order.Id, EventKey = key,
                RoutingKey = RabbitMqRoutingKeys.NotificationRequested, CreatedAt = DateTime.UtcNow,
                PayloadJson = JsonSerializer.Serialize(new NotificationRequestedEvent(id, target, command)) });
        }
        var id = order.Id;
        if (order.Status == OrderStatus.Pending)
        {
            var command = new CreateNotificationCommand(NotificationType.OrderCreated, "New Order Awaiting Confirmation",
                "A customer has placed a new order. Please confirm it within 5 minutes.",
                DeduplicationKey: $"shop-order-pending:{id}");
            Add(NotificationTarget.ShopOwner(order.ShopId), command with { ActionUrl = NotificationActionUrls.StoreOwnerOrder(id) }, "owner");
            Add(NotificationTarget.ShopStaff(order.ShopId), command with { ActionUrl = NotificationActionUrls.ShopStaffOrder(order.ShopId, id) }, "staff");
        }
        if (order.Status == OrderStatus.Cancelled || (order.Status == OrderStatus.Expired && before == OrderStatus.AwaitingPayment))
        {
            var expired = order.Status == OrderStatus.Expired;
            var type = expired ? NotificationType.OrderPaymentExpired : NotificationType.OrderCancelledByCustomer;
            var title = expired ? "Order payment expired" : "Order cancelled";
            var body = expired ? $"The payment window for order {order.OrderCode} has expired."
                : $"The customer cancelled order {order.OrderCode}.";
            Add(NotificationTarget.User(order.CustomerId), new(type, title, body, $"/orders/{id}", $"order:{id}:{type}:customer"), "customer");
            if (order.ConfirmedAt.HasValue || before == OrderStatus.Pending || order.StatusHistories.Any(h => h.ToStatus == OrderStatus.Pending))
            {
                Add(NotificationTarget.ShopOwner(order.ShopId), new(type, title, body, $"/store-owner/orders/{id}", $"order:{id}:{type}:owner"), "owner");
                Add(NotificationTarget.ShopStaff(order.ShopId), new(type, title, body, $"/staff/shops/{order.ShopId}/orders/{id}", $"order:{id}:{type}:staff"), "staff");
            }
            return;
        }
        var notice = order.Status switch
        {
            OrderStatus.Pending when before == OrderStatus.AwaitingPayment => (NotificationType.PaymentSucceeded, "Payment Succeeded", "Your payment was successful and your order is awaiting store confirmation.", $"order-paid:{id}"),
            OrderStatus.Confirmed => (NotificationType.OrderAccepted, "Order Confirmed", "The store has accepted your order. Open order details to follow its progress.", $"order-confirm:{id}"),
            OrderStatus.Rejected => (NotificationType.OrderRejected, "Order Rejected", "The store could not accept your order. Please check your order details.", $"order-reject:{id}"),
            OrderStatus.Expired when before == OrderStatus.Pending => (NotificationType.OrderRejected, "Order Rejected", "The store could not accept your order. Please check your order details.", $"order-reject:{id}"),
            OrderStatus.ReadyForPickup => (NotificationType.PickupReminder, "Your Order is Ready for Pickup!", "Please visit the store to pick up your order.", $"order-ready:{id}"),
            OrderStatus.Completed => (NotificationType.OrderCompleted, "Order Completed", "Thank you for using SaveBite! Feel free to leave a review for your meal.", $"order-complete:{id}"),
            OrderStatus.NoShow => (NotificationType.OrderNoShow, "Order Pickup Missed", "You did not pick up your order before the deadline.", $"order-noshow:{id}"),
            _ => ((NotificationType Type, string Title, string Body, string Key)?)null
        };
        if (notice is { } n) Add(NotificationTarget.OrderCustomer(id), new CreateNotificationCommand(
            n.Type, n.Title, n.Body, NotificationActionUrls.CustomerOrder(id), n.Key), "customer");
    }
    public static void StageRemainingPayment(AppDbContext db, Order order)
    {
        var id = Guid.NewGuid();
        db.Add(new OrderEventOutbox { Id = id, OrderId = order.Id, CreatedAt = DateTime.UtcNow,
            EventKey = $"order-notification:{order.Id}:remaining-paid:customer", RoutingKey = RabbitMqRoutingKeys.NotificationRequested,
            PayloadJson = JsonSerializer.Serialize(new NotificationRequestedEvent(id, NotificationTarget.OrderCustomer(order.Id),
                new CreateNotificationCommand(NotificationType.PaymentSucceeded, "Payment Succeeded", "The remaining payment for your order has been verified.",
                    NotificationActionUrls.CustomerOrder(order.Id), $"order-remaining-paid:{order.Id}"))) });
    }

}
