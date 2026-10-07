using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<UserSummaryResponse>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailsResponse> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
}