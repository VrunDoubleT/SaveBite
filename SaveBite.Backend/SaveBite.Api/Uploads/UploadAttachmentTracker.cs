using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Uploads;
public static class UploadAttachmentTracker
{
    public static async Task StageAsync(AppDbContext db, Guid? actorId, CancellationToken ct)
    {
        // Feedback/refund have already-separated upload APIs and retain URL contracts.
        var urls = db.ChangeTracker.Entries<ProductFeedbackImage>().Where(x => x.State == EntityState.Added)
            .Select(x => (Url: x.Entity.ImageUrl, Purpose: "feedback")).ToList();
        urls.AddRange(db.ChangeTracker.Entries<RefundEvidenceImage>().Where(x => x.State == EntityState.Added)
            .Select(x => (x.Entity.ImageUrl, "refund")));
        urls.AddRange(db.ChangeTracker.Entries<Refund>().Where(x => x.State is EntityState.Added or EntityState.Modified &&
            !string.IsNullOrEmpty(x.Entity.ReceiverQrImageUrl) && (x.State == EntityState.Added || x.Property(p => p.ReceiverQrImageUrl).IsModified))
            .Select(x => (x.Entity.ReceiverQrImageUrl!, "refund")));
        if (urls.Count > 0)
        {
            var requested = urls.Select(x => x.Url).Distinct().ToArray();
            var assets = await db.Set<UploadAsset>().Include(x => x.Session).Where(x => requested.Contains(x.Url)).ToListAsync(ct);
            foreach (var asset in assets)
            {
                if (actorId != asset.Session.UserId || !urls.Any(x => x.Url == asset.Url && (x.Purpose == asset.Session.Purpose || x.Purpose == "refund" && asset.Session.Purpose.StartsWith("refund:"))))
                    throw AppException.Forbidden("Upload does not belong to this account or operation.");
                if (asset.Status == "Attached") continue;
                if (asset.Status != "Uploaded" || asset.ExpiresAt <= DateTime.UtcNow || asset.Session.Status != "Active")
                    throw AppException.Conflict("Upload has expired or was cancelled. Please upload again.");
                asset.Status = "Attached";
            }
        }
        // Track session changes in the same SaveChanges transaction as the business entities.
        foreach (var asset in db.ChangeTracker.Entries<UploadAsset>().Where(x => x.Entity.Status == "Attached" && x.State == EntityState.Modified).Select(x => x.Entity).ToArray())
        {
            var session = asset.Session;
            if (session.Purpose is "product" or "shop-application")
            {
                session.Status = "Completed";
                session.ResultId ??= db.ChangeTracker.Entries<Product>().FirstOrDefault(x => x.State == EntityState.Added)?.Entity.Id
                    ?? db.ChangeTracker.Entries<ShopApplication>().FirstOrDefault(x => x.State == EntityState.Added)?.Entity.Id;
            }
        }
    }
}
