using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;


namespace SaveBite.Backend.Services.Interfaces;

public interface IShopStaffService
{
    Task<StaffInvitationResponse> InviteStaffAsync(Guid currentUserId, Guid shopId, InviteStaffRequest request, CancellationToken cancellationToken = default);
    Task RevokeInvitationAsync(Guid currentUserId, Guid shopId, Guid invitationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StaffInvitationResponse>> GetShopInvitationsAsync(Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default);

    Task<IEnumerable<StaffInvitationResponse>> GetCustomerInvitationsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task AcceptInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);
    Task DeclineInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ShopStaffResponse>> GetShopStaffsAsync(Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default);
    Task UpdateStaffInfoAsync(Guid currentUserId, Guid shopId, Guid staffId, UpdateStaffInfoRequest request, CancellationToken cancellationToken = default);
    Task RemoveStaffAsync(Guid currentUserId, Guid shopId, Guid staffId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StaffActivityLogResponse>> GetStaffActivityLogsAsync(Guid currentUserId, Guid shopId, Guid staffId, CancellationToken cancellationToken = default);

    Task<IEnumerable<AssociatedShopResponse>> GetAssociatedShopsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<StaffShopInfoResponse> GetShopAndStaffInfoAsync(Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default);
    Task<OwnerShopResponse> GetOwnerShopAsync( Guid ownerUserId, CancellationToken cancellationToken
        
    );
}