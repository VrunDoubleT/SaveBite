using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IStaffInvitationRepository
{
    Task<Shop?> GetShopByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalItems)> SearchStaffCandidatesAsync(Guid shopId, Guid ownerUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<StaffInvitation?> GetInvitationByIdAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<StaffInvitation?> GetPendingInvitationAsync(Guid shopId, Guid invitedUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StaffInvitation>> GetInvitationsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StaffInvitation> Items, int TotalItems)> GetInvitationsByShopIdPagedAsync(Guid shopId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<StaffInvitation>> GetInvitationsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddInvitationAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
    Task UpdateInvitationAsync(StaffInvitation invitation, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
