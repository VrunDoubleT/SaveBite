using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Repositories.Interfaces;

// Persist trust updates atomically under a customer lock shared with checkout.
public interface ITrustOrderRepository
{
    Task ExecuteAsync(Guid orderId, Func<Order, Task> handle, CancellationToken ct);
    Task<bool> IsProcessedAsync(Guid orderId, CancellationToken ct);
    Task<UserTrustScore?> ScoreAsync(Guid userId, CancellationToken ct);
    Task<UserLevelProgress?> ProgressAsync(Guid userId, CancellationToken ct);
    Task<List<TrustLevel>> LevelsAsync(CancellationToken ct);
    Task<TrustScoreRule?> EarningAsync(SaveBite.Backend.Models.Enums.TrustScoreRuleType type, DateTime at, CancellationToken ct);
    Task<int> OccurrencesAsync(Guid userId, int year, int month, CancellationToken ct);
    Task<RecoveryRequirementRule?> PenaltyRuleAsync(int occurrence, DateTime at, CancellationToken ct);
    Task<List<NoShowPenalty>> RecoveringAsync(Guid userId, CancellationToken ct);
    Task<List<Order>> RecoveryCandidatesAsync(Guid userId, CancellationToken ct);
    void Add<T>(T entity) where T : class;
}
