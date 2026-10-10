using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

using InvitationStatus = SaveBite.Backend.Models.Enums.StaffInvitation;
using StaffInvitation = SaveBite.Backend.Models.Entities.StaffInvitation;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class StaffInvitationRepository(AppDbContext context)
    : IStaffInvitationRepository
{
    private const string StatusPending =
        nameof(InvitationStatus.Pending);

    private const string StatusCancelled =
        nameof(InvitationStatus.Cancelled);

    private const int InvitationLifetimeDays = 3;

    public async Task<Shop?> GetShopByOwnerUserIdAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(shop => shop.OwnerUserId == ownerUserId, cancellationToken);
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
        // Pending invitation is considered active.
        // If it has not exceeded the 3-day lifetime.
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

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await context
            .SaveChangesAsync(
                cancellationToken
            );
    }
}
