using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class PickupDeadlineReminderJob(ICheckoutRepository orders, INotificationQueue notifications,
    TimeProvider clock, SaveBite.Backend.Data.AppDbContext db)
{
    public async Task ExecuteAsync(Guid orderId, DateTime deadline, CancellationToken ct)
    {
        await orders.WithOrderLockAsync(orderId, async () =>
        {
            var order = await orders.GetOrderAsync(orderId, null, ct);
            var now = clock.GetUtcNow().UtcDateTime;
            if (order == null || (order.PickupReminderEmailSentAt != null || order.PickupReminderEmailQueuedAt != null) || order.HardDeadlineAt?.Ticks / 10 != deadline.Ticks / 10
                || !Eligible(order.Status) || order.Customer.Status != UserStatus.Active
                || order.Customer.CustomerStatus != CustomerStatus.Active || now >= deadline) return false;
            var due = PickupReminderPolicy.DueAt(order.ConfirmedAt ?? order.CreatedAt, deadline);
            if (now < due) throw new InvalidOperationException("Pickup reminder ran before its scheduled time.");
            var localDeadline = deadline.AddHours(7);
            var body = $"Order {order.OrderCode} at {order.Shop.Name} must be collected before {localDeadline:dd/MM/yyyy HH:mm} (UTC+7). " +
                $"Address: {order.Shop.AddressLine}. Please collect it before this hard deadline to avoid a missed pickup.";
            await notifications.EnqueueAsync(NotificationTarget.User(order.CustomerId), new CreateNotificationCommand(
                NotificationType.PickupDeadlineApproaching, "Pickup deadline approaching", body,
                $"/orders/{orderId}", $"pickup-deadline:{orderId}:{deadline.Ticks}"), ct);
            var key = $"pickup-email:{orderId}:{deadline.Ticks / 10}";
            db.Add(new SaveBite.Backend.Models.Entities.OrderEventOutbox
            {
                Id = Guid.NewGuid(), OrderId = orderId, EventKey = key, CreatedAt = now,
                RoutingKey = SaveBite.Backend.Messaging.Constants.RabbitMqRoutingKeys.EmailRoutingKey,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new SaveBite.Backend.Messaging.Events.EmailNotificationEvent
                {
                    DeliveryKey = key, ExpiresAt = deadline, To = order.Customer.Email,
                    Subject = "SaveBite: your pickup deadline is approaching", Body = body, IsHtml = false
                })
            });
            order.PickupReminderEmailQueuedAt = now;
            await orders.SaveAsync(ct);
            return true;
        }, ct);
    }

    public static bool Eligible(OrderStatus status)
        => status is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup;
}
