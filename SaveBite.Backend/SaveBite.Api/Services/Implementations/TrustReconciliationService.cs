using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class TrustReconciliationService : ITrustReconciliationService
{
    private readonly AppDbContext _db;
    private readonly INotificationQueue _notifications;
    private readonly IRealtimePublisher _realtime;
    private readonly TimeProvider _clock;
    private readonly ILogger<TrustReconciliationService> _logger;

    private const int BatchSize = 200;

    public TrustReconciliationService(
        AppDbContext db,
        INotificationQueue notifications,
        IRealtimePublisher realtime,
        TimeProvider clock,
        ILogger<TrustReconciliationService> logger)
    {
        _db = db;
        _notifications = notifications;
        _realtime = realtime;
        _clock = clock;
        _logger = logger;
    }

    public async Task ReconcileAsync(long policyVersion, CancellationToken cancellationToken = default)
    {
        // 1. FAST ABORT CHECK: If the system already has a newer Policy Version, this message is immediately discarded.
        var currentVersion = await _db.TrustPolicyVersions
            .Where(v => v.Id == 1)
            .Select(v => v.CurrentVersion)
            .SingleOrDefaultAsync(cancellationToken);

        if (currentVersion > policyVersion)
        {
            _logger.LogInformation(
                "Policy version {MsgVersion} is obsolete (Current system version is {CurrentVersion}). Fast aborting.",
                policyVersion, currentVersion);
            return;
        }

        // 2. Retrieve the configuration of current Active Levels (including requirements)
        var activeLevels = await _db.TrustLevels
            .Include(l => l.Requirement)
            .Where(l => l.IsActive)
            .OrderBy(l => l.Rank)
            .ToListAsync(cancellationToken);

        if (activeLevels.Count == 0)
        {
            _logger.LogError("Reconciliation aborted: No active trust levels found.");
            return;
        }

        var defaultLevel = activeLevels[0]; // The lowest level has a requirement of 0.

        _logger.LogInformation("Starting Trust Level reconciliation for Policy Version {Version}", policyVersion);

        // 3. Batch-by-batch processing
        while (!cancellationToken.IsCancellationRequested)
        {
            // Check the version before processing each batch (if an Admin has just initiated a new action, stop immediately to yield to them)
            currentVersion = await _db.TrustPolicyVersions
                .Where(v => v.Id == 1)
                .Select(v => v.CurrentVersion)
                .SingleOrDefaultAsync(cancellationToken);

            if (currentVersion > policyVersion)
            {
                _logger.LogInformation(
                    "Reconciliation for version {Version} halted mid-way because new version {NewVersion} was detected.",
                    policyVersion, currentVersion);
                return;
            }

            // Retrieve users to whom this version has not yet been applied (AppliedPolicyVersion IS NULL or < policyVersion)
            var batchScores = await _db.UserTrustScores
                .Where(s => s.AppliedPolicyVersion == null || s.AppliedPolicyVersion < policyVersion)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (batchScores.Count == 0)
            {
                _logger.LogInformation("Reconciliation completed successfully for Policy Version {Version}.", policyVersion);
                break;
            }

            var userIds = batchScores.Select(s => s.UserId).ToList();
            var progressMap = await _db.UserLevelProgresses
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, cancellationToken);

            var now = _clock.GetUtcNow().UtcDateTime;
            var historiesToAdd = new List<UserTrustLevelHistory>();
            var notificationsToSend = new List<(Guid UserId, string LevelName, Guid HistoryId)>();

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            foreach (var score in batchScores)
            {
                progressMap.TryGetValue(score.UserId, out var progress);
                var completedOrders = progress?.CompletedOrdersCount ?? 0;
                var accumulatedValue = progress?.AccumulatedOrderValue ?? 0m;

                // Calculate the highest valid level for which a user meets all 3 criteria
                var targetLevel = activeLevels
                    .Where(l => l.Requirement != null
                                && score.CurrentScore >= l.Requirement.MinScore
                                && completedOrders >= l.Requirement.MinCompletedOrders
                                && accumulatedValue >= l.Requirement.MinTotalOrderValue)
                    .OrderByDescending(l => l.Rank)
                    .FirstOrDefault() ?? defaultLevel;

                var oldLevelId = score.TrustLevelId;
                var levelChanged = (oldLevelId != targetLevel.Id);

                // Update the user's status
                score.TrustLevelId = targetLevel.Id;
                score.AppliedPolicyVersion = policyVersion;
                score.UpdatedAt = now;

                if (levelChanged)
                {
                    var historyId = Guid.NewGuid();
                    historiesToAdd.Add(new UserTrustLevelHistory
                    {
                        Id = historyId,
                        UserId = score.UserId,
                        PreviousTrustLevelId = oldLevelId,
                        NewTrustLevelId = targetLevel.Id,
                        Reason = TrustLevelChangeReason.AdminAdjustment,
                        OrderId = null,
                        NoShowPenaltyId = null,
                        CreatedAt = now
                    });

                    notificationsToSend.Add((score.UserId, targetLevel.Name, historyId));
                }
            }

            if (historiesToAdd.Count > 0)
            {
                _db.UserTrustLevelHistories.AddRange(historiesToAdd);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            // Send notification and SignalR realtime event to each customer whose level was updated
            foreach (var item in notificationsToSend)
            {
                await _notifications.EnqueueAsync(
                    NotificationTarget.User(item.UserId),
                    new CreateNotificationCommand(
                        NotificationType.TrustLevelChanged,
                        NotificationMessages.TrustAndPenalties.TrustLevelUpdatedTitle,
                        NotificationMessages.TrustAndPenalties.TrustLevelUpdatedFromConfigBody(item.LevelName),
                        NotificationActionUrls.CustomerTrustScore(),
                        $"trust-level:{item.HistoryId}"),
                    cancellationToken);

                await _realtime.SendToUserAsync(
                    item.UserId,
                    RealtimeEvents.TrustLevelChanged,
                    new { levelName = item.LevelName });
            }
        }
    }
}