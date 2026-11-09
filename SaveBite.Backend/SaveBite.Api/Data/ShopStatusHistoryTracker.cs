using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Data;

public sealed class ShopStatusHistoryTracker
{
    private readonly List<ShopStatusHistory> pending = [];
    private readonly Dictionary<Guid, (Guid ActorId, string? Reason)> adminChanges = [];
    public void AdminChange(Guid shopId, Guid actorId, string? reason) => adminChanges[shopId] = (actorId, reason);
    public void Saved() => adminChanges.Clear();

    public void Stage(AppDbContext db, Guid? actorId)
    {
        // Rebuild unsaved rows after a failed SaveChanges instead of duplicating history.
        foreach (var row in pending)
            if (db.Entry(row).State == EntityState.Added) db.Entry(row).State = EntityState.Detached;
        pending.Clear();
        db.ChangeTracker.DetectChanges();
        foreach (var entry in db.ChangeTracker.Entries<Shop>().Where(e => e.State == EntityState.Modified).ToArray())
        {
            var shop = entry.Entity;
            var from = entry.Property(x => x.Status).OriginalValue;
            var admin = entry.Property(x => x.SuspendedByAdmin).OriginalValue;
            var fees = entry.Property(x => x.SuspendedForPlatformFees).OriginalValue;
            var refunds = entry.Property(x => x.SuspendedForOverdueRefunds).OriginalValue;
            var manual = adminChanges.TryGetValue(shop.Id, out var change);
            if (from == shop.Status && admin == shop.SuspendedByAdmin && fees == shop.SuspendedForPlatformFees
                && refunds == shop.SuspendedForOverdueRefunds && !manual) continue;
            var reasons = new List<string>();
            if (manual && !string.IsNullOrWhiteSpace(change.Reason)) reasons.Add(change.Reason);
            if (admin != shop.SuspendedByAdmin) reasons.Add(shop.SuspendedByAdmin ? "Administrator suspension applied." : "Administrator suspension removed.");
            if (fees != shop.SuspendedForPlatformFees) reasons.Add(shop.SuspendedForPlatformFees ? "Overdue platform fees remain unpaid." : "Overdue platform fees settled.");
            if (refunds != shop.SuspendedForOverdueRefunds) reasons.Add(shop.SuspendedForOverdueRefunds ? "Mandatory refunds are overdue and unpaid." : "All overdue mandatory refunds settled.");
            if (reasons.Count == 0) reasons.Add($"Shop status changed from {from} to {shop.Status}.");
            var row = new ShopStatusHistory
            {
                Id = Guid.NewGuid(), ShopId = shop.Id, FromStatus = from, ToStatus = shop.Status,
                WasSuspendedByAdmin = admin, WasSuspendedForPlatformFees = fees, WasSuspendedForOverdueRefunds = refunds,
                SuspendedByAdmin = shop.SuspendedByAdmin, SuspendedForPlatformFees = shop.SuspendedForPlatformFees,
                SuspendedForOverdueRefunds = shop.SuspendedForOverdueRefunds,
                ChangedByType = manual ? "Admin" : actorId.HasValue ? "User" : "System",
                ChangedByUserId = manual ? change.ActorId : actorId,
                Reason = string.Join(" ", reasons), CreatedAt = DateTime.UtcNow
            };
            db.ShopStatusHistories.Add(row);
            pending.Add(row);
        }
    }
}
