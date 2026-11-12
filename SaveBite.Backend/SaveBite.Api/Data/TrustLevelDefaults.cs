using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Data;

// Required application configuration, independent of optional sample data.
public static class TrustLevelDefaults
{
    public static async Task EnsureAsync(AppDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize the empty-table check and insertion across container instances and admin writes.
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE trust_levels IN SHARE ROW EXCLUSIVE MODE", ct);

        if (!await db.TrustLevels.AnyAsync(ct))
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var level = new TrustLevel
            {
                Id = Guid.NewGuid(),
                Name = "Bronze",
                Description = "Cap khoi dau.",
                Rank = 1,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            level.Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = level.Id,
                MinScore = 0,
                MinCompletedOrders = 0,
                MinTotalOrderValue = 0m
            };
            level.DepositPolicy = new TrustLevelDepositPolicy
            {
                Id = Guid.NewGuid(),
                TrustLevelId = level.Id,
                RequireFullOnlinePayment = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            level.Histories.Add(new TrustLevelHistory
            {
                Id = Guid.NewGuid(),
                TrustLevelId = level.Id,
                VersionNo = 1,
                ChangeType = TrustLevelChangeType.Created,
                Name = level.Name,
                Description = level.Description,
                Rank = level.Rank,
                IsActive = true,
                MinScore = 0,
                MinCompletedOrders = 0,
                MinTotalOrderValue = 0m,
                DepositPolicySnapshotJson = JsonSerializer.Serialize(new
                {
                    RequireFullOnlinePayment = true,
                    DepositFreeOrderAmount = (decimal?)null,
                    MaxDepositOrderAmount = (decimal?)null,
                    Ranges = Array.Empty<object>()
                }),
                ChangedAt = now
            });
            db.TrustLevels.Add(level);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }
}
