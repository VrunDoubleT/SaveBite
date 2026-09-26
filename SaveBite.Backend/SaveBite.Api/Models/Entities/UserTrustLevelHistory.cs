using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class UserTrustLevelHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    
    public Guid? PreviousTrustLevelId { get; set; }

    public Guid NewTrustLevelId { get; set; }

    public TrustLevelChangeReason Reason { get; set; } = TrustLevelChangeReason.InitialAssignment;
    
    public Guid? OrderId { get; set; }

    public Guid? NoShowPenaltyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public TrustLevel? PreviousTrustLevel { get; set; }
    public TrustLevel NewTrustLevel { get; set; } = null!;
    public Order? Order { get; set; }
    public NoShowPenalty? NoShowPenalty { get; set; }
}
