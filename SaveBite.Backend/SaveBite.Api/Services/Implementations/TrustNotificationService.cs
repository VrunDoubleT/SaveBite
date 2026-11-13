using SaveBite.Backend.Constants;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Read committed histories, including on a retried order event, and preserve stable deduplication keys.
public sealed class TrustNotificationService(ITrustNotificationRepository repository, INotificationQueue notifications) : ITrustNotificationService
{
    public async Task PublishForOrderAsync(Guid orderId, CancellationToken ct)
    {
        foreach (var recovery in await repository.RecoveryChangesAsync(orderId, ct))
        {
            var penalty = recovery.NoShowPenalty;
            var finished = penalty.Status == NoShowPenaltyStatus.Recovered && penalty.RecoveredAt == recovery.CreatedAt;
            var body = $"Completed order {recovery.Order.OrderCode} was used to repay no-show penalty #{penalty.OccurrenceNumber}. "
                + "This order does not earn trust points, completed-order totals or accumulated purchase value. "
                + (finished ? "Your penalty recovery is complete. Your next eligible completed order can earn progress again. "
                    : "Complete the remaining recovery requirements before earning progress again. ")
                + (penalty.PermanentOfflineLock ? "Your permanent pay-at-shop restriction remains in place." : "");
            await notifications.EnqueueAsync(NotificationTarget.User(penalty.UserId), new CreateNotificationCommand(
                NotificationType.PenaltyRecoveryProgress, "Completed order applied to penalty recovery", body,
                NotificationActionUrls.CustomerTrustScorePenalties(), $"trust-recovery:{recovery.Id}"), ct);
        }
        foreach (var change in (await repository.ScoreChangesAsync(orderId, ct)).Where(h => h.ScoreDelta != 0))
            await notifications.EnqueueAsync(NotificationTarget.User(change.UserId), new CreateNotificationCommand(
                NotificationType.TrustScoreChanged,
                NotificationMessages.TrustAndPenalties.TrustScoreUpdatedTitle,
                NotificationMessages.TrustAndPenalties.TrustScoreUpdatedBody(change.ScoreBefore, change.ScoreAfter, change.Reason),
                NotificationActionUrls.CustomerTrustScore(), $"trust-score:{change.Id}"), ct);
        foreach (var penalty in await repository.PenaltiesAsync(orderId, ct))
            await notifications.EnqueueAsync(NotificationTarget.User(penalty.UserId), new CreateNotificationCommand(
                NotificationType.NoShowPenaltyCreated,
                NotificationMessages.TrustAndPenalties.MissedPickupPenaltyTitle,
                NotificationMessages.TrustAndPenalties.MissedPickupPenaltyBody(penalty.ScoreDeducted),
                NotificationActionUrls.CustomerTrustScore(), $"trust-penalty:{penalty.Id}"), ct);
        foreach (var level in (await repository.LevelChangesAsync(orderId, ct)).Where(h => h.PreviousTrustLevelId != null))
            await notifications.EnqueueAsync(NotificationTarget.User(level.UserId), new CreateNotificationCommand(
                NotificationType.TrustLevelChanged,
                NotificationMessages.TrustAndPenalties.TrustLevelUpdatedTitle,
                NotificationMessages.TrustAndPenalties.TrustLevelUpdatedBody(level.NewTrustLevel.Name),
                NotificationActionUrls.CustomerTrustScore(), $"trust-level:{level.Id}"), ct);
    }
}
