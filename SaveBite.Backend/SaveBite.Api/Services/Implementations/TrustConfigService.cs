using System.Text.Json;
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
    private const string KindEarning = "Earning";
    private const string KindPenalty = "Penalty";

    // Small allowance so "effective now" requests are not rejected because of clock drift.
    private static readonly TimeSpan EffectiveFromTolerance = TimeSpan.FromMinutes(1);

    private readonly ITrustConfigRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TrustConfigService> _logger;

    public TrustConfigService(
        ITrustConfigRepository repository,
        TimeProvider timeProvider,
        ILogger<TrustConfigService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Levels (UC-129 .. UC-132)
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<AdminTrustLevelResponse>> GetLevelsAsync(CancellationToken cancellationToken = default)
    {
        var levels = await _repository.GetLevelsAsync(cancellationToken);
        var counts = await _repository.GetUserCountsByLevelAsync(cancellationToken);

        return levels.Select(level => ToLevelResponse(level, counts)).ToList();
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

        if (!IsBenefitsUnchanged(level, description, request.RequireFullOnlinePayment, freeAmount, maxAmount, orderedRanges))
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var oldSnapshot = SnapshotLevelBenefits(level);

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

            var newSnapshot = SnapshotLevelBenefits(level);
            WriteAudit(adminId, "UpdateTrustLevelBenefits", "TrustLevel", level.Id, oldSnapshot, newSnapshot, now);

            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Trust level benefits and deposit policy updated. LevelId: {LevelId}", level.Id);
        }

        return ToLevelResponse(level, await _repository.GetUserCountsByLevelAsync(cancellationToken));
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

    private static object SnapshotLevelBenefits(TrustLevel level) => new
    {
        level.Description,
        DepositPolicy = level.DepositPolicy is null
            ? null
            : new
            {
                level.DepositPolicy.RequireFullOnlinePayment,
                level.DepositPolicy.DepositFreeOrderAmount,
                level.DepositPolicy.MaxDepositOrderAmount,
                Ranges = level.DepositPolicy.Ranges
                    .OrderBy(r => r.FromOrderAmount)
                    .Select(r => new
                    {
                        r.FromOrderAmount,
                        r.ToOrderAmount,
                        r.DepositAmount
                    })
                    .ToList()
            }
    };

    // UC-132: the thresholds a customer must reach to be on a level.
    public async Task<AdminTrustLevelResponse> UpdateLevelRequirementAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var level = await GetLevelAsync(levelId, cancellationToken);

        if (level.IsActive)
        {
            var activeLevels = await _repository.GetActiveLevelsForValidationAsync(cancellationToken);
            ValidateThresholds(activeLevels, level.Id, request);
        }

        var old = level.Requirement is null
            ? null
            : new
            {
                level.Requirement.MinScore,
                level.Requirement.MinCompletedOrders,
                level.Requirement.MinTotalOrderValue
            };

        var unchanged = level.Requirement is not null
                        && level.Requirement.MinScore == request.MinScore
                        && level.Requirement.MinCompletedOrders == request.MinCompletedOrders
                        && level.Requirement.MinTotalOrderValue == request.MinTotalOrderValue;

        if (!unchanged)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (level.Requirement is null)
            {
                level.Requirement = new TrustLevelRequirement
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level.Id
                };
            }

            level.Requirement.MinScore = request.MinScore;
            level.Requirement.MinCompletedOrders = request.MinCompletedOrders;
            level.Requirement.MinTotalOrderValue = request.MinTotalOrderValue;
            level.UpdatedAt = now;

            WriteAudit(adminId, "UpdateTrustLevelRequirement", "TrustLevel", level.Id, old,
                new { request.MinScore, request.MinCompletedOrders, request.MinTotalOrderValue }, now);

            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Trust level requirement updated. LevelId: {LevelId}", level.Id);
        }

        return ToLevelResponse(level, await _repository.GetUserCountsByLevelAsync(cancellationToken));
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
    // Penalty rules (UC-133, UC-134)
    // -----------------------------------------------------------------------

    public async Task<IReadOnlyList<PenaltyRuleResponse>> GetPenaltyRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _repository.GetPenaltyRulesAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return rules.Select(rule => ToPenaltyRuleResponse(rule, now)).ToList();
    }

    // UC-134: saving creates a new version; earlier versions are kept so existing penalties
    // keep the requirements they were created with (BR-160, BR-161).
    public async Task<PenaltyRuleResponse> UpdatePenaltyRuleAsync(
        Guid adminId, Guid ruleId, UpdatePenaltyRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await _repository.GetPenaltyRuleForUpdateAsync(ruleId, cancellationToken)
                   ?? throw AppException.NotFound("Penalty rule not found.");

        if (rule.EffectiveTo.HasValue)
            throw AppException.Conflict("This rule version has been replaced. Update the latest version of the rule instead.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        // A version that has not started yet is edited in place and keeps its start time.
        var isPending = rule.EffectiveFrom > now;
        var effectiveFrom = isPending ? rule.EffectiveFrom : ResolveEffectiveFrom(request.EffectiveFrom, now);

        // BR-159: only one enabled rule per occurrence tier at any time.
        if (request.IsActive
            && await _repository.HasOverlappingPenaltyRuleAsync(request.MinimumOccurrence, effectiveFrom, rule.Id, cancellationToken))
        {
            throw AppException.Conflict($"Another active penalty rule already covers occurrence {request.MinimumOccurrence}.");
        }

        var old = SnapshotPenaltyRule(rule);
        RecoveryRequirementRule target;

        if (isPending)
        {
            // Scheduled but not applied yet: nothing depends on it, so it is edited in place.
            target = rule;
            ApplyPenaltyValues(target, request);
        }
        else
        {
            if (effectiveFrom <= rule.EffectiveFrom)
                throw AppException.BadRequest("The new version must start after the current version.");

            rule.EffectiveTo = effectiveFrom;

            target = new RecoveryRequirementRule
            {
                Id = Guid.NewGuid(),
                EffectiveFrom = effectiveFrom,
                CreatedByUserId = adminId,
                CreatedAt = now
            };
            ApplyPenaltyValues(target, request);
            _repository.AddPenaltyRule(target);
        }

        WriteAudit(adminId, "UpdatePenaltyRule", "RecoveryRequirementRule", target.Id, old, SnapshotPenaltyRule(target), now);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Penalty rule updated. RuleId: {RuleId}, PreviousRuleId: {PreviousRuleId}", target.Id, rule.Id);

        return ToPenaltyRuleResponse(target, now);
    }

    private static void ApplyPenaltyValues(RecoveryRequirementRule rule, UpdatePenaltyRuleRequest request)
    {
        rule.MinimumOccurrence = request.MinimumOccurrence;
        rule.ScoreDeduction = request.ScoreDeduction;
        rule.RequiredOnlineOrders = request.RequiredOnlineOrders;
        rule.RequiredOnlineOrderValue = request.RequiredOnlineOrderValue;
        rule.PermanentOfflineLock = request.PermanentOfflineLock;
        rule.IsActive = request.IsActive;
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

        // BR-161: one rule per direction at any time.
        if (await _repository.HasOverlappingScoreRuleAsync(rule.ScoreDelta > 0, effectiveFrom, rule.Id, cancellationToken))
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

    private static DateTime ResolveEffectiveFrom(DateTime? requested, DateTime now)
    {
        if (!requested.HasValue)
            return now;

        var value = requested.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(requested.Value, DateTimeKind.Utc)
            : requested.Value.ToUniversalTime();

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
            rule.CreatedAt);

    private void WriteAudit(
        Guid adminId, string action, string targetType, Guid targetId, object? oldValues, object newValues, DateTime now)
    {
        _repository.AddAuditLog(new AuditLog
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