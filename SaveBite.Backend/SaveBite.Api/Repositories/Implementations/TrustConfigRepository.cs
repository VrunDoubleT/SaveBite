using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class TrustConfigRepository : ITrustConfigRepository
{
    private readonly AppDbContext _dbContext;

    public TrustConfigRepository(AppDbContext dbContext) => _dbContext = dbContext;

    // ---------------------------------------------------------------- Levels

    public async Task<IReadOnlyList<TrustLevel>> GetLevelsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevels.AsNoTracking()
            .Include(level => level.Requirement)
            .OrderBy(level => level.Rank)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserCountsByLevelAsync(CancellationToken cancellationToken = default)
        => await _dbContext.UserTrustScores.AsNoTracking()
            .GroupBy(score => score.TrustLevelId)
            .Select(group => new { LevelId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.LevelId, item => item.Count, cancellationToken);

    public Task<TrustLevel?> GetLevelForUpdateAsync(Guid levelId, CancellationToken cancellationToken = default)
        => _dbContext.TrustLevels
            .Include(level => level.Requirement)
            .SingleOrDefaultAsync(level => level.Id == levelId, cancellationToken);

    public async Task<IReadOnlyList<TrustLevel>> GetActiveLevelsForValidationAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevels
            .Include(level => level.Requirement)
            .Where(level => level.IsActive)
            .OrderBy(level => level.Rank)
            .ToListAsync(cancellationToken);

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

    // A new version is open-ended, so it overlaps any other enabled rule of the same
    // occurrence tier that has no end date or ends after the new start.
    public Task<bool> HasOverlappingPenaltyRuleAsync(
        int minimumOccurrence, DateTime from, Guid excludeRuleId, CancellationToken cancellationToken = default)
        => _dbContext.RecoveryRequirementRules.AsNoTracking()
            .AnyAsync(rule => rule.Id != excludeRuleId
                              && rule.IsActive
                              && rule.MinimumOccurrence == minimumOccurrence
                              && (rule.EffectiveTo == null || rule.EffectiveTo > from),
                cancellationToken);

    public void AddPenaltyRule(RecoveryRequirementRule rule) => _dbContext.RecoveryRequirementRules.Add(rule);

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
        bool earning, DateTime from, Guid excludeRuleId, CancellationToken cancellationToken = default)
        => _dbContext.TrustScoreRules.AsNoTracking()
            .AnyAsync(rule => rule.Id != excludeRuleId
                              && (rule.ScoreDelta > 0) == earning
                              && (rule.EffectiveTo == null || rule.EffectiveTo > from),
                cancellationToken);

    public void AddScoreRule(TrustScoreRule rule) => _dbContext.TrustScoreRules.Add(rule);

    // ---------------------------------------------------------------- Shared

    public void AddAuditLog(AuditLog log) => _dbContext.Set<AuditLog>().Add(log);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
