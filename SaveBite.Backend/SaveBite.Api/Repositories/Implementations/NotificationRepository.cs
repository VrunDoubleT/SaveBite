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

    // Return existing rows as well as new rows so broker retries can recover a failed realtime send.
    public async Task<IReadOnlyList<Notification>> CreateManyAsync(
        IReadOnlyList<Notification> notifications, CancellationToken ct = default)
    {
        if (notifications.Count == 0) return Array.Empty<Notification>();
        var results = new List<Notification>();
        foreach (var group in notifications.GroupBy(x => x.DeduplicationKey))
        {
            if (string.IsNullOrWhiteSpace(group.Key))
            {
                var fresh = group.ToList();
                _db.Notifications.AddRange(fresh);
                await _db.SaveChangesAsync(ct);
                results.AddRange(fresh);
                continue;
            }
            // Batch fan-out inserts; all values remain SQL parameters, including user-supplied text.
            foreach (var batch in group.Chunk(500))
            {
                var parameters = new List<object>();
                var rows = new List<string>();
                foreach (var notification in batch)
                {
                    var offset = parameters.Count;
                    parameters.AddRange(new object[] { notification.Id, notification.UserId, notification.Type.ToString(),
                        notification.Title, notification.Body, notification.ActionUrl!, notification.DeduplicationKey!, notification.CreatedAt });
                    rows.Add("(" + string.Join(", ", Enumerable.Range(offset, 8).Select(index => "{" + index + "}")) + ")");
                }
                var sql = "INSERT INTO notifications (id, user_id, type, title, body, action_url, deduplication_key, created_at) VALUES "
                    + string.Join(", ", rows)
                    + " ON CONFLICT (user_id, deduplication_key) WHERE deduplication_key IS NOT NULL DO NOTHING";
                // Resolve only uniqueness conflicts; real database failures propagate to consumer retry.
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    System.Runtime.CompilerServices.FormattableStringFactory.Create(sql, parameters.ToArray()), ct);
            }
            var userIds = group.Select(x => x.UserId).Distinct().ToArray();
            // Never resurrect a notification the recipient has already dismissed.
            results.AddRange(await _db.Notifications.AsNoTracking().Where(x => userIds.Contains(x.UserId)
                && x.DeduplicationKey == group.Key && x.DismissedAt == null).ToListAsync(ct));
        }
        return results;
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

    public async Task<int> MarkAllAsReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId && n.DismissedAt == null && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, readAt), ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
