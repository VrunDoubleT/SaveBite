using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Constants;
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
    private readonly IPenaltyRuleScheduler _penaltyScheduler;
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
        ILogger<TrustConfigService> logger, INotificationQueue notifications, IPenaltyRuleScheduler penaltyScheduler)
    {
        _audit = audit;
        _penaltyScheduler = penaltyScheduler;
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
                WriteAudit(adminId, AuditActionTypes.CreateTrustLevel, "TrustLevel", newLevel.Id, null, SnapshotTrustLevel(newLevel), now);

                await _repository.IncrementPolicyVersionAndQueueOutboxAsync($"Level {newLevel.Name} created", ct);

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
        WriteAudit(adminId, AuditActionTypes.UpdateTrustLevelInfo, "TrustLevel", level.Id, oldValues, SnapshotTrustLevel(level), now);

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

        // UC-155: Deactivate or reactivate a level
    public async Task<AdminTrustLevelResponse> UpdateLevelStatusAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelStatusRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _repository.ExecuteInTransactionAsync(async ct =>
            {
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

                    var currentLowestActive = activeLevels.OrderBy(l => l.Rank).First();
                    if (currentLowestActive.Id == level.Id)
                    {
                        throw AppException.BadRequest(
                            $"Cannot deactivate the lowest active level ({level.Name}). " +
                            "Every customer must qualify for at least one active level with zero minimum requirements.");
                    }

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

                    var affectedUserCount = counts.GetValueOrDefault(level.Id);

                    WriteLevelHistory(
                        level,
                        nextVersion,
                        TrustLevelChangeType.Deactivated,
                        affectedUserCount,
                        adminId,
                        now);
                    WriteAudit(adminId, AuditActionTypes.DeactivateTrustLevel, "TrustLevel", level.Id, oldValues,
                        new { Level = SnapshotTrustLevel(level), AffectedUserCount = affectedUserCount }, now);

                    // Increment Policy Version & Process Outbox in the background
                    await _repository.IncrementPolicyVersionAndQueueOutboxAsync(
                        $"Level {level.Name} deactivated", ct);

                    await _repository.SaveChangesAsync(ct);

                    _logger.LogInformation("Trust level deactivated. LevelId: {LevelId}", level.Id);
                }
                else
                {
                    // Reactivating a level
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
                    WriteAudit(adminId, AuditActionTypes.ReactivateTrustLevel, "TrustLevel", level.Id, oldValues, SnapshotTrustLevel(level), now);

                    // Increment Policy Version & Process Outbox in the background
                    await _repository.IncrementPolicyVersionAndQueueOutboxAsync(
                        $"Level {level.Name} reactivated", ct);

                    await _repository.SaveChangesAsync(ct);

                    _logger.LogInformation("Trust level reactivated. LevelId: {LevelId}", level.Id);
                }

                var updatedCounts = await _repository.GetUserCountsByLevelAsync(ct);
                return ToLevelResponse(level, updatedCounts);
            }, cancellationToken: cancellationToken);

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
            WriteAudit(adminId, AuditActionTypes.UpdateTrustLevelBenefits, "TrustLevel", level.Id, oldValues, SnapshotTrustLevel(level), now);

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
            await _repository.ExecuteInTransactionAsync(async ct =>
            {
                var trackedLevel = await GetLevelAsync(levelId, ct);
                var oldValues = SnapshotTrustLevel(trackedLevel);
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var nextVersion = await _repository.GetNextLevelHistoryVersionAsync(trackedLevel.Id, ct);

                if (trackedLevel.Requirement is null)
                {
                    trackedLevel.Requirement = new TrustLevelRequirement
                    {
                        Id = Guid.NewGuid(),
                        TrustLevelId = trackedLevel.Id
                    };
                    _repository.AddRequirement(trackedLevel.Requirement);
                }

                trackedLevel.Requirement.MinScore = request.MinScore;
                trackedLevel.Requirement.MinCompletedOrders = request.MinCompletedOrders;
                trackedLevel.Requirement.MinTotalOrderValue = request.MinTotalOrderValue;
                trackedLevel.UpdatedAt = now;

                WriteLevelHistory(
                    trackedLevel,
                    nextVersion,
                    TrustLevelChangeType.RequirementUpdated,
                    counts.GetValueOrDefault(trackedLevel.Id),
                    adminId,
                    now);
                WriteAudit(adminId, AuditActionTypes.UpdateTrustLevelRequirement, "TrustLevel", trackedLevel.Id, oldValues, SnapshotTrustLevel(trackedLevel), now);

                // Increment the policy version and add it to the Outbox for the Reconciliation Consumer to process in the background
                await _repository.IncrementPolicyVersionAndQueueOutboxAsync(
                    $"Requirement updated for level {trackedLevel.Name}", ct);

                try
                {
                    await _repository.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    throw AppException.Conflict("A concurrent update occurred on this trust level. Please retry.");
                }

                _logger.LogInformation("Trust level requirement updated. LevelId: {LevelId}", trackedLevel.Id);
                return true;
            }, cancellationToken: cancellationToken);

            level = await GetLevelAsync(levelId, cancellationToken);
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

    public async Task<PenaltyRuleResponse> CreatePenaltyRuleAsync(
        Guid adminId, CreatePenaltyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var changedIds = new List<Guid>();
        var result = await _repository.ExecuteInTransactionAsync(async ct =>
        {
            var rules = (await _repository.LockPenaltyRulesAsync(ct)).ToList();
            var from = ResolveEffectiveFrom(request.EffectiveFrom, now);
            var to = ResolveEffectiveTo(request.EffectiveTo, from);
            var previous = rules.Where(r => r.MinimumOccurrence == request.MinimumOccurrence && r.EffectiveFrom < from)
                .OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
            var next = rules.Where(r => r.MinimumOccurrence == request.MinimumOccurrence && r.EffectiveFrom > from)
                .OrderBy(r => r.EffectiveFrom).FirstOrDefault();
            if (previous != null && previous.EffectiveTo == null)
            {
                var old = SnapshotPenaltyRule(previous);
                previous.EffectiveTo = from;
                previous.EndProcessedAt = null;
                WriteAudit(adminId, AuditActionTypes.UpdatePenaltyRule, "RecoveryRequirementRule", previous.Id, old, SnapshotPenaltyRule(previous), now);
                changedIds.Add(previous.Id);
            }
            // Inserting between versions defaults the new end to the following start.
            if (to == null && next != null) to = next.EffectiveFrom;
            var rule = new RecoveryRequirementRule
            {
                Id = Guid.NewGuid(), MinimumOccurrence = request.MinimumOccurrence, ScoreDeduction = request.ScoreDeduction,
                RequiredOnlineOrders = request.RequiredOnlineOrders, RequiredOnlineOrderValue = request.RequiredOnlineOrderValue,
                PermanentOfflineLock = request.PermanentOfflineLock, IsActive = request.IsActive,
                EffectiveFrom = from, EffectiveTo = to, CreatedByUserId = adminId, CreatedAt = now
            };
            rules.Add(rule);
            PenaltyScheduleRules.Validate(rules, now);
            _repository.AddPenaltyRule(rule);
            WriteAudit(adminId, AuditActionTypes.CreatePenaltyRule, "RecoveryRequirementRule", rule.Id, null, SnapshotPenaltyRule(rule), now);
            await _repository.SaveChangesAsync(ct);
            changedIds.Add(rule.Id);
            return ToPenaltyRuleResponse(rule, now);
        }, cancellationToken: cancellationToken);
        foreach (var id in changedIds) await _penaltyScheduler.SyncAsync(id, cancellationToken);
        return result;
    }

    public async Task<PenaltyRuleResponse> UpdatePenaltyRuleAsync(
        Guid adminId, Guid ruleId, UpdatePenaltyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var result = await _repository.ExecuteInTransactionAsync(async ct =>
        {
            var rules = await _repository.LockPenaltyRulesAsync(ct);
            var rule = rules.SingleOrDefault(r => r.Id == ruleId) ?? throw AppException.NotFound("Penalty rule not found.");
            var old = SnapshotPenaltyRule(rule);
            rule.MinimumOccurrence = request.MinimumOccurrence;
            rule.ScoreDeduction = request.ScoreDeduction;
            rule.RequiredOnlineOrders = request.RequiredOnlineOrders;
            rule.RequiredOnlineOrderValue = request.RequiredOnlineOrderValue;
            rule.PermanentOfflineLock = request.PermanentOfflineLock;
            rule.IsActive = request.IsActive;
            rule.EffectiveFrom = ResolveUpdatedEffectiveFrom(rule.EffectiveFrom, request.EffectiveFrom, now);
            rule.EffectiveTo = ResolveEffectiveTo(request.EffectiveTo, rule.EffectiveFrom);
            rule.StartProcessedAt = null;
            rule.EndProcessedAt = null;
            PenaltyScheduleRules.Validate(rules, now);
            WriteAudit(adminId, AuditActionTypes.UpdatePenaltyRule, "RecoveryRequirementRule", rule.Id, old, SnapshotPenaltyRule(rule), now);
            await _repository.SaveChangesAsync(ct);
            return ToPenaltyRuleResponse(rule, now);
        }, cancellationToken: cancellationToken);
        await _penaltyScheduler.SyncAsync(ruleId, cancellationToken);
        return result;
    }

    public async Task DeletePenaltyRuleAsync(Guid adminId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        await _repository.ExecuteInTransactionAsync(async ct =>
        {
            var rules = (await _repository.LockPenaltyRulesAsync(ct)).ToList();
            var rule = rules.SingleOrDefault(r => r.Id == ruleId) ?? throw AppException.NotFound("Penalty rule not found.");
            if (rule.EffectiveFrom <= _timeProvider.GetUtcNow().UtcDateTime)
                throw AppException.BadRequest("Only future penalty rules can be deleted.");
            if (await _repository.IsPenaltyRuleReferencedAsync(ruleId, ct))
                throw AppException.Conflict("This rule has already been applied.");
            rules.Remove(rule);
            PenaltyScheduleRules.Validate(rules, _timeProvider.GetUtcNow().UtcDateTime);
            _penaltyScheduler.Cancel(rule.StartJobId, rule.EndJobId);
            _repository.RemovePenaltyRule(rule);
            WriteAudit(adminId, AuditActionTypes.DeletePenaltyRule, "RecoveryRequirementRule", rule.Id, SnapshotPenaltyRule(rule), new { Deleted = true }, _timeProvider.GetUtcNow().UtcDateTime);
            await _repository.SaveChangesAsync(ct);
            return true;
        }, cancellationToken: cancellationToken);
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
        => UpdateRuleAsync(adminId, ruleId, request, earningOnly: false, AuditActionTypes.UpdateTrustScoreRule, cancellationToken);

    public Task<TrustScoreRuleResponse> UpdateEarningRuleAsync(
        Guid adminId, Guid ruleId, UpdateTrustScoreRuleRequest request, CancellationToken cancellationToken = default)
        => UpdateRuleAsync(adminId, ruleId, request, earningOnly: true, AuditActionTypes.UpdateTrustEarningRule, cancellationToken);

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
        if (rule == null || rule.RuleType == TrustScoreRuleType.NoShow)
            throw AppException.NotFound("Current earning rule not found. Manage deductions in Penalty Rules.");
        if (request.ScoreDelta <= 0)
            throw AppException.BadRequest("Points must be positive. Use Inactive to stop awarding points.");
        var old = new { rule.ScoreDelta, rule.IsActive };
        rule.ScoreDelta = request.ScoreDelta;
        rule.IsActive = request.IsActive;
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        WriteAudit(adminId, action, "TrustScoreRule", rule.Id, old, new { rule.ScoreDelta, rule.IsActive }, now);
        await _repository.SaveChangesAsync(cancellationToken);
        return ToScoreRuleResponse(rule, now);
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
        => new(rule.Id, rule.ScoreDelta, KindEarning, rule.CreatedByUser?.FullName,
            rule.CreatedAt, rule.RuleType.ToString(), rule.IsActive);

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