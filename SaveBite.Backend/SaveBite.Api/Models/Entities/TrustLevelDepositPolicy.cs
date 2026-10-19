namespace SaveBite.Backend.Models.Entities;

// Define full-online payment or the order-value interval eligible for PayAtShop.
public class TrustLevelDepositPolicy
{
    public Guid Id { get; set; }
    public Guid TrustLevelId { get; set; }
    public bool RequireFullOnlinePayment { get; set; }
    // Orders <= this amount need no deposit; null when full online payment is required.
    public decimal? DepositFreeOrderAmount { get; set; }
    // Orders above this amount must be paid in full online.
    public decimal? MaxDepositOrderAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TrustLevel TrustLevel { get; set; } = null!;
    public ICollection<TrustLevelDepositRange> Ranges { get; set; } = new List<TrustLevelDepositRange>();
}
