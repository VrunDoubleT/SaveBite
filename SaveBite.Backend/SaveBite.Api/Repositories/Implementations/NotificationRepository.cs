using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    // Resolve active recipient user IDs for the specified target scope (Customer, Staff, StoreOwner, or Admin).
    public async Task<IReadOnlyList<Guid>> ResolveRecipientUserIdsAsync(
        NotificationTarget target, CancellationToken ct = default)
    {
        switch (target.Scope)
        {
            case NotificationRecipientScope.Users:
            {
                if (target.UserIds == null || target.UserIds.Count == 0)
                    return Array.Empty<Guid>();

                return await _db.Users.AsNoTracking()
                    .Where(u => target.UserIds.Contains(u.Id) && u.Status == UserStatus.Active)
                    .Select(u => u.Id)
                    .ToListAsync(ct);
            }

            case NotificationRecipientScope.OrderCustomer:
            {
                if (!target.EntityId.HasValue) return Array.Empty<Guid>();
                var orderId = target.EntityId.Value;

                var customerId = await _db.Orders.AsNoTracking()
                    .Where(o => o.Id == orderId
                        && o.Customer.Status == UserStatus.Active
                        && o.Customer.CustomerStatus == CustomerStatus.Active)
                    .Select(o => (Guid?)o.CustomerId)
                    .SingleOrDefaultAsync(ct);

                return customerId.HasValue ? new[] { customerId.Value } : Array.Empty<Guid>();
            }

            case NotificationRecipientScope.ShopOwner:
            {
                if (!target.EntityId.HasValue) return Array.Empty<Guid>();
                var shopId = target.EntityId.Value;

                var ownerId = await _db.Shops.AsNoTracking()
                    .Where(s => s.Id == shopId
                        && s.Status == ShopStatus.Active
                        && s.Owner.Status == UserStatus.Active
                        && s.Owner.ShopStatus == ShopAccessStatus.Active)
                    .Select(s => (Guid?)s.OwnerUserId)
                    .SingleOrDefaultAsync(ct);

                return ownerId.HasValue ? new[] { ownerId.Value } : Array.Empty<Guid>();
            }

            case NotificationRecipientScope.ShopStaff:
            {
                if (!target.EntityId.HasValue) return Array.Empty<Guid>();
                var shopId = target.EntityId.Value;

                return await _db.ShopStaffMembers.AsNoTracking()
                    .Where(sm => sm.ShopId == shopId
                        && sm.Status == ShopStaffStatus.Active
                        && sm.Shop.Status == ShopStatus.Active
                        && sm.User.Status == UserStatus.Active)
                    .Select(sm => sm.UserId)
                    .Distinct()
                    .ToListAsync(ct);
            }

            case NotificationRecipientScope.ShopOperators:
            {
                if (!target.EntityId.HasValue) return Array.Empty<Guid>();
                var shopId = target.EntityId.Value;

                return await _db.Users.AsNoTracking()
                    .Where(u => u.Status == UserStatus.Active && u.Role != UserRole.Admin
                        && (_db.Shops.Any(s => s.Id == shopId && s.Status == ShopStatus.Active && s.OwnerUserId == u.Id && u.ShopStatus == ShopAccessStatus.Active)
                            || _db.ShopStaffMembers.Any(sm => sm.ShopId == shopId && sm.UserId == u.Id && sm.Status == ShopStaffStatus.Active && sm.Shop.Status == ShopStatus.Active)))
                    .Select(u => u.Id)
                    .ToListAsync(ct);
            }

            case NotificationRecipientScope.Admins:
            {
                return await _db.Users.AsNoTracking()
                    .Where(u => u.Role == UserRole.Admin && u.Status == UserStatus.Active)
                    .Select(u => u.Id)
                    .ToListAsync(ct);
            }

            case NotificationRecipientScope.Customers:
            {
                return await _db.Users.AsNoTracking()
                    .Where(u => u.Role != UserRole.Admin
                        && u.Status == UserStatus.Active
                        && u.CustomerStatus == CustomerStatus.Active)
                    .Select(u => u.Id)
                    .ToListAsync(ct);
            }

            default:
                return Array.Empty<Guid>();
        }
    }

    // Persist notifications in bulk while preventing duplicates by DeduplicationKey.
    public async Task<IReadOnlyList<Notification>> CreateManyAsync(
        IReadOnlyList<Notification> notifications, CancellationToken ct = default)
    {
        if (notifications.Count == 0) return Array.Empty<Notification>();

        var dedupKey = notifications[0].DeduplicationKey;
        IReadOnlyList<Notification> toInsert = notifications;

        if (!string.IsNullOrWhiteSpace(dedupKey))
        {
            var userIds = notifications.Select(n => n.UserId).ToList();
            var existingUserIds = await _db.Notifications.AsNoTracking()
                .Where(n => userIds.Contains(n.UserId) && n.DeduplicationKey == dedupKey)
                .Select(n => n.UserId)
                .ToListAsync(ct);

            var existingSet = existingUserIds.ToHashSet();
            toInsert = notifications.Where(n => !existingSet.Contains(n.UserId)).ToList();
        }

        if (toInsert.Count == 0) return Array.Empty<Notification>();

        _db.Notifications.AddRange(toInsert);
        try
        {
            await _db.SaveChangesAsync(ct);
            return toInsert;
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(dedupKey))
        {
            foreach (var entry in toInsert)
            {
                _db.Entry(entry).State = EntityState.Detached;
            }
            return Array.Empty<Notification>();
        }
    }

    // Retrieve paginated notifications for the user (excluding soft-deleted notifications where DismissedAt != null).
    public async Task<(IReadOnlyList<Notification> Items, int TotalItems, int UnreadCount)> GetUserNotificationsPageAsync(
        Guid userId, int page, int pageSize, bool? isUnreadOnly, CancellationToken ct = default)
    {
        var baseQuery = _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId && n.DismissedAt == null);

        var unreadCount = await baseQuery.CountAsync(n => n.ReadAt == null, ct);

        var filteredQuery = isUnreadOnly == true
            ? baseQuery.Where(n => n.ReadAt == null)
            : baseQuery;

        var totalItems = await filteredQuery.CountAsync(ct);

        var items = await filteredQuery
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalItems, unreadCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => _db.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && n.DismissedAt == null && n.ReadAt == null, ct);

    public Task<Notification?> GetByIdForUserAsync(
        Guid notificationId, Guid userId, CancellationToken ct = default)
        => _db.Notifications
            .SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && n.DismissedAt == null, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
