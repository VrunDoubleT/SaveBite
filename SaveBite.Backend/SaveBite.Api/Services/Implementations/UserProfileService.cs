using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IAuthRepository _authRepository;
    private readonly ICloudinaryService _cloudinaryService;

    public UserProfileService(IAuthRepository authRepository, ICloudinaryService cloudinaryService)
    {
        _authRepository = authRepository;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<CurrentUserResponse> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        
        var fullName = request.FullName.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        user.FullName = fullName;
        user.Phone = phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _authRepository.SaveChangesAsync(cancellationToken);

        return ToCurrentUserResponse(user);
    }

    public async Task<CurrentUserResponse> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var oldAvatarUrl = user.AvatarUrl;
        CloudinaryUploadResult upload;
        try
        {
            upload = await _cloudinaryService.UploadImageAsync(file, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            throw AppException.BadRequest(ex.Message);
        }

        user.AvatarUrl = upload.Url;
        user.UpdatedAt = DateTime.UtcNow;

        await _authRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldAvatarUrl))
        {
            var oldPublicId = GetCloudinaryPublicId(oldAvatarUrl);

            if (!string.IsNullOrWhiteSpace(oldPublicId))
            {
                await _cloudinaryService.DeleteImageAsync(oldPublicId);
            }
        }
        return ToCurrentUserResponse(user);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _authRepository.GetUserByIdAsync(userId, cancellationToken);
        return user ?? throw AppException.Unauthorized();
    }
    
    private static string? GetCloudinaryPublicId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        
        const string uploadMarker = "/upload/";
        var uploadIndex = uri.AbsolutePath.IndexOf(uploadMarker, StringComparison.Ordinal);
        if (uploadIndex < 0) return null;
        
        var publicId = uri.AbsolutePath[(uploadIndex + uploadMarker.Length)..];
        var versionEnd = publicId.IndexOf('/');
        if (versionEnd >= 0 && publicId.StartsWith("v") && long.TryParse(publicId[..versionEnd].AsSpan(1), out _))
        {
            publicId = publicId[(versionEnd + 1)..];
        }

        return Path.ChangeExtension(publicId, null);
    }

    // MAP ENTITY TO RESPONSE
    private static CurrentUserResponse ToCurrentUserResponse(User user)
        => new(
            user.Id,
            user.Email,
            user.Phone,
            user.FullName,
            user.AvatarUrl,
            user.Role.ToString(),
            user.CustomerStatus.ToString());
}
