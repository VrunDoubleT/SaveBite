using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class TrustConfigRepository : ITrustConfigRepository
{
    private readonly AppDbContext _dbContext;

    public TrustConfigRepository(AppDbContext dbContext) => _dbContext = dbContext;

    // ---------------------------------------------------------------- Levels

    public async Task<IReadOnlyList<TrustLevel>> GetLevelsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevels.AsNoTracking()
            .Include(level => level.Requirement)
            .Include(level => level.DepositPolicy)
                .ThenInclude(policy => policy!.Ranges)
            .OrderBy(level => level.Rank)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserCountsByLevelAsync(CancellationToken cancellationToken = default)
        => await _dbContext.UserTrustScores.AsNoTracking()
            .GroupBy(score => score.TrustLevelId)
            .Select(group => new { LevelId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.LevelId, item => item.Count, cancellationToken);

    public Task<bool> LevelExistsAsync(Guid levelId, CancellationToken cancellationToken = default)
        => _dbContext.TrustLevels.AsNoTracking().AnyAsync(level => level.Id == levelId, cancellationToken);

    public Task<bool> LevelNameExistsAsync(string name, Guid? excludeLevelId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        var query = _dbContext.TrustLevels.AsNoTracking()
            .Where(level => level.Name.ToLower() == normalized);

        if (excludeLevelId.HasValue)
            query = query.Where(level => level.Id != excludeLevelId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetMaxLevelRankAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevels.AsNoTracking()
            .Select(level => (int?)level.Rank)
            .MaxAsync(cancellationToken) ?? 0;

    public async Task<int> GetNextLevelHistoryVersionAsync(Guid levelId, CancellationToken cancellationToken = default)
    {
        var maxVersion = await _dbContext.TrustLevelHistories.AsNoTracking()
            .Where(history => history.TrustLevelId == levelId)
            .Select(history => (int?)history.VersionNo)
            .MaxAsync(cancellationToken) ?? 0;

        return maxVersion + 1;
    }

    public async Task<IReadOnlyList<TrustLevelHistory>> GetLevelHistoriesAsync(Guid levelId, CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevelHistories.AsNoTracking()
            .Include(history => history.ChangedByUser)
            .Where(history => history.TrustLevelId == levelId)
            .OrderByDescending(history => history.VersionNo)
            .ToListAsync(cancellationToken);

    public Task<TrustLevel?> GetLevelForUpdateAsync(Guid levelId, CancellationToken cancellationToken = default)
        => _dbContext.TrustLevels
            .Include(level => level.Requirement)
            .Include(level => level.DepositPolicy)
                .ThenInclude(policy => policy!.Ranges)
            .SingleOrDefaultAsync(level => level.Id == levelId, cancellationToken);

    public async Task<IReadOnlyList<TrustLevel>> GetActiveLevelsForValidationAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevels
            .Include(level => level.Requirement)
            .Where(level => level.IsActive)
            .OrderBy(level => level.Rank)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TrustLevel>> GetAllLevelsWithRequirementsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevels
            .Include(level => level.Requirement)
            .OrderBy(level => level.Rank)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserTrustScore>> GetUserScoresByLevelForUpdateAsync(
        Guid levelId, CancellationToken cancellationToken = default)
        => await _dbContext.UserTrustScores
            .Where(score => score.TrustLevelId == levelId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, UserLevelProgress>> GetUserProgressMapAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, UserLevelProgress>();

        return await _dbContext.UserLevelProgresses.AsNoTracking()
            .Where(progress => userIds.Contains(progress.UserId))
            .ToDictionaryAsync(progress => progress.UserId, cancellationToken);
    }

    public async Task<long> IncrementPolicyVersionAndQueueOutboxAsync(string triggerReason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        // Version 1 row-level locking to serialize concurrent configuration updates
        var version = await _dbContext.TrustPolicyVersions
            .FromSqlInterpolated($"SELECT * FROM trust_policy_version WHERE id = 1 FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (version == null)
        {
            version = new TrustPolicyVersion { Id = 1, CurrentVersion = 1, UpdatedAt = now };
            _dbContext.TrustPolicyVersions.Add(version);
        }
        else
        {
            version.CurrentVersion++;
            version.UpdatedAt = now;
        }
        // Push the Outbox message into the `event_outbox` table (the Interceptor will automatically trigger the Dispatcher after the commit).
        var outbox = new OrderEventOutbox
        {
            Id = Guid.NewGuid(),
            OrderId = null,
            EventKey = $"trust:reconcile:{version.CurrentVersion}",
            RoutingKey = RabbitMqRoutingKeys.TrustLevelReconcile,
            PayloadJson = JsonSerializer.Serialize(new TrustLevelReconcileEvent(version.CurrentVersion, triggerReason, now)),
            CreatedAt = now
        };
        _dbContext.Set<OrderEventOutbox>().Add(outbox);

        return version.CurrentVersion;
}

    public void AddLevel(TrustLevel level) => _dbContext.TrustLevels.Add(level);

    public void AddRequirement(TrustLevelRequirement requirement) => _dbContext.TrustLevelRequirements.Add(requirement);

    public void AddDepositPolicy(TrustLevelDepositPolicy policy)
        => _dbContext.TrustLevelDepositPolicies.Add(policy);

    public void AddDepositRanges(IEnumerable<TrustLevelDepositRange> ranges)
        => _dbContext.TrustLevelDepositRanges.AddRange(ranges);

    public void RemoveDepositRanges(IEnumerable<TrustLevelDepositRange> ranges)
        => _dbContext.TrustLevelDepositRanges.RemoveRange(ranges);

    public void AddLevelHistory(TrustLevelHistory history)
        => _dbContext.TrustLevelHistories.Add(history);

    public void AddUserTrustLevelHistories(IEnumerable<UserTrustLevelHistory> histories)
        => _dbContext.UserTrustLevelHistories.AddRange(histories);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        bool lockLevelsAndScores = false,
        CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (lockLevelsAndScores)
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT id FROM trust_levels ORDER BY id FOR UPDATE", cancellationToken);
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT id FROM user_trust_scores ORDER BY id FOR UPDATE", cancellationToken);
            }

            _dbContext.ChangeTracker.Clear();
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    // --------------------------------------------------------- Penalty rules

    public async Task<IReadOnlyList<RecoveryRequirementRule>> GetPenaltyRulesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.RecoveryRequirementRules.AsNoTracking()
            .Include(rule => rule.CreatedByUser)
            .OrderBy(rule => rule.MinimumOccurrence)
            .ThenByDescending(rule => rule.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<RecoveryRequirementRule?> GetPenaltyRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default)
        => _dbContext.RecoveryRequirementRules
            .SingleOrDefaultAsync(rule => rule.Id == ruleId, cancellationToken);

    // Two half-open intervals [from1, to1) and [from2, to2) overlap iff:
    // from1 < (to2 ?? ∞) AND from2 < (to1 ?? ∞).
    public Task<RecoveryRequirementRule?> FindOverlappingPenaltyRuleAsync(
        int minimumOccurrence,
        DateTime from,
        DateTime? to,
        Guid? excludeRuleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RecoveryRequirementRules.AsNoTracking()
            .Where(rule => rule.MinimumOccurrence == minimumOccurrence);

        if (excludeRuleId.HasValue)
            query = query.Where(rule => rule.Id != excludeRuleId.Value);

        if (to.HasValue)
        {
            var toValue = to.Value;
            query = query.Where(rule =>
                rule.EffectiveFrom < toValue &&
                (rule.EffectiveTo == null || rule.EffectiveTo > from));
        }
        else
        {
            query = query.Where(rule =>
                rule.EffectiveTo == null || rule.EffectiveTo > from);
        }

        return query
            .OrderBy(rule => rule.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsPenaltyRuleReferencedAsync(Guid ruleId, CancellationToken cancellationToken = default)
        => _dbContext.NoShowPenalties.AsNoTracking()
            .AnyAsync(penalty => penalty.RecoveryRuleId == ruleId, cancellationToken);

    public void AddPenaltyRule(RecoveryRequirementRule rule) => _dbContext.RecoveryRequirementRules.Add(rule);

    public void RemovePenaltyRule(RecoveryRequirementRule rule) => _dbContext.RecoveryRequirementRules.Remove(rule);

    // ----------------------------------------------------------- Score rules

    public async Task<IReadOnlyList<TrustScoreRule>> GetScoreRulesAsync(bool? earningOnly, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TrustScoreRules.AsNoTracking()
            .Include(rule => rule.CreatedByUser)
            .AsQueryable();

        if (earningOnly == true)
            query = query.Where(rule => rule.ScoreDelta > 0);

        return await query
            .OrderByDescending(rule => rule.ScoreDelta > 0)
            .ThenByDescending(rule => rule.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public Task<TrustScoreRule?> GetScoreRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default)
        => _dbContext.TrustScoreRules
            .SingleOrDefaultAsync(rule => rule.Id == ruleId, cancellationToken);

    public Task<bool> HasOverlappingScoreRuleAsync(
        SaveBite.Backend.Models.Enums.TrustScoreRuleType ruleType, DateTime from, Guid excludeRuleId, CancellationToken cancellationToken = default)
        => _dbContext.TrustScoreRules.AsNoTracking()
            .AnyAsync(rule => rule.Id != excludeRuleId
                              && rule.RuleType == ruleType
                              && (rule.EffectiveTo == null || rule.EffectiveTo > from),
                cancellationToken);

    public void AddScoreRule(TrustScoreRule rule) => _dbContext.TrustScoreRules.Add(rule);

    // ---------------------------------------------------------------- Shared



    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}