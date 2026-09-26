using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class NoShowPenalty
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid OrderId { get; set; }

    public Guid RecoveryRuleId { get; set; }
    public int PenaltyYear { get; set; }
    public int PenaltyMonth { get; set; }
    
    public int OccurrenceNumber { get; set; }

    // Snapshot
    public int ScoreDeducted { get; set; }
    public int RequiredOnlineOrders { get; set; }
    public decimal RequiredOnlineOrderValue { get; set; }
    
    // Progress Recovery
    public int CompletedOnlineOrders { get; set; }
    public decimal CompletedOnlineOrderValue { get; set; }

    public bool PermanentOfflineLock { get; set; }

    public DateTime? LockEndsAt { get; set; }

    public NoShowPenaltyStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RecoveredAt { get; set; }

    public User User { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public RecoveryRequirementRule RecoveryRule { get; set; } = null!;

    public ICollection<NoShowPenaltyOrderHistory> RecoveryOrders { get; set; } = new List<NoShowPenaltyOrderHistory>();
    public ICollection<TrustScoreHistory> TrustScoreHistories { get; set; } = new List<TrustScoreHistory>();
    public ICollection<UserTrustLevelHistory> TrustLevelHistories { get; set; } = new List<UserTrustLevelHistory>();
}
