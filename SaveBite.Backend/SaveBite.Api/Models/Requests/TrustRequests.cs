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
// Admin-side requests (UC-130, UC-132, UC-134, UC-136, UC-138, UC-153 .. UC-156)
// ---------------------------------------------------------------------

public sealed class TrustLevelDepositRangeRequest
{
    public decimal FromOrderAmount { get; set; }
    public decimal ToOrderAmount { get; set; }
    public decimal DepositAmount { get; set; }
}

public sealed class UpdateTrustLevelBenefitsRequest
{
    /// <summary>Benefit text shown to customers for this level.</summary>
    public string? Description { get; set; }

    /// <summary>When true, every order on this level requires 100% online payment (PayAtShop disabled).</summary>
    public bool RequireFullOnlinePayment { get; set; }

    /// <summary>Orders &lt;= this amount require 0 VND deposit; null when RequireFullOnlinePayment is true.</summary>
    public decimal? DepositFreeOrderAmount { get; set; }

    /// <summary>Orders &gt; this amount require 100% online payment; null when RequireFullOnlinePayment is true.</summary>
    public decimal? MaxDepositOrderAmount { get; set; }

    /// <summary>Contiguous order-value intervals covering (DepositFreeOrderAmount, MaxDepositOrderAmount].</summary>
    public List<TrustLevelDepositRangeRequest> Ranges { get; set; } = new();
}

public sealed class UpdateTrustLevelRequirementRequest
{
    public int MinScore { get; set; }
    public int MinCompletedOrders { get; set; }
    public decimal MinTotalOrderValue { get; set; }
}

// UC-153: Add New Level (Name + Requirement + Benefits/Deposit Policy in one transaction)
public sealed class CreateTrustLevelRequest
{
    public string Name { get; set; } = string.Empty;
    public UpdateTrustLevelRequirementRequest Requirement { get; set; } = new();
    public UpdateTrustLevelBenefitsRequest Benefits { get; set; } = new();
}

// UC-154: Update Level Information (Rename only)
public sealed class UpdateTrustLevelInfoRequest
{
    public string Name { get; set; } = string.Empty;
}

// UC-155: Update Level Status (Deactivate / Reactivate)
public sealed class UpdateTrustLevelStatusRequest
{
    public bool IsActive { get; set; }
}

// UC-156: Add New Penalty Rule
public sealed class CreatePenaltyRuleRequest
{
    public int MinimumOccurrence { get; set; }
    public int ScoreDeduction { get; set; }
    public int RequiredOnlineOrders { get; set; }
    public decimal RequiredOnlineOrderValue { get; set; }
    public bool PermanentOfflineLock { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When the new rule takes effect. Defaults to now.</summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>Optional end time of the rule. Null means open-ended.</summary>
    public DateTime? EffectiveTo { get; set; }
}

// UC-134: Update Penalty Rule
public sealed class UpdatePenaltyRuleRequest
{
    public int MinimumOccurrence { get; set; }
    public int ScoreDeduction { get; set; }
    public int RequiredOnlineOrders { get; set; }
    public decimal RequiredOnlineOrderValue { get; set; }
    public bool PermanentOfflineLock { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When the rule takes effect.</summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>Optional end time of the rule. Null means open-ended.</summary>
    public DateTime? EffectiveTo { get; set; }
}

public sealed class UpdateTrustScoreRuleRequest
{
    public int ScoreDelta { get; set; }

    /// <summary>When the new version takes effect. Defaults to now.</summary>
    public DateTime? EffectiveFrom { get; set; }
}