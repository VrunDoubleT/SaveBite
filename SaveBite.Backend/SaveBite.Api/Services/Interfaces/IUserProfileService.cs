using Microsoft.AspNetCore.Http;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IUserProfileService
{
    Task<CurrentUserResponse> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserResponse> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default);
}
