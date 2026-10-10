using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public class ShopStaffRepository(AppDbContext context)
    : IShopStaffRepository
{
    // User queries.

    public async Task<User?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();

        return await context.Users
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalized,
                cancellationToken
            );
    }

    // Staff queries.

    public async Task<ShopStaff?>
        GetStaffByIdAsync(
            Guid staffId,
            CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .Include(s => s.Shop)
            .Include(s => s.User)
            .FirstOrDefaultAsync(
                s => s.Id == staffId,
                cancellationToken
            );
    }

    public async Task<ShopStaff?>
        GetStaffByShopAndUserAsync(
            Guid shopId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .Include(s => s.Shop)
            .Include(s => s.User)
            .FirstOrDefaultAsync(
                s =>
                    s.ShopId == shopId
                    && s.UserId == userId,
                cancellationToken
            );
    }

    public async Task<IEnumerable<ShopStaff>>
        GetStaffsByShopIdAsync(
            Guid shopId,
            CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .AsNoTracking()
            .Include(s => s.Shop)
            .Include(s => s.User)
            .Where(s =>
                s.ShopId == shopId
                && s.Status !=
                    ShopStaffStatus.Removed
            )
            .OrderBy(s =>
                s.JoinedAt
            )
            .ToListAsync(cancellationToken);
    }

    public async Task<(
        IReadOnlyList<ShopStaff> Items,
        int TotalItems)>
        GetStaffsByShopIdPagedAsync(
            Guid shopId,
            string? keyword,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        var query =
            context.ShopStaffMembers
                .AsNoTracking()
                .Include(s => s.Shop)
                .Include(s => s.User)
                .Where(s =>
                    s.ShopId == shopId
                    && s.Status !=
                        ShopStaffStatus.Removed
                );

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword.Trim().ToLower();

            query = query.Where(s =>
                s.DisplayName
                    .ToLower()
                    .Contains(search)

                || s.User.Email
                    .ToLower()
                    .Contains(search)

                || (
                    s.Nickname != null
                    && s.Nickname
                        .ToLower()
                        .Contains(search)
                )
            );
        }

        var totalItems =
            await query.CountAsync(
                cancellationToken
            );

        var items = await query
            .OrderBy(s => s.DisplayName)
            .ThenBy(s => s.JoinedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<IEnumerable<ShopStaff>>
        GetShopsByStaffUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .AsNoTracking()
            .Include(s => s.Shop)
            .Include(s => s.User)
            .Where(s =>
                s.UserId == userId
                && s.Status ==
                    ShopStaffStatus.Active
            )
            .ToListAsync(cancellationToken);
    }

    public async Task AddStaffAsync(
        ShopStaff staff,
        CancellationToken cancellationToken = default)
    {
        await context.ShopStaffMembers
            .AddAsync(
                staff,
                cancellationToken
            );
    }

    public Task UpdateStaffAsync(
        ShopStaff staff,
        CancellationToken cancellationToken = default)
    {
        var entry =
            context.Entry(staff);

        if (entry.State ==
            EntityState.Detached)
        {
            entry.State =
                EntityState.Modified;
        }

        return Task.CompletedTask;
    }

    // Shop queries.

    public async Task<Shop?>
        GetShopByOwnerUserIdAsync(
            Guid ownerUserId,
            CancellationToken cancellationToken = default)
    {
        return await context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(
                shop =>
                    shop.OwnerUserId ==
                    ownerUserId,
                cancellationToken
            );
    }

    // Activity log queries.

    // Retrieve activity logs without pagination.
    public async Task<IEnumerable<StaffActivityLog>>
        GetStaffActivityLogsAsync(
            Guid shopId,
            Guid actionByUserId,
            CancellationToken cancellationToken = default)
    {
        return await context.StaffActivityLogs
            .AsNoTracking()
            .Include(l => l.ActionByUser)
            .Include(l => l.Order)
            .Where(l =>
                l.ShopId == shopId
                && l.ActionBy ==
                    actionByUserId
            )
            .OrderByDescending(l =>
                l.CreatedAt
            )
            .ToListAsync(cancellationToken);
    }

    // Pagination.
    public async Task<(
        IReadOnlyList<StaffActivityLog> Items,
        int TotalItems)>
        GetStaffActivityLogsPagedAsync(
            Guid shopId,
            Guid staffUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        var query =
            context.StaffActivityLogs
                .AsNoTracking()
                .Include(l => l.ActionByUser)
                .Include(l => l.Order)
                .Where(l =>
                    l.ShopId == shopId
                    && l.ActionBy ==
                        staffUserId
                );

        var totalItems =
            await query.CountAsync(
                cancellationToken
            );

        var items = await query
            .OrderByDescending(l =>
                l.CreatedAt
            )
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task AddActivityLogAsync(
        StaffActivityLog log,
        CancellationToken cancellationToken = default)
    {
        await context.StaffActivityLogs
            .AddAsync(
                log,
                cancellationToken
            );
    }

    // Persistence operations.

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await context
            .SaveChangesAsync(
                cancellationToken
            );
    }

    public async Task<bool> HasOtherStaffMembershipAsync(
        Guid userId,
        Guid excludedStaffId,
        CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .AnyAsync(
                s =>
                    s.UserId == userId
                    && s.Id != excludedStaffId
                    && s.Status != ShopStaffStatus.Removed,
                cancellationToken
            );
    }
}
