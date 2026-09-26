namespace SaveBite.Backend.Models.Entities;

public class RecoveryRequirementRule
{
    public Guid Id { get; set; }
    
    public int MinimumOccurrence { get; set; }

    public int ScoreDeduction { get; set; }

    public int RequiredOnlineOrders { get; set; }
    public decimal RequiredOnlineOrderValue { get; set; }

    public bool PermanentOfflineLock { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<NoShowPenalty> NoShowPenalties { get; set; } = new List<NoShowPenalty>();
}
