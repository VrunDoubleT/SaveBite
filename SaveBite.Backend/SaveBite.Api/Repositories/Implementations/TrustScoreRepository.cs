using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class TrustScoreRepository : ITrustScoreRepository
{
    private readonly AppDbContext _dbContext;

    public TrustScoreRepository(AppDbContext dbContext) => _dbContext = dbContext;

    // Retrieve the user's current Trust Score together with the level they are on.
    public Task<UserTrustScore?> GetUserTrustScoreAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.UserTrustScores.AsNoTracking()
            .Include(score => score.TrustLevel)
                .ThenInclude(level => level.Requirement)
            .Include(score => score.TrustLevel)
                .ThenInclude(level => level.DepositPolicy)
                    .ThenInclude(policy => policy!.Ranges)
            .SingleOrDefaultAsync(score => score.UserId == userId, cancellationToken);

    public Task<UserLevelProgress?> GetLevelProgressAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.UserLevelProgresses.AsNoTracking()
            .SingleOrDefaultAsync(progress => progress.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<TrustLevel>> GetActiveLevelsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TrustLevels.AsNoTracking()
            .Include(level => level.Requirement)
            .Include(level => level.DepositPolicy)
                .ThenInclude(policy => policy!.Ranges)
            .Where(level => level.IsActive)
            .OrderBy(level => level.Rank)
            .ToListAsync(cancellationToken);

    public Task<TrustLevel?> GetLevelWithRequirementAsync(Guid levelId, CancellationToken cancellationToken = default)
        => _dbContext.TrustLevels.AsNoTracking()
            .Include(level => level.Requirement)
            .Include(level => level.DepositPolicy)
                .ThenInclude(policy => policy!.Ranges)
            .SingleOrDefaultAsync(level => level.Id == levelId, cancellationToken);

    // Score history of the user, newest first.
    public async Task<PagedResult<TrustScoreHistory>> GetHistoryPagedAsync(
        Guid userId, GetTrustScoreHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TrustScoreHistories.AsNoTracking()
            .Where(history => history.UserId == userId);

        if (request.EventType.HasValue)
            query = query.Where(history => history.EventType == request.EventType.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(history => history.Order)
            .OrderByDescending(history => history.CreatedAt)
            .ThenByDescending(history => history.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<TrustScoreHistory>.Create(items, request.Page, request.PageSize, total);
    }

    // Penalty history of the user, newest first.
    public async Task<PagedResult<NoShowPenalty>> GetPenaltiesPagedAsync(
        Guid userId, GetPenaltiesRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.NoShowPenalties.AsNoTracking()
            .Where(penalty => penalty.UserId == userId);

        if (request.Status.HasValue)
            query = query.Where(penalty => penalty.Status == request.Status.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(penalty => penalty.Order)
            .OrderByDescending(penalty => penalty.CreatedAt)
            .ThenByDescending(penalty => penalty.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<NoShowPenalty>.Create(items, request.Page, request.PageSize, total);
    }

    // A single penalty of the user with the orders counted towards its recovery.
    public Task<NoShowPenalty?> GetPenaltyWithRecoveryOrdersAsync(
        Guid userId, Guid penaltyId, CancellationToken cancellationToken = default)
        => _dbContext.NoShowPenalties.AsNoTracking()
            .Include(penalty => penalty.Order)
            .Include(penalty => penalty.RecoveryOrders)
                .ThenInclude(recovery => recovery.Order)
            .AsSplitQuery()
            .SingleOrDefaultAsync(penalty => penalty.Id == penaltyId && penalty.UserId == userId, cancellationToken);

    public Task<int> CountRecoveringPenaltiesAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.NoShowPenalties.AsNoTracking()
            .CountAsync(
                penalty => penalty.UserId == userId && penalty.Status == NoShowPenaltyStatus.Recovering,
                cancellationToken);

    public Task<TrustScoreRule?> GetCurrentEarningRuleAsync(DateTime now, CancellationToken cancellationToken = default)
        => _dbContext.TrustScoreRules.AsNoTracking()
            .Where(rule => rule.ScoreDelta > 0
                           && rule.EffectiveFrom <= now
                           && (rule.EffectiveTo == null || rule.EffectiveTo > now))
            .OrderByDescending(rule => rule.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryProvisionAsync(
        Guid userId, Guid initialLevelId, int initialScore, DateTime now, CancellationToken cancellationToken = default)
    {
        _dbContext.UserTrustScores.Add(new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TrustLevelId = initialLevelId,
            CurrentScore = initialScore,
            IsOfflineLockedPermanently = false,
            CreatedAt = now,
            UpdatedAt = now
        });

        if (!await _dbContext.UserLevelProgresses.AnyAsync(progress => progress.UserId == userId, cancellationToken))
        {
            _dbContext.UserLevelProgresses.Add(new UserLevelProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompletedOrdersCount = 0,
                AccumulatedOrderValue = 0,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        _dbContext.UserTrustLevelHistories.Add(new UserTrustLevelHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PreviousTrustLevelId = null,
            NewTrustLevelId = initialLevelId,
            Reason = TrustLevelChangeReason.InitialAssignment,
            CreatedAt = now
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // A concurrent request may have provisioned this user first (unique index on user_id).
            _dbContext.ChangeTracker.Clear();

            if (await _dbContext.UserTrustScores.AnyAsync(score => score.UserId == userId, cancellationToken))
                return false;

            throw;
        }
    }
}