using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopFollowService(AppDbContext db, TimeProvider clock) : IShopFollowService
{
    public async Task<ShopFollowResponse> FollowAsync(Guid customerId, Guid shopId, CancellationToken ct = default)
    {
        ValidateShopId(shopId);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockCustomer(customerId, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM shops WHERE id = {shopId} FOR SHARE", ct);
        var shop = await db.Shops.AsNoTracking().SingleOrDefaultAsync(s => s.Id == shopId, ct) ?? throw AppException.NotFound("Shop not found.");
        var follow = await db.Set<ShopFollow>().SingleOrDefaultAsync(f => f.CustomerId == customerId && f.ShopId == shopId, ct);
        if (shop.OwnerUserId == customerId) throw AppException.Forbidden("You cannot follow your own shop.");
        if (follow == null)
        {
            if (shop.Status != ShopStatus.Active) throw AppException.Conflict("Only an active shop can be followed.");
            var now = clock.GetUtcNow().UtcDateTime;
            follow = new ShopFollow { CustomerId = customerId, ShopId = shopId, FollowedAt = now, UpdatedAt = now };
            db.Add(follow);
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return Map(follow, shop); // Repeated follow preserves the user's notification preference and date.
    }

    public async Task UnfollowAsync(Guid customerId, Guid shopId, CancellationToken ct = default)
    {
        ValidateShopId(shopId);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockCustomer(customerId, ct);
        await db.Set<ShopFollow>().Where(f => f.CustomerId == customerId && f.ShopId == shopId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct); // Idempotent, including when the shop is unavailable/deleted.
    }

    public async Task<ShopFollowResponse> UpdateSettingsAsync(Guid customerId, Guid shopId, ShopNotificationStatus status, CancellationToken ct = default)
    {
        ValidateShopId(shopId);
        if (!Enum.IsDefined(status)) throw AppException.BadRequest("NotificationStatus must be Enabled or Disabled.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockCustomer(customerId, ct);
        var follow = await db.Set<ShopFollow>().Include(f => f.Shop)
            .SingleOrDefaultAsync(f => f.CustomerId == customerId && f.ShopId == shopId, ct)
            ?? throw AppException.NotFound("Follow this shop before changing notification settings.");
        follow.NotificationStatus = status;
        follow.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Map(follow, follow.Shop);
    }

    public async Task<PagedResult<ShopFollowResponse>> GetFollowedShopsAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default)
    {
        await RequireCustomer(customerId, ct);
        if (page < 1 || page > 100000 || pageSize is < 1 or > 100)
            throw AppException.BadRequest("Page must be 1–100000; pageSize must be 1–100.");
        var query = db.Set<ShopFollow>().AsNoTracking().Where(f => f.CustomerId == customerId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(f => f.FollowedAt).ThenBy(f => f.ShopId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new ShopFollowResponse(f.ShopId, f.Shop.Name, f.Shop.LogoUrl, f.Shop.AddressLine,
                f.Shop.Status, f.NotificationStatus, f.FollowedAt)).ToListAsync(ct);
        return PagedResult<ShopFollowResponse>.Create(items, page, pageSize, total);
    }

    private async Task LockCustomer(Guid customerId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {customerId} FOR UPDATE", ct);
        // A scope may have tracked this follow before another command changed it.
        db.ChangeTracker.Clear();
        await RequireCustomer(customerId, ct);
    }
    private async Task RequireCustomer(Guid customerId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == customerId, ct);
        if (user == null) throw AppException.Unauthorized();
        if (user.Role == UserRole.Admin || user.Status != UserStatus.Active || user.CustomerStatus != CustomerStatus.Active)
            throw AppException.Forbidden("An active customer account is required.");
    }
    private static void ValidateShopId(Guid id) { if (id == Guid.Empty) throw AppException.BadRequest("Shop ID is required."); }
    private static ShopFollowResponse Map(ShopFollow follow, Shop shop) => new(shop.Id, shop.Name, shop.LogoUrl,
        shop.AddressLine, shop.Status, follow.NotificationStatus, follow.FollowedAt);
}
