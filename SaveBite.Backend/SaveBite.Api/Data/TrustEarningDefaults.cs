using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.Data;

public static class TrustEarningDefaults
{
    public static async Task EnsureAsync(AppDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE trust_score_rules IN SHARE ROW EXCLUSIVE MODE", ct);
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var (type, points) in new[] { (TrustScoreRuleType.OnlineOrderCompleted, 3), (TrustScoreRuleType.PayAtShopOrderCompleted, 4) })
        {
            if (await db.TrustScoreRules.AnyAsync(r => r.RuleType == type, ct)) continue;
            db.TrustScoreRules.Add(new TrustScoreRule { Id = Guid.NewGuid(), RuleType = type,
                ScoreDelta = points, IsActive = true, CreatedAt = now });
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
