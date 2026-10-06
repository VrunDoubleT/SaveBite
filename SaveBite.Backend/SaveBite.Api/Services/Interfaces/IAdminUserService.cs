using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<UserSummaryResponse>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailsResponse> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SuspendAccountAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task ReactivateAccountAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task SuspendCustomerAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task ReactivateCustomerAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
}