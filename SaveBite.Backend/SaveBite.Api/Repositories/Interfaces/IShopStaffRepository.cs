using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IShopStaffRepository
{
    // Invitations
    Task<StaffInvitation?> GetInvitationByIdAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<StaffInvitation?> GetPendingInvitationAsync(Guid shopId, Guid invitedUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StaffInvitation>> GetInvitationsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StaffInvitation>> GetInvitationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddInvitationAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
    Task UpdateInvitationAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Shop?> GetShopByOwnerUserIdAsync( Guid ownerUserId, CancellationToken cancellationToken = default
        );
    
    // Persistence
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Staff
    Task<ShopStaff?> GetStaffByIdAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task<ShopStaff?> GetStaffByShopAndUserAsync(Guid shopId, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopStaff>> GetStaffsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopStaff>> GetShopsByStaffUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddStaffAsync(ShopStaff staff, CancellationToken cancellationToken = default);
    Task UpdateStaffAsync(ShopStaff staff, CancellationToken cancellationToken = default);

    // Activity logs (a log belongs to a shop and is linked to staff via ActionBy = staff's user id)
    Task<IEnumerable<StaffActivityLog>> GetStaffActivityLogsAsync(Guid shopId, Guid actionByUserId, CancellationToken cancellationToken = default);
    Task AddActivityLogAsync(StaffActivityLog log, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalItems)> SearchStaffCandidatesAsync(Guid shopId, Guid ownerUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StaffInvitation> Items, int TotalItems)> GetInvitationsByShopIdPagedAsync(Guid shopId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ShopStaff> Items, int TotalItems)> GetStaffsByShopIdPagedAsync(Guid shopId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StaffActivityLog> Items, int TotalItems)> GetStaffActivityLogsPagedAsync(Guid shopId, Guid staffUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> HasOtherStaffMembershipAsync(Guid userId, Guid excludedStaffId, CancellationToken cancellationToken = default);    
}