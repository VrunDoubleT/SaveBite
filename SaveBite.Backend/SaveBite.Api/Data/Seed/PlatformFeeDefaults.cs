using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Data.Seed;

public static class PlatformFeeDefaults
{
    public static async Task EnsureAsync(AppDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Share the lock used by fee schedule mutations and billing across instances.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        if (!await db.PlatformFeeConfigs.AnyAsync(ct))
        {
            var adminId = await db.Users.Where(u => u.Role == UserRole.Admin)
                .OrderBy(u => u.CreatedAt).ThenBy(u => u.Id)
                .Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("An administrator must be seeded before the default platform fee.");
            var now = clock.GetUtcNow().UtcDateTime;
            db.PlatformFeeConfigs.Add(new PlatformFeeConfig
            {
                Id = Guid.NewGuid(), FeeRate = 0.05m, LifecycleStatus = "Published",
                EffectiveFrom = now, EffectiveTo = null, CreatedAt = now, CreatedBy = adminId,
                Reason = "Initial default platform fee: 5%.", ActivationProcessedAt = now
            });
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
}
