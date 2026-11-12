namespace SaveBite.Backend.BackgroundJobs.Interfaces;

public interface IPenaltyRuleScheduler
{
    Task SyncAsync(Guid id, CancellationToken ct);
    void Cancel(string? startId, string? endId);
}
