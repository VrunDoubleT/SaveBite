namespace SaveBite.Backend.Models.Entities;

public class PlatformFeeConfig
{
    public Guid Id { get; set; }
    public decimal FeeRate { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public ICollection<OrderPlatformFee> OrderPlatformFees { get; set; } = new List<OrderPlatformFee>();
}
