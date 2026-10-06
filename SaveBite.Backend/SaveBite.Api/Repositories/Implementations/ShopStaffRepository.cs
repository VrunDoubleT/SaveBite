using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using InvitationStatus = SaveBite.Backend.Models.Enums.StaffInvitation;
using StaffInvitation = SaveBite.Backend.Models.Entities.StaffInvitation;

namespace SaveBite.Backend.Repositories.Implementations;

public class ShopStaffRepository(AppDbContext context) : IShopStaffRepository
{
    // Entity stores status as string, so the values come from the enum member names
    private const string StatusPending = nameof(InvitationStatus.Pending);
    private const string StatusCancelled = nameof(InvitationStatus.Cancelled);

    // ---------- Users ----------

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();

        return await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }

    // ---------- Invitations ----------

    public async Task<StaffInvitation?> GetInvitationByIdAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        return await context.StaffInvitations
            .Include(i => i.Shop)
            .Include(i => i.InvitedUser)
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
    }

    public async Task<StaffInvitation?> GetPendingInvitationAsync(Guid shopId, Guid invitedUserId, CancellationToken cancellationToken = default)
    {
        return await context.StaffInvitations
            .FirstOrDefaultAsync(i => i.ShopId == shopId
                                   && i.InvitedUserId == invitedUserId
                                   && i.Status == StatusPending, cancellationToken);
    }

    public async Task<IEnumerable<StaffInvitation>> GetInvitationsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await context.StaffInvitations
            .AsNoTracking()
            .Include(i => i.Shop)
            .Include(i => i.InvitedUser)
            .Where(i => i.ShopId == shopId)
            .OrderByDescending(i => i.InvitedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StaffInvitation>> GetInvitationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Revoked (Cancelled) invitations are hidden from the invited user
        return await context.StaffInvitations
            .AsNoTracking()
            .Include(i => i.Shop)
            .Include(i => i.InvitedUser)
            .Where(i => i.InvitedUserId == userId && i.Status != StatusCancelled)
            .OrderByDescending(i => i.InvitedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddInvitationAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
    {
        await context.StaffInvitations.AddAsync(invitation, cancellationToken);
    }

    public Task UpdateInvitationAsync(StaffInvitation invitation, CancellationToken cancellationToken = default)
    {
        // Tracked entities are picked up by the change tracker. Only mark the root
        // entity when detached, so Shop/User loaded via Include are not rewritten.
        // Persisting is handled by IRepositoryTransaction.CommitAsync
        var entry = context.Entry(invitation);
        if (entry.State == EntityState.Detached)
            entry.State = EntityState.Modified;
        return Task.CompletedTask;
    }

    // ---------- Staff ----------

    public async Task<ShopStaff?> GetStaffByIdAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .Include(s => s.Shop)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);
    }

    public async Task<ShopStaff?> GetStaffByShopAndUserAsync(Guid shopId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .Include(s => s.Shop)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.ShopId == shopId && s.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<ShopStaff>> GetStaffsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .AsNoTracking()
            .Include(s => s.Shop)
            .Include(s => s.User)
            .Where(s => s.ShopId == shopId && s.Status != ShopStaffStatus.Removed)
            .OrderBy(s => s.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopStaff>> GetShopsByStaffUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.ShopStaffMembers
            .AsNoTracking()
            .Include(s => s.Shop)
            .Include(s => s.User)
            .Where(s => s.UserId == userId && s.Status == ShopStaffStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task AddStaffAsync(ShopStaff staff, CancellationToken cancellationToken = default)
    {
        await context.ShopStaffMembers.AddAsync(staff, cancellationToken);
    }

    public Task UpdateStaffAsync(ShopStaff staff, CancellationToken cancellationToken = default)
    {
        var entry = context.Entry(staff);
        if (entry.State == EntityState.Detached)
            entry.State = EntityState.Modified;
        return Task.CompletedTask;
    }

    // ---------- Shop ----------

    public async Task<Shop?> GetShopByOwnerUserIdAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(
                shop => shop.OwnerUserId == ownerUserId,
                cancellationToken
            );
    }
    // ---------- Activity logs ----------

    public async Task<IEnumerable<StaffActivityLog>> GetStaffActivityLogsAsync(
        Guid shopId,
        Guid actionByUserId,
        CancellationToken cancellationToken = default)
    {
        return await context.StaffActivityLogs
            .AsNoTracking()
            .Include(l => l.ActionByUser)
            .Include(l => l.Order)
            .Where(l =>
                l.ShopId == shopId &&
                l.ActionBy == actionByUserId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddActivityLogAsync(StaffActivityLog log, CancellationToken cancellationToken = default)
    {
        await context.StaffActivityLogs.AddAsync(log, cancellationToken);
    }
    
    // ---------- Persistence ----------

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // One SaveChanges = one DB transaction, so invitation + staff changes stay atomic
        return await context.SaveChangesAsync(cancellationToken);
    }
}