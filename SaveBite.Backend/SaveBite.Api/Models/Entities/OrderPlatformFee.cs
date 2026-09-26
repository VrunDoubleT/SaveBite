namespace SaveBite.Backend.Models.Entities;

public class OrderPlatformFee
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Guid ShopId { get; set; }
    public Guid FeeConfigId { get; set; }
    
    public Guid? StatementId { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal FeeRate { get; set; }
    public decimal FeeAmount { get; set; }

    public DateTime CalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public PlatformFeeConfig FeeConfig { get; set; } = null!;
    public PlatformFeeStatement? Statement { get; set; }
    public ICollection<PlatformFeeAdjustment> Adjustments { get; set; } = new List<PlatformFeeAdjustment>();
}
