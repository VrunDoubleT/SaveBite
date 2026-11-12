namespace SaveBite.Backend.Models.Entities;

public class PlatformFeeConfig
{
    public Guid Id { get; set; }
    public decimal FeeRate { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    // Published rows form the effective timeline; terminal decisions are retained permanently.
    public string LifecycleStatus { get; set; } = "Published";
    public string? Reason { get; set; }
    public Guid? ReplacesConfigId { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public string? ClosureReason { get; set; }
    public DateTime? ActivationProcessedAt { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<OrderPlatformFee> OrderPlatformFees { get; set; } = new List<OrderPlatformFee>();
}
