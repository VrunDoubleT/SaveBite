using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class TrustLevelHistory
{
    public Guid Id { get; set; }

    public Guid TrustLevelId { get; set; }

    public int VersionNo { get; set; }

    public TrustLevelChangeType ChangeType { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Rank { get; set; }

    public bool IsActive { get; set; }

    public int MinScore { get; set; }

    public int MinCompletedOrders { get; set; }

    public decimal MinTotalOrderValue { get; set; }

    public string? DepositPolicySnapshotJson { get; set; }

    public int AffectedUserCount { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }

    public TrustLevel TrustLevel { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}