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
            var command = new CreateNotificationCommand(
                NotificationType.OrderCreated,
                NotificationMessages.Orders.NewOrderAwaitingConfirmationTitle,
                NotificationMessages.Orders.NewOrderAwaitingConfirmationBody,
                DeduplicationKey: $"shop-order-pending:{id}");
            Add(NotificationTarget.ShopOwner(order.ShopId), command with { ActionUrl = NotificationActionUrls.StoreOwnerOrder(id) }, "owner");
            Add(NotificationTarget.ShopStaff(order.ShopId), command with { ActionUrl = NotificationActionUrls.ShopStaffOrder(order.ShopId, id) }, "staff");
        }
        if (order.Status == OrderStatus.Cancelled || (order.Status == OrderStatus.Expired && before == OrderStatus.AwaitingPayment))
        {
            var expired = order.Status == OrderStatus.Expired;
            var type = expired ? NotificationType.OrderPaymentExpired : NotificationType.OrderCancelledByCustomer;
            var title = expired ? NotificationMessages.Orders.OrderPaymentExpiredTitle : NotificationMessages.Orders.OrderCancelledTitle;
            var body = expired
                ? NotificationMessages.Orders.OrderPaymentExpiredBody(order.OrderCode)
                : NotificationMessages.Orders.OrderCancelledBody(order.OrderCode);
            Add(NotificationTarget.User(order.CustomerId), new(type, title, body, NotificationActionUrls.CustomerOrder(id), $"order:{id}:{type}:customer"), "customer");
            if (order.ConfirmedAt.HasValue || before == OrderStatus.Pending || order.StatusHistories.Any(h => h.ToStatus == OrderStatus.Pending))
            {
                Add(NotificationTarget.ShopOwner(order.ShopId), new(type, title, body, NotificationActionUrls.StoreOwnerOrder(id), $"order:{id}:{type}:owner"), "owner");
                Add(NotificationTarget.ShopStaff(order.ShopId), new(type, title, body, NotificationActionUrls.ShopStaffOrder(order.ShopId, id), $"order:{id}:{type}:staff"), "staff");
            }
            return;
        }
        var notice = order.Status switch
        {
            OrderStatus.Pending when before == OrderStatus.AwaitingPayment => (NotificationType.PaymentSucceeded, NotificationMessages.Payments.PaymentSucceededTitle, NotificationMessages.Payments.PaymentSucceededAwaitingConfirmationBody, $"order-paid:{id}"),
            OrderStatus.Confirmed => (NotificationType.OrderAccepted, NotificationMessages.Orders.OrderConfirmedTitle, NotificationMessages.Orders.OrderConfirmedBody, $"order-confirm:{id}"),
            OrderStatus.Rejected => (NotificationType.OrderRejected, NotificationMessages.Orders.OrderRejectedTitle, NotificationMessages.Orders.OrderRejectedBody, $"order-reject:{id}"),
            OrderStatus.Expired when before == OrderStatus.Pending => (NotificationType.OrderRejected, NotificationMessages.Orders.OrderRejectedTitle, NotificationMessages.Orders.OrderRejectedBody, $"order-reject:{id}"),
            OrderStatus.ReadyForPickup => (NotificationType.PickupReminder, NotificationMessages.Orders.ReadyForPickupTitle, NotificationMessages.Orders.ReadyForPickupBody, $"order-ready:{id}"),
            OrderStatus.Completed => (NotificationType.OrderCompleted, NotificationMessages.Orders.OrderCompletedTitle, NotificationMessages.Orders.OrderCompletedBody, $"order-complete:{id}"),
            OrderStatus.NoShow => (NotificationType.OrderNoShow, NotificationMessages.Orders.OrderNoShowTitle, NotificationMessages.Orders.OrderNoShowBody, $"order-noshow:{id}"),
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
                new CreateNotificationCommand(NotificationType.PaymentSucceeded, NotificationMessages.Payments.PaymentSucceededTitle, NotificationMessages.Payments.RemainingPaymentVerifiedBody,
                    NotificationActionUrls.CustomerOrder(order.Id), $"order-remaining-paid:{order.Id}"))) });
    }

}
