using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Services.Implementations;

public static class RefundNotificationCommands
{
    public static CreateNotificationCommand DestinationRequired(Guid refundId, string orderCode, DateTime deadlineUtc)
        => new(NotificationType.RefundDestinationRequired,
            NotificationMessages.Refunds.DestinationRequiredTitle,
            NotificationMessages.Refunds.DestinationRequiredBody(orderCode, deadlineUtc),
            NotificationActionUrls.CustomerRefund(refundId),
            $"refund:{refundId}:destination-required:customer");
}
