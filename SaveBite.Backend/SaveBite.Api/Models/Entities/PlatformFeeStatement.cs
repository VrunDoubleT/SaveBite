using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class PlatformFeeStatement
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }

    public int BillingYear { get; set; }
    public short BillingMonth { get; set; }

    public PlatformFeeStatementStatus Status { get; set; } = PlatformFeeStatementStatus.Draft;

    public decimal TotalFeeAmount { get; set; }

    public DateTime? IssuedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public ICollection<OrderPlatformFee> OrderPlatformFees { get; set; } = new List<OrderPlatformFee>();
    public ICollection<PlatformFeeAdjustment> Adjustments { get; set; } = new List<PlatformFeeAdjustment>();
    public ICollection<PlatformFeePayment> Payments { get; set; } = new List<PlatformFeePayment>();
}
