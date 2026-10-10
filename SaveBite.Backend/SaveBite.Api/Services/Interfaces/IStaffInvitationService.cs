using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IStaffInvitationService
{
    Task<PagedResult<StaffCandidateResponse>> SearchStaffCandidatesAsync(Guid currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<StaffInvitationResponse> InviteStaffAsync(Guid currentUserId, InviteStaffRequest request, CancellationToken cancellationToken = default);
    Task RevokeInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffInvitationResponse>> GetShopInvitationsAsync(Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<StaffInvitationResponse>> GetCustomerInvitationsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task AcceptInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);
    Task DeclineInvitationAsync(Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default);
}
