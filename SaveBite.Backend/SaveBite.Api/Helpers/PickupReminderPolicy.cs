using SaveBite.Backend.Configurations;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Messaging.Events;
namespace SaveBite.Backend.Helpers;

public static class PickupReminderPolicy
{
    // Legacy queued reminders have only this stable key, not business metadata.
    public static bool TryIdentifyEmail(EmailNotificationEvent email, out Guid orderId, out DateTime deadline)
    {
        orderId = default; deadline = default;
        if (email.BusinessKind == "pickup-reminder")
        {
            if (email.BusinessSubjectId is not Guid id || id == Guid.Empty || email.ExpectedDeadline is not DateTime at) return false;
            orderId = id; deadline = at; return true;
        }
        if (email.BusinessKind != null || email.DeliveryKey?.StartsWith("pickup-email:", StringComparison.Ordinal) != true) return false;
        var parts = email.DeliveryKey.Split(':');
        if (parts.Length != 3 || !Guid.TryParse(parts[1], out orderId) || orderId == Guid.Empty
            || !long.TryParse(parts[2], out var micros) || micros < 0 || micros > DateTime.MaxValue.Ticks / 10) return false;
        deadline = new DateTime(micros * 10, DateTimeKind.Utc);
        return true;
    }

    public static bool CanDeliver(Order order, DateTime expectedDeadline, DateTime now, string recipient)
        => order.Status is (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup)
            && order.CompletedAt == null && !order.NoShow
            && order.PickupReminderEmailSentAt == null
            && order.HardDeadlineAt is DateTime deadline && deadline.Ticks / 10 == expectedDeadline.Ticks / 10
            && now < deadline && order.Customer.Status == UserStatus.Active
            && order.Customer.CustomerStatus == CustomerStatus.Active
            && string.Equals(order.Customer.Email, recipient, StringComparison.OrdinalIgnoreCase);

    public static DateTime DueAt(DateTime confirmedAt, DateTime hardDeadline)
    {
        var standard = hardDeadline.AddMinutes(-TimingConfiguration.Orders.PickupReminderLeadMinutes);
        return standard > confirmedAt ? standard : confirmedAt.AddTicks((hardDeadline - confirmedAt).Ticks / 2);
    }
}
