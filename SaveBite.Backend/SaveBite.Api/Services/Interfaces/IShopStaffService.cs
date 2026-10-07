using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;


namespace SaveBite.Backend.Services.Interfaces;

public interface IShopStaffService
{
    Task<StaffInvitationResponse> InviteStaffAsync(Guid currentUserId, InviteStaffRequest request, CancellationToken cancellationToken = default);
    Task RevokeInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffInvitationResponse>> GetShopInvitationsAsync(Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IEnumerable<StaffInvitationResponse>> GetCustomerInvitationsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task AcceptInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);

    Task DeclineInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);
    Task<PagedResult<ShopStaffResponse>> GetShopStaffsAsync(Guid currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateStaffInfoAsync(Guid currentUserId, Guid staffId, UpdateStaffInfoRequest request, CancellationToken cancellationToken = default);

    Task RemoveStaffAsync(Guid currentUserId, Guid staffId, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffActivityLogResponse>> GetStaffActivityLogsAsync(Guid currentUserId, Guid staffId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<AssociatedShopResponse>> GetAssociatedShopsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<StaffShopInfoResponse> GetShopAndStaffInfoAsync(Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default);
   
    Task<OwnerShopResponse> GetOwnerShopAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffCandidateResponse>> SearchStaffCandidatesAsync(Guid currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default
    );
}