using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Repositories.Interfaces;

public interface ITrustNotificationRepository
{
    Task<List<TrustProgressHistory>> ScoreChangesAsync(Guid orderId, CancellationToken ct);
    Task<List<NoShowPenalty>> PenaltiesAsync(Guid orderId, CancellationToken ct);
    Task<List<UserTrustLevelHistory>> LevelChangesAsync(Guid orderId, CancellationToken ct);
    Task<List<NoShowPenaltyOrderHistory>> RecoveryChangesAsync(Guid orderId, CancellationToken ct);
}
