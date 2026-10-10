using Microsoft.AspNetCore.Http;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IUserService
{
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<UserSummaryResponse>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default);
    Task<UserDetailsResponse> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);

    Task<CurrentUserResponse> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserResponse> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default);
}
