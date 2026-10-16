using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

// ---------------------------------------------------------------------
// Customer-side requests (UC-46, UC-48)
// ---------------------------------------------------------------------

public sealed class GetTrustScoreHistoryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Optional filter by score event type.</summary>
    public TrustScoreEventType? EventType { get; set; }
}

public sealed class GetPenaltiesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Optional filter by penalty status (Recovering / Recovered).</summary>
    public NoShowPenaltyStatus? Status { get; set; }
}

// ---------------------------------------------------------------------
// Admin-side requests (UC-130, UC-132, UC-134, UC-136, UC-138)
// ---------------------------------------------------------------------

public sealed class UpdateTrustLevelBenefitsRequest
{
    /// <summary>Benefit text shown to customers for this level.</summary>
    public string? Description { get; set; }
}

public sealed class UpdateTrustLevelRequirementRequest
{
    public int MinScore { get; set; }
    public int MinCompletedOrders { get; set; }
    public decimal MinTotalOrderValue { get; set; }
}

public sealed class UpdatePenaltyRuleRequest
{
    public int MinimumOccurrence { get; set; }
    public int ScoreDeduction { get; set; }
    public int RequiredOnlineOrders { get; set; }
    public decimal RequiredOnlineOrderValue { get; set; }
    public bool PermanentOfflineLock { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When the new version takes effect. Defaults to now.</summary>
    public DateTime? EffectiveFrom { get; set; }
}

public sealed class UpdateTrustScoreRuleRequest
{
    public int ScoreDelta { get; set; }

    /// <summary>When the new version takes effect. Defaults to now.</summary>
    public DateTime? EffectiveFrom { get; set; }
}
