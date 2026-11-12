using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class TrustScoreRule
{
    public Guid Id { get; set; }

    public TrustScoreRuleType RuleType { get; set; }

    public int ScoreDelta { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<TrustProgressHistory> Histories { get; set; } = new List<TrustProgressHistory>();
}
