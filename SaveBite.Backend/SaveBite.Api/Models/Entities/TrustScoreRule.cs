namespace SaveBite.Backend.Models.Entities;

public class TrustScoreRule
{
    public Guid Id { get; set; }

    public int ScoreDelta { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public ICollection<TrustScoreHistory> Histories { get; set; } = new List<TrustScoreHistory>();
}
