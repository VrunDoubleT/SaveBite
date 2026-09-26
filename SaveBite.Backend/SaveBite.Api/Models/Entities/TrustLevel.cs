namespace SaveBite.Backend.Models.Entities;

public class TrustLevel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Rank { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public TrustLevelRequirement? Requirement { get; set; }
    public ICollection<UserTrustScore> UserTrustScores { get; set; } = new List<UserTrustScore>();
    public ICollection<UserTrustLevelHistory> PreviousLevelHistories { get; set; } = new List<UserTrustLevelHistory>();
    public ICollection<UserTrustLevelHistory> NewLevelHistories { get; set; } = new List<UserTrustLevelHistory>();
}
