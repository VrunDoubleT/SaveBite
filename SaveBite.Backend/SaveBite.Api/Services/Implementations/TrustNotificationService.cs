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
        foreach (var change in (await repository.ScoreChangesAsync(orderId, ct)).Where(h => h.ScoreDelta != 0))
            await notifications.EnqueueAsync(NotificationTarget.User(change.UserId), new CreateNotificationCommand(
                NotificationType.TrustScoreChanged,
                NotificationMessages.TrustAndPenalties.TrustScoreUpdatedTitle,
                NotificationMessages.TrustAndPenalties.TrustScoreUpdatedBody(change.ScoreBefore, change.ScoreAfter, change.Reason),
                "/account/trust-score", $"trust-score:{change.Id}"), ct);
        foreach (var penalty in await repository.PenaltiesAsync(orderId, ct))
            await notifications.EnqueueAsync(NotificationTarget.User(penalty.UserId), new CreateNotificationCommand(
                NotificationType.NoShowPenaltyCreated,
                NotificationMessages.TrustAndPenalties.MissedPickupPenaltyTitle,
                NotificationMessages.TrustAndPenalties.MissedPickupPenaltyBody(penalty.ScoreDeducted),
                "/account/trust-score", $"trust-penalty:{penalty.Id}"), ct);
        foreach (var level in (await repository.LevelChangesAsync(orderId, ct)).Where(h => h.PreviousTrustLevelId != null))
            await notifications.EnqueueAsync(NotificationTarget.User(level.UserId), new CreateNotificationCommand(
                NotificationType.TrustLevelChanged,
                NotificationMessages.TrustAndPenalties.TrustLevelUpdatedTitle,
                NotificationMessages.TrustAndPenalties.TrustLevelUpdatedBody(level.NewTrustLevel.Name),
                "/account/trust-score", $"trust-level:{level.Id}"), ct);
    }
}
