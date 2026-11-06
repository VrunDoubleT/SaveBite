using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

// Unified progress ledger of a customer: one row per change of trust score and/or accumulated order value.
public class TrustProgressHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? RuleId { get; set; }

    public Guid? NoShowPenaltyId { get; set; }

    public TrustScoreEventType EventType { get; set; }

    public int ScoreBefore { get; set; }

    public int ScoreDelta { get; set; }

    public int ScoreAfter { get; set; }

    // Accumulated order value (money) before / change / after this event, same idea as the score columns.
    public decimal ValueBefore { get; set; }

    public decimal ValueDelta { get; set; }

    public decimal ValueAfter { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
    public TrustScoreRule? Rule { get; set; }
    public NoShowPenalty? NoShowPenalty { get; set; }
}
