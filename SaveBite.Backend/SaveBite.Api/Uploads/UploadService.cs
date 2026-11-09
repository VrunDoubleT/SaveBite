using SaveBite.Backend.Configurations;
using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Uploads;

public sealed class UploadService(AppDbContext db, ICloudinaryService cloud, ICheckoutRepository orders, IShopOrderRepository shops)
{
    public async Task AuthorizeAsync(Guid userId, string purpose, Guid? target, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw AppException.Unauthorized();
        if (user.Status != UserStatus.Active) throw AppException.Forbidden();
        if (purpose == "product")
        {
            if (user.Role != UserRole.StoreOwner || !await db.Shops.AnyAsync(x => x.OwnerUserId == userId && x.Status == ShopStatus.Active, ct)) throw AppException.Forbidden();
        }
        else if (purpose == "shop-application" || purpose == "feedback")
        {
            if (user.CustomerStatus != CustomerStatus.Active || user.Role == UserRole.Admin) throw AppException.Forbidden();
        }
        else if (purpose.StartsWith("refund:"))
        {
            if (!target.HasValue) throw AppException.BadRequest("Order ID is required.");
            if (purpose == "refund:transfer-proof")
            {
                var shopId = await shops.OwnerShopAsync(userId, ct);
                await shops.RequireAccessAsync(userId, shopId, ct, allowSuspended: true);
                _ = await shops.GetAsync(shopId, target.Value, ct) ?? throw AppException.NotFound();
            }
            else if (purpose is "refund:request-evidence" or "refund:destination-qr")
            {
                if (user.CustomerStatus != CustomerStatus.Active) throw AppException.Forbidden();
                _ = await orders.GetOrderAsync(target.Value, userId, ct) ?? throw AppException.NotFound();
            }
            else throw AppException.BadRequest("Invalid upload purpose.");
        }
        else throw AppException.BadRequest("Invalid upload purpose.");
    }
    public async Task<UploadSession> CreateAsync(Guid userId, string purpose, Guid? target, CancellationToken ct)
    {
        await AuthorizeAsync(userId, purpose, target, ct);
        if (await db.Set<UploadSession>().CountAsync(x => x.UserId == userId && x.Status == "Active" && x.ExpiresAt > DateTime.UtcNow, ct) >= 100)
            throw AppException.BadRequest("Too many active upload sessions. Finish or cancel existing uploads.");
        var now = DateTime.UtcNow;
        var session = new UploadSession { Id = Guid.NewGuid(), UserId = userId, Purpose = purpose, TargetId = target, CreatedAt = now, ExpiresAt = now.AddHours(TimingConfiguration.Uploads.SessionLifetimeHours) };
        db.Add(session); await db.SaveChangesAsync(ct); return session;
    }
    public async Task<UploadAsset> UploadAsync(Guid userId, Guid sessionId, Guid assetId, IFormFile file, CancellationToken ct)
    {
        await using var assetLock = await UploadAssetLock.TryAcquireAsync(db, assetId, ct)
            ?? throw AppException.Conflict("This file is still uploading. Please retry shortly.");
        var session = await db.Set<UploadSession>().SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct)
            ?? throw AppException.NotFound();
        await AuthorizeAsync(userId, session.Purpose, session.TargetId, ct);
        if (session.Status != "Active" || session.ExpiresAt <= DateTime.UtcNow) throw AppException.Conflict("Upload session is closed or expired.");
        var max = session.Purpose is "feedback" || session.Purpose.StartsWith("refund:") ? 5L * 1024 * 1024 : 10L * 1024 * 1024;
        if (file.Length <= 0 || file.Length > max) throw AppException.BadRequest($"File must be nonempty and no larger than {max / 1024 / 1024} MB.");
        var raw = session.Purpose == "shop-application" && file.ContentType == "application/pdf";
        if (!raw && file.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) throw AppException.BadRequest("Unsupported file type.");
        var existing = await db.Set<UploadAsset>().SingleOrDefaultAsync(x => x.Id == assetId, ct);
        if (existing != null)
        {
            if (existing.SessionId != sessionId) throw AppException.Forbidden();
            if (existing.Status == "Uploaded") return existing;
            if (existing.Status != "Uploading") throw AppException.Conflict("This upload has already been used or deleted.");
        }
        if (existing == null)
        {
            // Serialize reservation only; never hold a database transaction during network upload.
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM upload_sessions WHERE id = {sessionId} FOR UPDATE", ct);
            var limit = session.Purpose == "product" ? 70 : session.Purpose == "shop-application" ? 12 : session.Purpose == "feedback" ? 5 : 200;
            if (await db.Set<UploadAsset>().CountAsync(x => x.SessionId == sessionId, ct) >= limit) throw AppException.BadRequest("Upload session file limit reached.");
            existing = new UploadAsset { Id = assetId, SessionId = sessionId, FileName = Path.GetFileName(file.FileName), ContentType = file.ContentType,
                Bytes = file.Length, ResourceType = raw ? "raw" : "image", PublicId = cloud.TemporaryPublicId(assetId), CreatedAt = DateTime.UtcNow, ExpiresAt = session.ExpiresAt };
            db.Add(existing); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        // Deterministic public ID is persisted before uploading, allowing orphan recovery.
        if (session.Purpose == "refund:destination-qr")
        {
            var bank = await RefundQrReader.ReadAsync(file, ct);
            existing.MetadataJson = JsonSerializer.Serialize(new
            {
                bankBin = bank.BankBin,
                accountNumber = bank.AccountNumber,
                accountHolder = bank.AccountHolder
            });
        }
        // Once sent to Cloudinary, finish and persist the outcome even if the
        // browser disconnects. Keep the asset lock until both steps complete.
        ct.ThrowIfCancellationRequested();
        var result = await cloud.UploadTemporaryAsync(file, assetId, raw, CancellationToken.None);
        existing.Url = result.Url;
        var stillActive = await db.Set<UploadSession>().AsNoTracking().AnyAsync(x => x.Id == sessionId && x.Status == "Active" && x.ExpiresAt > DateTime.UtcNow, CancellationToken.None);
        existing.Status = stillActive ? "Uploaded" : "Deleting";
        await db.SaveChangesAsync(CancellationToken.None);
        if (!stillActive) throw AppException.Conflict("Upload session was cancelled.");
        return existing;
    }
    public async Task CancelAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await db.Set<UploadSession>().SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct) ?? throw AppException.NotFound();
        if (await db.Set<UploadAsset>().AnyAsync(x => x.SessionId == sessionId && x.Status == "Attached", ct)) throw AppException.Conflict("Some files have already been saved. This operation can no longer be cancelled.");
        session.Status = "Cancelled"; await db.SaveChangesAsync(ct);
    }
}
