namespace SaveBite.Backend.Models.Entities;

public class UserTrustScore
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TrustLevelId { get; set; }

    public int CurrentScore { get; set; }

    public bool IsOfflineLockedPermanently { get; set; }

    /// <summary>
    /// The latest Trust Level configuration version has been applied to this user.
    /// Ensure that an old consumer never overwrites the results produced by a new consumer.
    /// </summary>
    public long? AppliedPolicyVersion { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public TrustLevel TrustLevel { get; set; } = null!;
}