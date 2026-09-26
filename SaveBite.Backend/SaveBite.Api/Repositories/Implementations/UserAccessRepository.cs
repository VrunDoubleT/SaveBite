using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class UserAccessRepository : IUserAccessRepository
{
    private readonly AppDbContext _dbContext;

    public UserAccessRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserAccessState?> GetAccessStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserAccessState(
                user.Status,
                user.CustomerStatus,
                user.ShopStatus,
                user.Role))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> HasStoreOwnerAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _dbContext.Shops
            .AsNoTracking()
            .AnyAsync(
                shop => shop.OwnerUserId == userId &&
                        shop.Status != ShopStatus.Suspended,
                cancellationToken);

    public Task<bool> HasStaffAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _dbContext.ShopStaffMembers
            .AsNoTracking()
            .AnyAsync(
                staff => staff.UserId == userId &&
                         staff.Status == ShopStaffStatus.Active &&
                         staff.Shop.Status != ShopStatus.Suspended,
                cancellationToken);

    public Task<bool> HasStoreOwnerOrStaffAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        // A relationship is authoritative; a role claim alone must not grant
        // access after a staff member is removed or a shop is suspended.
        => _dbContext.Shops
            .AsNoTracking()
            .AnyAsync(
                shop => shop.Status != ShopStatus.Suspended &&
                        (shop.OwnerUserId == userId ||
                         shop.StaffMembers.Any(staff =>
                             staff.UserId == userId &&
                             staff.Status == ShopStaffStatus.Active)),
                cancellationToken);
}
