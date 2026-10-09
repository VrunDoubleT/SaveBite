using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

using InvitationStatus = SaveBite.Backend.Models.Enums.StaffInvitation;
using StaffInvitation = SaveBite.Backend.Models.Entities.StaffInvitation;

namespace SaveBite.Backend.Repositories.Implementations;

public class ShopStaffRepository(AppDbContext context)
    : IShopStaffRepository
{
    private const string StatusPending =
        nameof(InvitationStatus.Pending);

    private const string StatusCancelled =
        nameof(InvitationStatus.Cancelled);

    private const int InvitationLifetimeDays = 3;


    // =========================================================
    // USERS
    // =========================================================

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


    public async Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == userId,
                cancellationToken
            );
    }


    // =========================================================
    // SEARCH STAFF CANDIDATES
    // =========================================================

    public async Task<(
        IReadOnlyList<User> Items,
        int TotalItems)>
        SearchStaffCandidatesAsync(
            Guid shopId,
            Guid ownerUserId,
            string? keyword,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        // Pending invitation is considered active
        // if it has not exceeded the 3-day lifetime.
        var invitationCutoff =
            DateTime.UtcNow.AddDays(
                -InvitationLifetimeDays
            );

        var query = context.Users
            .AsNoTracking()
            .Where(u =>
                u.Id != ownerUserId
                && u.Status == UserStatus.Active

                && u.Role != UserRole.Admin
                && u.Role != UserRole.StoreOwner

                && !context.ShopStaffMembers.Any(s =>
                    s.ShopId == shopId
                    && s.UserId == u.Id
                    && s.Status != ShopStaffStatus.Removed
                )

                && !context.StaffInvitations.Any(i =>
                    i.ShopId == shopId
                    && i.InvitedUserId == u.Id
                    && i.Status == StatusPending
                    && i.InvitedAt > invitationCutoff
                )
            );

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = keyword
                .Trim()
                .ToLower();

            query = query.Where(u =>
                u.FullName.ToLower().Contains(search)
                || u.Email.ToLower().Contains(search)
            );
        }

        var totalItems =
            await query.CountAsync(
                cancellationToken
            );

        var items = await query
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }


    // =========================================================
    // INVITATIONS
    // =========================================================

    public async Task<StaffInvitation?>
        GetInvitationByIdAsync(
            Guid invitationId,
            CancellationToken cancellationToken = default)
    {
        return await context.StaffInvitations
            .Include(i => i.Shop)
            .Include(i => i.InvitedUser)
            .FirstOrDefaultAsync(
                i => i.Id == invitationId,
                cancellationToken
            );
    }


    public async Task<StaffInvitation?>
        GetPendingInvitationAsync(
            Guid shopId,
            Guid invitedUserId,
            CancellationToken cancellationToken = default)
    {
        return await context.StaffInvitations
            .FirstOrDefaultAsync(
                i =>
                    i.ShopId == shopId
                    && i.InvitedUserId == invitedUserId
                    && i.Status == StatusPending,
                cancellationToken
            );
    }


    public async Task<IEnumerable<StaffInvitation>>
        GetInvitationsByShopIdAsync(
            Guid shopId,
            CancellationToken cancellationToken = default)
    {
        return await context.StaffInvitations
            .AsNoTracking()
            .Include(i => i.Shop)
            .Include(i => i.InvitedUser)
            .Where(i =>
                i.ShopId == shopId
            )
            .OrderByDescending(i =>
                i.InvitedAt
            )
            .ToListAsync(cancellationToken);
    }


    public async Task<(
        IReadOnlyList<StaffInvitation> Items,
        int TotalItems)>
        GetInvitationsByShopIdPagedAsync(
            Guid shopId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        var query = context.StaffInvitations
            .AsNoTracking()
            .Include(i => i.Shop)
            .Include(i => i.InvitedUser)
            .Where(i =>
                i.ShopId == shopId
            );

        var totalItems =
            await query.CountAsync(
                cancellationToken
            );

        var items = await query
            .OrderByDescending(i =>
                i.InvitedAt
            )
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }


    public async Task<IEnumerable<StaffInvitation>>
        GetInvitationsByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        return await context.StaffInvitations
            .AsNoTracking()
            .Include(i => i.Shop)
            .Include(i => i.InvitedUser)
            .Where(i =>
                i.InvitedUserId == userId
                && i.Status != StatusCancelled
            )
            .OrderByDescending(i =>
                i.InvitedAt
            )
            .ToListAsync(cancellationToken);
    }


    public async Task AddInvitationAsync(
        StaffInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        await context.StaffInvitations
            .AddAsync(
                invitation,
                cancellationToken
            );
    }


    public Task UpdateInvitationAsync(
        StaffInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        var entry =
            context.Entry(invitation);

        if (entry.State ==
            EntityState.Detached)
        {
            entry.State =
                EntityState.Modified;
        }

        return Task.CompletedTask;
    }


    // =========================================================
    // STAFF
    // =========================================================

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


    // =========================================================
    // SHOP
    // =========================================================

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


    // =========================================================
    // ACTIVITY LOGS
    // =========================================================

    // Method cũ
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


    // Pagination
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


    // =========================================================
    // PERSISTENCE
    // =========================================================

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