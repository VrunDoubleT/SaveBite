namespace SaveBite.Backend.Models.Entities;

// Assign one fixed deposit to an order-value interval within a level policy.
public class TrustLevelDepositRange
{
    public Guid Id { get; set; }
    public Guid DepositPolicyId { get; set; }
    // Lower bound is exclusive, upper bound is inclusive: (FromOrderAmount, ToOrderAmount].
    public decimal FromOrderAmount { get; set; }
    public decimal ToOrderAmount { get; set; }
    public decimal DepositAmount { get; set; }

    public TrustLevelDepositPolicy DepositPolicy { get; set; } = null!;
}
