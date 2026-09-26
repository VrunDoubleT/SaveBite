namespace SaveBite.Backend.Models.Entities;

public class PlatformFeeAdjustment
{
    public Guid Id { get; set; }

    public Guid ShopId { get; set; }
    public Guid OrderFeeId { get; set; }
    public Guid RefundId { get; set; }
    
    public Guid? StatementId { get; set; }
    
    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public OrderPlatformFee OrderFee { get; set; } = null!;
    public Refund Refund { get; set; } = null!;
    public PlatformFeeStatement? Statement { get; set; }
}
