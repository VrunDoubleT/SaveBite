using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class TrustScoreHistory
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

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
    public TrustScoreRule? Rule { get; set; }
    public NoShowPenalty? NoShowPenalty { get; set; }
}
