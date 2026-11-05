using SaveBite.Backend.Models.DTOs;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class TrustConfigService : ITrustConfigService
{
    private readonly IAdminAuditWriter _audit;
    private const string KindEarning = "Earning";
    private const string KindPenalty = "Penalty";

    // Small allowance so "effective now" requests are not rejected because of clock drift.
    private static readonly TimeSpan EffectiveFromTolerance = TimeSpan.FromMinutes(1);

    private readonly ITrustConfigRepository _repository;
    private readonly INotificationQueue _notifications;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TrustConfigService> _logger;

    public TrustConfigService(
        ITrustConfigRepository repository, IAdminAuditWriter audit,
        TimeProvider timeProvider,
        ILogger<TrustConfigService> logger, INotificationQueue notifications)
    {
        _audit = audit;
        _repository = repository;
        _notifications = notifications;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Levels (UC-129 .. UC-132, UC-152 .. UC-155)
    // -----------------------------------------------------------------------

    // UC-129 / UC-131 / UC-152
    public async Task<IReadOnlyList<AdminTrustLevelResponse>> GetLevelsAsync(CancellationToken cancellationToken = default)
    {
        var levels = await _repository.GetLevelsAsync(cancellationToken);
        var counts = await _repository.GetUserCountsByLevelAsync(cancellationToken);

        return levels.Select(level => ToLevelResponse(level, counts)).ToList();
    }

    // UC-152: View Level History
    public async Task<IReadOnlyList<TrustLevelHistoryItemResponse>> GetLevelHistoryAsync(
        Guid levelId, CancellationToken cancellationToken = default)
    {
        if (!await _repository.LevelExistsAsync(levelId, cancellationToken))
            throw AppException.NotFound("Trust level not found.");

        var histories = await _repository.GetLevelHistoriesAsync(levelId, cancellationToken);
        return histories.Select(ToLevelHistoryResponse).ToList();
    }

    // UC-153: Add New Level (Rank = max(all levels) + 1, default Active, single transaction)
    public async Task<AdminTrustLevelResponse> CreateLevelAsync(
        Guid adminId, CreateTrustLevelRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeLevelName(request.Name);
        var orderedRanges = ValidateAndNormalizeDepositPolicy(request.Benefits);
        var description = string.IsNullOrWhiteSpace(request.Benefits.Description)
            ? null
            : request.Benefits.Description.Trim();
        var freeAmount = request.Benefits.RequireFullOnlinePayment ? null : request.Benefits.DepositFreeOrderAmount;
        var maxAmount = request.Benefits.RequireFullOnlinePayment ? null : request.Benefits.MaxDepositOrderAmount;

        try
        {
            return await _repository.ExecuteInTransactionAsync(async ct =>
            {
                if (await _repository.LevelNameExistsAsync(name, null, ct))
                    throw AppException.Conflict("A trust level with this name already exists.");

                var allLevels = await _repository.GetAllLevelsWithRequirementsAsync(ct);
                var nextRank = (allLevels.Count == 0 ? 0 : allLevels.Max(l => l.Rank)) + 1;

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var newLevel = new TrustLevel
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description,
                    Rank = nextRank,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var requirement = new TrustLevelRequirement
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = newLevel.Id,
                    MinScore = request.Requirement.MinScore,
                    MinCompletedOrders = request.Requirement.MinCompletedOrders,
                    MinTotalOrderValue = request.Requirement.MinTotalOrderValue
                };
                newLevel.Requirement = requirement;

                // Validate thresholds against active levels (BR-157, BR-158) and preceding level if any.
                var activeLevels = allLevels.Where(l => l.IsActive).Append(newLevel).ToList();
                ValidateThresholds(activeLevels, newLevel.Id, request.Requirement);

                var previousHighest = allLevels.OrderByDescending(l => l.Rank).FirstOrDefault();
                if (previousHighest?.Requirement is not null && !previousHighest.IsActive)
                {
                    var prevReq = previousHighest.Requirement;
                    var nonDecreasing = request.Requirement.MinScore >= prevReq.MinScore
                                        && request.Requirement.MinCompletedOrders >= prevReq.MinCompletedOrders
                                        && request.Requirement.MinTotalOrderValue >= prevReq.MinTotalOrderValue;
                    var increases = request.Requirement.MinScore > prevReq.MinScore
                                    || request.Requirement.MinCompletedOrders > prevReq.MinCompletedOrders
                                    || request.Requirement.MinTotalOrderValue > prevReq.MinTotalOrderValue;

                    if (!nonDecreasing || !increases)
                    {
                        throw AppException.BadRequest(
                            $"Requirements must exceed the preceding level ({previousHighest.Name}, Rank {previousHighest.Rank}): " +
                            "every criterion must be at least as much, and at least one must be strictly greater.");
                    }
                }

                var policy = new TrustLevelDepositPolicy
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = newLevel.Id,
                    RequireFullOnlinePayment = request.Benefits.RequireFullOnlinePayment,
                    DepositFreeOrderAmount = freeAmount,
                    MaxDepositOrderAmount = maxAmount,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var rangeEntities = orderedRanges.Select(r => new TrustLevelDepositRange
                {
                    Id = Guid.NewGuid(),
                    DepositPolicyId = policy.Id,
                    FromOrderAmount = r.FromOrderAmount,
                    ToOrderAmount = r.ToOrderAmount,
                    DepositAmount = r.DepositAmount
                }).ToList();

                foreach (var r in rangeEntities)
                    policy.Ranges.Add(r);

                newLevel.DepositPolicy = policy;

                _repository.AddLevel(newLevel);
                _repository.AddRequirement(requirement);
                _repository.AddDepositPolicy(policy);
                if (rangeEntities.Count > 0)
                    _repository.AddDepositRanges(rangeEntities);

                WriteLevelHistory(newLevel, 1, TrustLevelChangeType.Created, affectedUserCount: 0, adminId, now);
                WriteAudit(adminId, "CreateTrustLevel", "TrustLevel", newLevel.Id, null, SnapshotTrustLevel(newLevel), now);

                await _repository.SaveChangesAsync(ct);
                _logger.LogInformation("New Trust Level created. LevelId: {LevelId}, Name: {Name}, Rank: {Rank}",
                    newLevel.Id, newLevel.Name, newLevel.Rank);

                return ToLevelResponse(newLevel, new Dictionary<Guid, int>());
            }, lockLevelsAndScores: true, cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw AppException.Conflict("A trust level with this name or rank already exists.");
        }
    }

    // UC-154: Update Level Information (Rename only)
    public async Task<AdminTrustLevelResponse> UpdateLevelInfoAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelInfoRequest request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeLevelName(request.Name);
        var level = await GetLevelAsync(levelId, cancellationToken);
        var counts = await _repository.GetUserCountsByLevelAsync(cancellationToken);

        // If nothing changed, return immediately without recording a new version.
        if (string.Equals(level.Name, name, StringComparison.Ordinal))
            return ToLevelResponse(level, counts);

        if (await _repository.LevelNameExistsAsync(name, level.Id, cancellationToken))
            throw AppException.Conflict("Another trust level with this name already exists.");

        var oldValues = SnapshotTrustLevel(level);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var nextVersion = await _repository.GetNextLevelHistoryVersionAsync(level.Id, cancellationToken);

        level.Name = name;
        level.UpdatedAt = now;

        WriteLevelHistory(
            level,
            nextVersion,
            TrustLevelChangeType.InfoUpdated,
            counts.GetValueOrDefault(level.Id),
            adminId,
            now);
        WriteAudit(adminId, "UpdateTrustLevelInfo", "TrustLevel", level.Id, oldValues, SnapshotTrustLevel(level), now);

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw AppException.Conflict("Another trust level with this name already exists.");
        }

        _logger.LogInformation("Trust level renamed. LevelId: {LevelId}, NewName: {Name}", level.Id, level.Name);
        return ToLevelResponse(level, counts);
    }

    // UC-155: Update Level Status (Deactivate / Reactivate)
    public async Task<AdminTrustLevelResponse> UpdateLevelStatusAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelStatusRequest request, CancellationToken cancellationToken = default)
    {
        var notices = new List<(Guid UserId, Guid HistoryId, string Name)>();
        try
        {
            var result = await _repository.ExecuteInTransactionAsync(async ct =>
            {
                notices.Clear();
                var level = await GetLevelAsync(levelId, ct);
                var counts = await _repository.GetUserCountsByLevelAsync(ct);

                if (level.IsActive == request.IsActive)
                    return ToLevelResponse(level, counts);

                var activeLevels = await _repository.GetActiveLevelsForValidationAsync(ct);
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var nextVersion = await _repository.GetNextLevelHistoryVersionAsync(level.Id, ct);

                if (!request.IsActive)
                {
                    // Deactivating a level
                    var remainingActive = activeLevels
                        .Where(l => l.Id != level.Id)
                        .OrderBy(l => l.Rank)
                        .ToList();

                    if (remainingActive.Count == 0)
                        throw AppException.BadRequest("Cannot deactivate the only active trust level.");

                    // BR-158: Do not allow deactivating the lowest-ranked active level.
                    var currentLowestActive = activeLevels.OrderBy(l => l.Rank).First();
                    if (currentLowestActive.Id == level.Id)
                    {
                        throw AppException.BadRequest(
                            $"Cannot deactivate the lowest active level ({level.Name}). " +
                            "Every customer must qualify for at least one active level with zero minimum requirements.");
                    }

                    // Ensure remaining active levels still satisfy BR-157 and BR-158.
                    var lowestRemaining = remainingActive[0];
                    ValidateThresholds(
                        remainingActive,
                        lowestRemaining.Id,
                        new UpdateTrustLevelRequirementRequest
                        {
                            MinScore = lowestRemaining.Requirement?.MinScore ?? 0,
                            MinCompletedOrders = lowestRemaining.Requirement?.MinCompletedOrders ?? 0,
                            MinTotalOrderValue = lowestRemaining.Requirement?.MinTotalOrderValue ?? 0m
                        });

                    var oldValues = SnapshotTrustLevel(level);
                    level.IsActive = false;
                    level.UpdatedAt = now;

                    // Recalculate every customer currently on this level to the highest active level they meet (BR-051, BR-207).
                    var affectedScores = await _repository.GetUserScoresByLevelForUpdateAsync(level.Id, ct);
                    var userIds = affectedScores.Select(s => s.UserId).ToList();
                    var progressMap = await _repository.GetUserProgressMapAsync(userIds, ct);

                    var levelHistories = new List<UserTrustLevelHistory>(affectedScores.Count);
                    foreach (var userScore in affectedScores)
                    {
                        progressMap.TryGetValue(userScore.UserId, out var progress);
                        var completedOrders = progress?.CompletedOrdersCount ?? 0;
                        var accumulatedValue = progress?.AccumulatedOrderValue ?? 0m;

                        var targetLevel = remainingActive
                            .Where(candidate => candidate.Requirement != null
                                                && userScore.CurrentScore >= candidate.Requirement.MinScore
                                                && completedOrders >= candidate.Requirement.MinCompletedOrders
                                                && accumulatedValue >= candidate.Requirement.MinTotalOrderValue)
                            .OrderByDescending(candidate => candidate.Rank)
                            .FirstOrDefault() ?? remainingActive[0];

                        userScore.TrustLevelId = targetLevel.Id;
                        userScore.UpdatedAt = now;

                        var historyId = Guid.NewGuid();
                        notices.Add((userScore.UserId, historyId, targetLevel.Name));
                        levelHistories.Add(new UserTrustLevelHistory
                        {
                            Id = historyId,
                            UserId = userScore.UserId,
                            PreviousTrustLevelId = level.Id,
                            NewTrustLevelId = targetLevel.Id,
                            Reason = TrustLevelChangeReason.AdminAdjustment,
                            OrderId = null,
                            NoShowPenaltyId = null,
                            CreatedAt = now
                        });
                    }

                    if (levelHistories.Count > 0)
                        _repository.AddUserTrustLevelHistories(levelHistories);

                    WriteLevelHistory(
                        level,
                        nextVersion,
                        TrustLevelChangeType.Deactivated,
                        affectedScores.Count,
                        adminId,
                        now);
                    WriteAudit(adminId, "DeactivateTrustLevel", "TrustLevel", level.Id, oldValues,
                        new { Level = SnapshotTrustLevel(level), AffectedUserCount = affectedScores.Count }, now);

                    await _repository.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "Trust level deactivated. LevelId: {LevelId}, ReassignedUsers: {ReassignedCount}",
                        level.Id, affectedScores.Count);
                }
                else
                {
                    // Reactivating a level: verify requirement & deposit policy and validate BR-157 / BR-158.
                    if (level.Requirement is null)
                        throw AppException.BadRequest("Configure level requirements before activating this level.");

                    if (level.DepositPolicy is null)
                        throw AppException.BadRequest("Configure level deposit policy before activating this level.");

                    var candidateActiveLevels = activeLevels
                        .Append(level)
                        .OrderBy(l => l.Rank)
                        .ToList();

                    ValidateThresholds(
                        candidateActiveLevels,
                        level.Id,
                        new UpdateTrustLevelRequirementRequest
                        {
                            MinScore = level.Requirement.MinScore,
                            MinCompletedOrders = level.Requirement.MinCompletedOrders,
                            MinTotalOrderValue = level.Requirement.MinTotalOrderValue
                        });

                    var oldValues = SnapshotTrustLevel(level);
                    level.IsActive = true;
                    level.UpdatedAt = now;

                    WriteLevelHistory(
                        level,
                        nextVersion,
                        TrustLevelChangeType.Reactivated,
                        affectedUserCount: 0,
                        adminId,
                        now);
                    WriteAudit(adminId, "ReactivateTrustLevel", "TrustLevel", level.Id, oldValues, SnapshotTrustLevel(level), now);

                    await _repository.SaveChangesAsync(ct);

                    _logger.LogInformation("Trust level reactivated. LevelId: {LevelId}", level.Id);
                }

                foreach (var notice in notices)
                    await _notifications.EnqueueAsync(NotificationTarget.User(notice.UserId), new CreateNotificationCommand(
                        NotificationType.TrustLevelChanged, "Trust level updated", $"Your trust level is now {notice.Name} after an administrator updated the level configuration.",
                        "/account/trust-score", $"trust-level:{notice.HistoryId}"), ct);
                var updatedCounts = await _repository.GetUserCountsByLevelAsync(ct);
                return ToLevelResponse(level, updatedCounts);
            }, lockLevelsAndScores: true, cancellationToken);
            return result;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw AppException.Conflict("A concurrent update occurred on this trust level. Please retry.");
        }
    }

    // UC-130: update benefit description and PayAtShop deposit policy & ranges of a level (BR-155, BR-156, BR-197).
    public async Task<AdminTrustLevelResponse> UpdateLevelBenefitsAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelBenefitsRequest request, CancellationToken cancellationToken = default)
    {
        var level = await GetLevelAsync(levelId, cancellationToken);
        var orderedRanges = ValidateAndNormalizeDepositPolicy(request);

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var freeAmount = request.RequireFullOnlinePayment ? null : request.DepositFreeOrderAmount;
        var maxAmount = request.RequireFullOnlinePayment ? null : request.MaxDepositOrderAmount;
        var counts = await _repository.GetUserCountsByLevelAsync(cancellationToken);

        if (!IsBenefitsUnchanged(level, description, request.RequireFullOnlinePayment, freeAmount, maxAmount, orderedRanges))
        {
            var oldValues = SnapshotTrustLevel(level);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var nextVersion = await _repository.GetNextLevelHistoryVersionAsync(level.Id, cancellationToken);

            level.Description = description;
            level.UpdatedAt = now;

            var policy = level.DepositPolicy;
            if (policy is null)
            {
                policy = new TrustLevelDepositPolicy
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                level.DepositPolicy = policy;
                _repository.AddDepositPolicy(policy);
            }
            else
            {
                policy.UpdatedAt = now;
            }

            policy.RequireFullOnlinePayment = request.RequireFullOnlinePayment;
            policy.DepositFreeOrderAmount = freeAmount;
            policy.MaxDepositOrderAmount = maxAmount;

            SyncDepositRanges(policy, orderedRanges);

            WriteLevelHistory(
                level,
                nextVersion,
                TrustLevelChangeType.BenefitsUpdated,
                counts.GetValueOrDefault(level.Id),
                adminId,
                now);
            WriteAudit(adminId, "UpdateTrustLevelBenefits", "TrustLevel", level.Id, oldValues, SnapshotTrustLevel(level), now);

            try
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw AppException.Conflict("A concurrent update occurred on this trust level. Please retry.");
            }

            _logger.LogInformation("Trust level benefits and deposit policy updated. LevelId: {LevelId}", level.Id);
        }

        return ToLevelResponse(level, counts);
    }

    // Validate BR-155, BR-156, BR-197 and DB check constraints on deposit policy and ranges.
    private static IReadOnlyList<TrustLevelDepositRangeRequest> ValidateAndNormalizeDepositPolicy(
        UpdateTrustLevelBenefitsRequest request)
    {
        if (request.RequireFullOnlinePayment)
        {
            if (request.DepositFreeOrderAmount.HasValue
                || request.MaxDepositOrderAmount.HasValue
                || (request.Ranges is { Count: > 0 }))
            {
                throw AppException.BadRequest(
                    "Deposit thresholds and ranges must not be provided when full online payment is required.");
            }

            return Array.Empty<TrustLevelDepositRangeRequest>();
        }

        if (!request.DepositFreeOrderAmount.HasValue || !request.MaxDepositOrderAmount.HasValue)
        {
            throw AppException.BadRequest(
                "Deposit-free order amount and maximum deposit order amount are required when Pay at shop is enabled.");
        }

        var freeAmount = request.DepositFreeOrderAmount.Value;
        var maxAmount = request.MaxDepositOrderAmount.Value;

        if (freeAmount <= 0 || decimal.Truncate(freeAmount) != freeAmount)
            throw AppException.BadRequest("Deposit-free order amount must be a positive whole VND amount.");

        if (maxAmount <= freeAmount || decimal.Truncate(maxAmount) != maxAmount)
            throw AppException.BadRequest("Maximum deposit order amount must be a whole VND amount greater than the deposit-free order amount.");

        if (request.Ranges is null || request.Ranges.Count == 0)
            throw AppException.BadRequest("At least one deposit range is required when Pay at shop is enabled.");

        // BR-155 & BR-156: each range interval key (FromOrderAmount) within a level must be unique and non-contradictory.
        if (request.Ranges.Select(r => r.FromOrderAmount).Distinct().Count() != request.Ranges.Count)
            throw AppException.BadRequest("Each deposit range must start at a unique order amount.");

        var ordered = request.Ranges.OrderBy(r => r.FromOrderAmount).ToList();

        if (ordered[0].FromOrderAmount != freeAmount)
        {
            throw AppException.BadRequest(
                $"The first deposit range must start at the deposit-free order amount ({freeAmount:N0} VND).");
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            var current = ordered[i];

            if (current.FromOrderAmount <= 0 || decimal.Truncate(current.FromOrderAmount) != current.FromOrderAmount)
                throw AppException.BadRequest("Range start amounts must be positive whole VND amounts.");

            if (current.ToOrderAmount <= current.FromOrderAmount || decimal.Truncate(current.ToOrderAmount) != current.ToOrderAmount)
                throw AppException.BadRequest("Each deposit range must have an upper amount greater than its lower amount.");

            if (current.DepositAmount <= 0
                || current.DepositAmount > current.FromOrderAmount
                || decimal.Truncate(current.DepositAmount) != current.DepositAmount)
            {
                throw AppException.BadRequest(
                    "Each fixed deposit amount must be a positive whole VND amount and must not exceed its range's starting order amount.");
            }

            if (i > 0 && current.FromOrderAmount != ordered[i - 1].ToOrderAmount)
            {
                throw AppException.BadRequest(
                    "Deposit ranges must be contiguous with no gaps or overlaps between order-value intervals.");
            }
        }

        if (ordered[^1].ToOrderAmount != maxAmount)
        {
            throw AppException.BadRequest(
                $"The last deposit range must end at the maximum deposit order amount ({maxAmount:N0} VND).");
        }

        return ordered;
    }

    private void SyncDepositRanges(
        TrustLevelDepositPolicy policy,
        IReadOnlyList<TrustLevelDepositRangeRequest> targetRanges)
    {
        if (targetRanges.Count == 0)
        {
            if (policy.Ranges.Count > 0)
            {
                var allExisting = policy.Ranges.ToList();
                _repository.RemoveDepositRanges(allExisting);
                policy.Ranges.Clear();
            }
            return;
        }

        var existingByFrom = policy.Ranges.ToDictionary(r => r.FromOrderAmount);
        var targetFromSet = targetRanges.Select(r => r.FromOrderAmount).ToHashSet();

        var toRemove = policy.Ranges.Where(r => !targetFromSet.Contains(r.FromOrderAmount)).ToList();
        if (toRemove.Count > 0)
        {
            _repository.RemoveDepositRanges(toRemove);
            foreach (var removed in toRemove)
                policy.Ranges.Remove(removed);
        }

        var toAdd = new List<TrustLevelDepositRange>();
        foreach (var target in targetRanges)
        {
            if (existingByFrom.TryGetValue(target.FromOrderAmount, out var existing))
            {
                existing.ToOrderAmount = target.ToOrderAmount;
                existing.DepositAmount = target.DepositAmount;
            }
            else
            {
                var created = new TrustLevelDepositRange
                {
                    Id = Guid.NewGuid(),
                    DepositPolicyId = policy.Id,
                    FromOrderAmount = target.FromOrderAmount,
                    ToOrderAmount = target.ToOrderAmount,
                    DepositAmount = target.DepositAmount
                };
                policy.Ranges.Add(created);
                toAdd.Add(created);
            }
        }

        if (toAdd.Count > 0)
            _repository.AddDepositRanges(toAdd);
    }

    private static bool IsBenefitsUnchanged(
        TrustLevel level,
        string? description,
        bool requireFullOnline,
        decimal? freeAmount,
        decimal? maxAmount,
        IReadOnlyList<TrustLevelDepositRangeRequest> targetRanges)
    {
        if (level.Description != description || level.DepositPolicy is null)
            return false;

        var policy = level.DepositPolicy;
        if (policy.RequireFullOnlinePayment != requireFullOnline
            || policy.DepositFreeOrderAmount != freeAmount
            || policy.MaxDepositOrderAmount != maxAmount
            || policy.Ranges.Count != targetRanges.Count)
        {
            return false;
        }

        var existingOrdered = policy.Ranges.OrderBy(r => r.FromOrderAmount).ToList();
        for (var i = 0; i < targetRanges.Count; i++)
        {
            if (existingOrdered[i].FromOrderAmount != targetRanges[i].FromOrderAmount
                || existingOrdered[i].ToOrderAmount != targetRanges[i].ToOrderAmount
                || existingOrdered[i].DepositAmount != targetRanges[i].DepositAmount)
            {
                return false;
            }
        }

        return true;
    }

    private static object? SnapshotDepositPolicy(TrustLevelDepositPolicy? policy) => policy is null
        ? null
        : new
        {
            policy.RequireFullOnlinePayment,
            policy.DepositFreeOrderAmount,
            policy.MaxDepositOrderAmount,
            Ranges = policy.Ranges
                .OrderBy(r => r.FromOrderAmount)
                .Select(r => new
                {
                    r.FromOrderAmount,
                    r.ToOrderAmount,
                    r.DepositAmount
                })
                .ToList()
        };

    private static object SnapshotTrustLevel(TrustLevel level) => new
    {
        level.Name,
        level.Description,
        level.Rank,
        level.IsActive,
        Requirement = level.Requirement is null
            ? null
            : new
            {
                level.Requirement.MinScore,
                level.Requirement.MinCompletedOrders,
                level.Requirement.MinTotalOrderValue
            },
        DepositPolicy = SnapshotDepositPolicy(level.DepositPolicy)
    };

    // UC-132: the thresholds a customer must reach to be on a level.
    public async Task<AdminTrustLevelResponse> UpdateLevelRequirementAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var level = await GetLevelAsync(levelId, cancellationToken);
        var counts = await _repository.GetUserCountsByLevelAsync(cancellationToken);

        if (level.IsActive)
        {
            var activeLevels = await _repository.GetActiveLevelsForValidationAsync(cancellationToken);
            ValidateThresholds(activeLevels, level.Id, request);
        }

        var unchanged = level.Requirement is not null
                        && level.Requirement.MinScore == request.MinScore
                        && level.Requirement.MinCompletedOrders == request.MinCompletedOrders
                        && level.Requirement.MinTotalOrderValue == request.MinTotalOrderValue;

        if (!unchanged)
        {
            var oldValues = SnapshotTrustLevel(level);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var nextVersion = await _repository.GetNextLevelHistoryVersionAsync(level.Id, cancellationToken);

            if (level.Requirement is null)
            {
                level.Requirement = new TrustLevelRequirement
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level.Id
                };
                _repository.AddRequirement(level.Requirement);
            }

            level.Requirement.MinScore = request.MinScore;
            level.Requirement.MinCompletedOrders = request.MinCompletedOrders;
            level.Requirement.MinTotalOrderValue = request.MinTotalOrderValue;
            level.UpdatedAt = now;

            WriteLevelHistory(
                level,
                nextVersion,
                TrustLevelChangeType.RequirementUpdated,
                counts.GetValueOrDefault(level.Id),
                adminId,
                now);
            WriteAudit(adminId, "UpdateTrustLevelRequirement", "TrustLevel", level.Id, oldValues, SnapshotTrustLevel(level), now);

            try
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw AppException.Conflict("A concurrent update occurred on this trust level. Please retry.");
            }

            _logger.LogInformation("Trust level requirement updated. LevelId: {LevelId}", level.Id);
        }

        return ToLevelResponse(level, counts);
    }

    private sealed record Threshold(string Name, int MinScore, int MinCompletedOrders, decimal MinTotalOrderValue);

    // BR-157: thresholds increase consistently with the level rank.
    // BR-158: the lowest-ranked active level has no minimum requirement.
    private static void ValidateThresholds(
        IReadOnlyList<TrustLevel> activeLevels, Guid updatedLevelId, UpdateTrustLevelRequirementRequest request)
    {
        var ordered = activeLevels
            .OrderBy(level => level.Rank)
            .Select(level => level.Id == updatedLevelId
                ? new Threshold(level.Name, request.MinScore, request.MinCompletedOrders, request.MinTotalOrderValue)
                : new Threshold(
                    level.Name,
                    level.Requirement?.MinScore ?? 0,
                    level.Requirement?.MinCompletedOrders ?? 0,
                    level.Requirement?.MinTotalOrderValue ?? 0m))
            .ToList();

        if (ordered.Count == 0)
            return;

        var lowest = ordered[0];
        if (lowest.MinScore != 0 || lowest.MinCompletedOrders != 0 || lowest.MinTotalOrderValue != 0m)
        {
            throw AppException.BadRequest(
                $"The lowest active level ({lowest.Name}) must not have any minimum requirement.");
        }

        for (var i = 1; i < ordered.Count; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];

            var nonDecreasing = current.MinScore >= previous.MinScore
                                && current.MinCompletedOrders >= previous.MinCompletedOrders
                                && current.MinTotalOrderValue >= previous.MinTotalOrderValue;

            var increases = current.MinScore > previous.MinScore
                            || current.MinCompletedOrders > previous.MinCompletedOrders
                            || current.MinTotalOrderValue > previous.MinTotalOrderValue;

            if (!nonDecreasing || !increases)
            {
                throw AppException.BadRequest(
                    $"Requirements must increase with the level rank: {current.Name} must require at least as much as " +
                    $"{previous.Name} in every criterion, and more in at least one.");
            }
        }
    }

    // -----------------------------------------------------------------------
    // Penalty rules (UC-133, UC-134, UC-156)
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<PenaltyRuleResponse>> GetPenaltyRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetPenaltyRulesAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return rules.Select(rule => ToPenaltyRuleResponse(rule, now)).ToList();
    }

    // UC-156: Add New Penalty Rule (allows same MinimumOccurrence if [EffectiveFrom, EffectiveTo) does not overlap).
    public async Task<PenaltyRuleResponse> CreatePenaltyRuleAsync(
        Guid adminId, CreatePenaltyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var effectiveFrom = ResolveEffectiveFrom(request.EffectiveFrom, now);
        var effectiveTo = ResolveEffectiveTo(request.EffectiveTo, effectiveFrom);

        var conflict = await _repository.FindOverlappingPenaltyRuleAsync(
            request.MinimumOccurrence, effectiveFrom, effectiveTo, excludeRuleId: null, cancellationToken);

        if (conflict is not null)
        {
            throw AppException.Conflict(BuildPenaltyOverlapMessage(conflict));
        }

        var rule = new RecoveryRequirementRule
        {
            Id = Guid.NewGuid(),
            MinimumOccurrence = request.MinimumOccurrence,
            ScoreDeduction = request.ScoreDeduction,
            RequiredOnlineOrders = request.RequiredOnlineOrders,
            RequiredOnlineOrderValue = request.RequiredOnlineOrderValue,
            PermanentOfflineLock = request.PermanentOfflineLock,
            IsActive = request.IsActive,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedByUserId = adminId,
            CreatedAt = now
        };

        _repository.AddPenaltyRule(rule);
        WriteAudit(adminId, "CreatePenaltyRule", "RecoveryRequirementRule", rule.Id, null, SnapshotPenaltyRule(rule), now);

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw AppException.Conflict($"Another penalty rule for occurrence #{request.MinimumOccurrence} was created concurrently.");
        }

        _logger.LogInformation("New penalty rule created. RuleId: {RuleId}, MinimumOccurrence: {Occurrence}",
            rule.Id, rule.MinimumOccurrence);

        return ToPenaltyRuleResponse(rule, now);
    }

    // UC-134: Edit any penalty rule and validate non-overlapping [EffectiveFrom, EffectiveTo) intervals.
    public async Task<PenaltyRuleResponse> UpdatePenaltyRuleAsync(
        Guid adminId, Guid ruleId, UpdatePenaltyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetPenaltyRuleForUpdateAsync(ruleId, cancellationToken)
                   ?? throw AppException.NotFound("Penalty rule not found.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var effectiveFrom = ResolveUpdatedEffectiveFrom(rule.EffectiveFrom, request.EffectiveFrom, now);
        var effectiveTo = ResolveEffectiveTo(request.EffectiveTo, effectiveFrom);

        var conflict = await _repository.FindOverlappingPenaltyRuleAsync(
            request.MinimumOccurrence, effectiveFrom, effectiveTo, excludeRuleId: rule.Id, cancellationToken);

        if (conflict is not null)
        {
            throw AppException.Conflict(BuildPenaltyOverlapMessage(conflict));
        }

        var old = SnapshotPenaltyRule(rule);

        rule.MinimumOccurrence = request.MinimumOccurrence;
        rule.ScoreDeduction = request.ScoreDeduction;
        rule.RequiredOnlineOrders = request.RequiredOnlineOrders;
        rule.RequiredOnlineOrderValue = request.RequiredOnlineOrderValue;
        rule.PermanentOfflineLock = request.PermanentOfflineLock;
        rule.IsActive = request.IsActive;
        rule.EffectiveFrom = effectiveFrom;
        rule.EffectiveTo = effectiveTo;

        WriteAudit(adminId, "UpdatePenaltyRule", "RecoveryRequirementRule", rule.Id, old, SnapshotPenaltyRule(rule), now);

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw AppException.Conflict("A concurrent update occurred on this penalty rule. Please retry.");
        }

        _logger.LogInformation("Penalty rule updated. RuleId: {RuleId}", rule.Id);

        return ToPenaltyRuleResponse(rule, now);
    }

    // Delete a future scheduled penalty rule that has not taken effect yet.
    public async Task DeletePenaltyRuleAsync(
        Guid adminId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetPenaltyRuleForUpdateAsync(ruleId, cancellationToken)
                   ?? throw AppException.NotFound("Penalty rule not found.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (rule.EffectiveFrom <= now)
        {
            throw AppException.BadRequest(
                "Only future scheduled penalty rules that have not taken effect yet can be deleted.");
        }

        if (await _repository.IsPenaltyRuleReferencedAsync(rule.Id, cancellationToken))
        {
            throw AppException.Conflict(
                "Cannot delete a penalty rule that has already been applied to customer penalties.");
        }

        var old = SnapshotPenaltyRule(rule);
        _repository.RemovePenaltyRule(rule);
        WriteAudit(adminId, "DeletePenaltyRule", "RecoveryRequirementRule", rule.Id, old, new { Deleted = true }, now);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Future penalty rule deleted. RuleId: {RuleId}", rule.Id);
    }

    private static DateTime ResolveUpdatedEffectiveFrom(DateTime currentEffectiveFrom, DateTime? requested, DateTime now)
    {
        if (!requested.HasValue)
            return currentEffectiveFrom;

        var value = NormalizeUtc(requested.Value);

        // HTML datetime-local inputs truncate seconds/milliseconds; preserve exact DB timestamp if unchanged within 1 minute.
        if (Math.Abs((value - currentEffectiveFrom).TotalMinutes) < 1)
            return currentEffectiveFrom;

        if (value < now - EffectiveFromTolerance)
        {
            throw AppException.BadRequest(
                currentEffectiveFrom > now
                    ? "The effective start time cannot be in the past."
                    : "Cannot move the start time of an existing rule to another time in the past.");
        }

        return value < now ? now : value;
    }

    private static DateTime? ResolveEffectiveTo(DateTime? requestedTo, DateTime effectiveFrom)
    {
        if (!requestedTo.HasValue)
            return null;

        var value = NormalizeUtc(requestedTo.Value);
        if (value <= effectiveFrom)
            throw AppException.BadRequest("The 'Until' date/time must be strictly later than the 'From' date/time.");

        return value;
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

    private static string BuildPenaltyOverlapMessage(RecoveryRequirementRule conflict)
    {
        var fromText = conflict.EffectiveFrom.ToString("dd/MM/yyyy HH:mm 'UTC'");
        var toText = conflict.EffectiveTo.HasValue
            ? conflict.EffectiveTo.Value.ToString("dd/MM/yyyy HH:mm 'UTC'")
            : "Indefinite (no end date)";

        return $"The selected period overlaps with an existing rule for no-show #{conflict.MinimumOccurrence} " +
               $"({fromText} – {toText}). Please set an 'Until' date on the existing rule or choose a non-overlapping period.";
    }

    private static object SnapshotPenaltyRule(RecoveryRequirementRule rule) => new
    {
        rule.MinimumOccurrence,
        rule.ScoreDeduction,
        rule.RequiredOnlineOrders,
        rule.RequiredOnlineOrderValue,
        rule.PermanentOfflineLock,
        rule.IsActive,
        rule.EffectiveFrom,
        rule.EffectiveTo
    };

    // -----------------------------------------------------------------------
    // Trust Score rules (UC-135 .. UC-138)
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<TrustScoreRuleResponse>> GetScoreRulesAsync(CancellationToken cancellationToken = default)
        => await GetRulesAsync(earningOnly: false, cancellationToken);

    public async Task<IReadOnlyList<TrustScoreRuleResponse>> GetEarningRulesAsync(CancellationToken cancellationToken = default)
        => await GetRulesAsync(earningOnly: true, cancellationToken);

    public Task<TrustScoreRuleResponse> UpdateScoreRuleAsync(
        Guid adminId, Guid ruleId, UpdateTrustScoreRuleRequest request, CancellationToken cancellationToken = default)
        => UpdateRuleAsync(adminId, ruleId, request, earningOnly: false, "UpdateTrustScoreRule", cancellationToken);

    public Task<TrustScoreRuleResponse> UpdateEarningRuleAsync(
        Guid adminId, Guid ruleId, UpdateTrustScoreRuleRequest request, CancellationToken cancellationToken = default)
        => UpdateRuleAsync(adminId, ruleId, request, earningOnly: true, "UpdateTrustEarningRule", cancellationToken);

    private async Task<IReadOnlyList<TrustScoreRuleResponse>> GetRulesAsync(bool earningOnly, CancellationToken cancellationToken)
    {
        var rules = await _repository.GetScoreRulesAsync(earningOnly, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return rules.Select(rule => ToScoreRuleResponse(rule, now)).ToList();
    }

    private async Task<TrustScoreRuleResponse> UpdateRuleAsync(
        Guid adminId, Guid ruleId, UpdateTrustScoreRuleRequest request, bool earningOnly, string action,
        CancellationToken cancellationToken)
    {
        var rule = await _repository.GetScoreRuleForUpdateAsync(ruleId, cancellationToken);
        if (rule is null || (earningOnly && rule.ScoreDelta <= 0))
            throw AppException.NotFound(earningOnly ? "Earning rule not found." : "Trust Score rule not found.");

        if (rule.EffectiveTo.HasValue)
            throw AppException.Conflict("This rule version has been replaced. Update the latest version of the rule instead.");

        if (Math.Sign(request.ScoreDelta) != Math.Sign(rule.ScoreDelta))
            throw AppException.BadRequest("An earning rule must stay positive and a penalty rule must stay negative.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var isPending = rule.EffectiveFrom > now;
        var effectiveFrom = isPending ? rule.EffectiveFrom : ResolveEffectiveFrom(request.EffectiveFrom, now);

        if (request.ScoreDelta == rule.ScoreDelta)
            throw AppException.BadRequest("The score change is the same as the current rule.");

        // Allow one rule per earning type at any time; online and PayAtShop schedules are independent.
        if (await _repository.HasOverlappingScoreRuleAsync(rule.RuleType, effectiveFrom, rule.Id, cancellationToken))
            throw AppException.Conflict("Another Trust Score rule of the same type is already in effect for this period.");

        var oldDelta = rule.ScoreDelta;
        TrustScoreRule target;

        if (isPending)
        {
            target = rule;
            target.ScoreDelta = request.ScoreDelta;
        }
        else
        {
            if (effectiveFrom <= rule.EffectiveFrom)
                throw AppException.BadRequest("The new version must start after the current version.");

            rule.EffectiveTo = effectiveFrom;

            target = new TrustScoreRule
            {
                Id = Guid.NewGuid(),
                RuleType = rule.RuleType,
                ScoreDelta = request.ScoreDelta,
                EffectiveFrom = effectiveFrom,
                CreatedBy = adminId,
                CreatedAt = now
            };
            _repository.AddScoreRule(target);
        }

        WriteAudit(adminId, action, "TrustScoreRule", target.Id,
            new { ScoreDelta = oldDelta, rule.EffectiveFrom },
            new { target.ScoreDelta, target.EffectiveFrom }, now);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Trust Score rule updated. RuleId: {RuleId}, PreviousRuleId: {PreviousRuleId}", target.Id, rule.Id);

        return ToScoreRuleResponse(target, now);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<TrustLevel> GetLevelAsync(Guid levelId, CancellationToken cancellationToken)
        => await _repository.GetLevelForUpdateAsync(levelId, cancellationToken)
           ?? throw AppException.NotFound("Trust level not found.");

    private static string NormalizeLevelName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length is < 1 or > 100)
            throw AppException.BadRequest("Level name must be between 1 and 100 characters.");
        return trimmed;
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static DateTime ResolveEffectiveFrom(DateTime? requested, DateTime now)
    {
        if (!requested.HasValue)
            return now;

        var value = NormalizeUtc(requested.Value);

        if (value < now - EffectiveFromTolerance)
            throw AppException.BadRequest("The effective time cannot be in the past.");

        return value < now ? now : value;
    }

    private static bool IsCurrent(DateTime effectiveFrom, DateTime? effectiveTo, DateTime now)
        => effectiveFrom <= now && (effectiveTo is null || effectiveTo > now);

    internal static TrustLevelDepositPolicyResponse? ToDepositPolicyResponse(TrustLevelDepositPolicy? policy)
        => policy is null
            ? null
            : new TrustLevelDepositPolicyResponse(
                policy.Id,
                policy.RequireFullOnlinePayment,
                policy.DepositFreeOrderAmount,
                policy.MaxDepositOrderAmount,
                policy.Ranges
                    .OrderBy(range => range.FromOrderAmount)
                    .Select(range => new TrustLevelDepositRangeResponse(
                        range.Id,
                        range.FromOrderAmount,
                        range.ToOrderAmount,
                        range.DepositAmount))
                    .ToList(),
                policy.UpdatedAt);

    private static AdminTrustLevelResponse ToLevelResponse(TrustLevel level, IReadOnlyDictionary<Guid, int> userCounts)
        => new(
            level.Id,
            level.Name,
            level.Description,
            level.Rank,
            level.IsActive,
            ToDepositPolicyResponse(level.DepositPolicy),
            level.Requirement is null
                ? null
                : new TrustLevelRequirementResponse(
                    level.Requirement.MinScore,
                    level.Requirement.MinCompletedOrders,
                    level.Requirement.MinTotalOrderValue),
            userCounts.GetValueOrDefault(level.Id),
            level.UpdatedAt);

    private static TrustLevelHistoryItemResponse ToLevelHistoryResponse(TrustLevelHistory history)
    {
        TrustLevelDepositPolicySnapshotResponse? policySnapshot = null;
        if (!string.IsNullOrWhiteSpace(history.DepositPolicySnapshotJson))
        {
            try
            {
                policySnapshot = JsonSerializer.Deserialize<TrustLevelDepositPolicySnapshotResponse>(
                    history.DepositPolicySnapshotJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                policySnapshot = null;
            }
        }

        return new TrustLevelHistoryItemResponse(
            history.Id,
            history.TrustLevelId,
            history.VersionNo,
            history.ChangeType.ToString(),
            history.Name,
            history.Description,
            history.Rank,
            history.IsActive,
            history.MinScore,
            history.MinCompletedOrders,
            history.MinTotalOrderValue,
            policySnapshot,
            history.AffectedUserCount,
            history.ChangedByUserId,
            history.ChangedByUser?.FullName,
            history.ChangedAt);
    }

    private static PenaltyRuleResponse ToPenaltyRuleResponse(RecoveryRequirementRule rule, DateTime now)
        => new(
            rule.Id,
            rule.MinimumOccurrence,
            rule.ScoreDeduction,
            rule.RequiredOnlineOrders,
            rule.RequiredOnlineOrderValue,
            rule.PermanentOfflineLock,
            rule.IsActive,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.IsActive && IsCurrent(rule.EffectiveFrom, rule.EffectiveTo, now),
            rule.CreatedByUser?.FullName,
            rule.CreatedAt);

    private static TrustScoreRuleResponse ToScoreRuleResponse(TrustScoreRule rule, DateTime now)
        => new(
            rule.Id,
            rule.ScoreDelta,
            rule.ScoreDelta > 0 ? KindEarning : KindPenalty,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            IsCurrent(rule.EffectiveFrom, rule.EffectiveTo, now),
            rule.CreatedByUser?.FullName,
            rule.CreatedAt,
            rule.RuleType.ToString());

    private void WriteLevelHistory(
        TrustLevel level,
        int versionNo,
        TrustLevelChangeType changeType,
        int affectedUserCount,
        Guid adminId,
        DateTime now)
    {
        var policySnapshot = SnapshotDepositPolicy(level.DepositPolicy);
        _repository.AddLevelHistory(new TrustLevelHistory
        {
            Id = Guid.NewGuid(),
            TrustLevelId = level.Id,
            VersionNo = versionNo,
            ChangeType = changeType,
            Name = level.Name,
            Description = level.Description,
            Rank = level.Rank,
            IsActive = level.IsActive,
            MinScore = level.Requirement?.MinScore ?? 0,
            MinCompletedOrders = level.Requirement?.MinCompletedOrders ?? 0,
            MinTotalOrderValue = level.Requirement?.MinTotalOrderValue ?? 0m,
            DepositPolicySnapshotJson = policySnapshot is null ? null : JsonSerializer.Serialize(policySnapshot),
            AffectedUserCount = Math.Max(0, affectedUserCount),
            ChangedByUserId = adminId,
            ChangedAt = now
        });
    }

    private void WriteAudit(
        Guid adminId, string action, string targetType, Guid targetId, object? oldValues, object newValues, DateTime now)
    {
        _audit.Record(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = adminId,
            ActorType = AuditActorType.User,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValuesJson = JsonSerializer.Serialize(newValues),
            CreatedAt = now
        });
    }
}