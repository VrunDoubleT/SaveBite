namespace SaveBite.Backend.Models.Entities;

public class TrustLevelRequirement
{
    public Guid Id { get; set; }

    public Guid TrustLevelId { get; set; }

    public int MinScore { get; set; }

    public int MinCompletedOrders { get; set; }

    public decimal MinTotalOrderValue { get; set; }

    public TrustLevel TrustLevel { get; set; } = null!;
}