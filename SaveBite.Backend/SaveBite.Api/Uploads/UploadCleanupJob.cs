using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Uploads;
public sealed class UploadCleanupJob(AppDbContext db, ICloudinaryService cloud, ILogger<UploadCleanupJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.Set<UploadSession>().Where(x => x.Status == "Active" && x.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Expired"), ct);
        var assets = await db.Set<UploadAsset>().AsNoTracking()
            .Where(x => x.Status != "Attached" && x.Status != "Deleted" &&
                (x.ExpiresAt <= now || x.Session.Status == "Cancelled" || x.Status == "Deleting"))
            .OrderBy(x => x.ExpiresAt).Take(200).ToListAsync(ct);
        foreach (var asset in assets)
        {
            // CAS against attachment; cleanup can never delete an asset whose save won.
            var claimed = await db.Set<UploadAsset>().Where(x => x.Id == asset.Id && x.Status != "Attached" && x.Status != "Deleted")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Deleting"), ct);
            if (claimed == 0) continue;
            try
            {
                if (await cloud.DeleteAssetAsync(asset.PublicId, asset.ResourceType, ct))
                    await db.Set<UploadAsset>().Where(x => x.Id == asset.Id && x.Status == "Deleting")
                        .ExecuteDeleteAsync(ct);
            }
            catch (Exception error) { logger.LogWarning(error, "Temporary upload cleanup failed for {AssetId}; retry next sweep", asset.Id); }
        }
        await db.Set<UploadSession>()
            .Where(x => (x.Status == "Expired" || x.Status == "Cancelled") &&
                !x.Assets.Any())
            .ExecuteDeleteAsync(ct);
    }
}
