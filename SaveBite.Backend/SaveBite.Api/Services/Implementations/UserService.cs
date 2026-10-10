using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
using System.Linq;

namespace SaveBite.Backend.Services.Implementations;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UserService> _logger;
    private readonly ICloudinaryService _cloudinaryService;

    public UserService(
        IUserRepository repository,
        IRefreshTokenService refreshTokenService,
        TimeProvider timeProvider,
        ILogger<UserService> logger,
        ICloudinaryService cloudinaryService)
    {
        _repository = repository;
        _refreshTokenService = refreshTokenService;
        _timeProvider = timeProvider;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetUserWithAddressesAsync(
            userId,
            cancellationToken);

        if (user is null)
            throw AppException.Unauthorized();

        var defaultAddress = user.Addresses.FirstOrDefault(a => a.IsDefault) ?? user.Addresses.FirstOrDefault();
        var addressResponse = defaultAddress != null
            ? new DefaultAddressResponse(
                defaultAddress.Id,
                defaultAddress.Label,
                defaultAddress.AddressLine,
                defaultAddress.Ward,
                defaultAddress.District,
                defaultAddress.City,
                defaultAddress.Latitude,
                defaultAddress.Longitude,
                defaultAddress.IsDefault)
            : null;

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.Phone,
            user.FullName,
            user.AvatarUrl,
            user.Role.ToString(),
            user.CustomerStatus.ToString(),
            addressResponse);
    }

    public async Task<PagedResult<UserSummaryResponse>> GetUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default)
    {
        var pagedUsers = await _repository.GetPagedUsersAsync(request, cancellationToken);

        var items = pagedUsers.Items.Select(u => new UserSummaryResponse(
            u.Id,
            u.Email,
            u.FullName,
            u.Role.ToString(),
            u.Status.ToString(),
            u.CustomerStatus.ToString(),
            u.CreatedAt
        )).ToList();

        return PagedResult<UserSummaryResponse>.Create(items, pagedUsers.Page, pagedUsers.PageSize, pagedUsers.TotalItems);
    }

    public async Task<UserDetailsResponse> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (user == null) throw AppException.NotFound("User not found.");

        // Retrieve audit log history.
        var auditLogs = await _repository.GetUserAuditLogsAsync(userId, cancellationToken);
        var auditLogResponses = auditLogs.Select(l => new UserLogResponse(
            l.Action,
            l.Reason ?? "",
            l.NewValuesJson ?? "",
            l.CreatedAt,
            l.ActorUserId
        )).ToList();

        // Retrieve role change history.
        var roleLogs = await _repository.GetUserRoleLogsAsync(userId, cancellationToken);
        var roleLogResponses = roleLogs.Select(l => new UserRoleLogResponse(
            l.PreviousRole.ToString(),
            l.NewRole.ToString(),
            l.Reason ?? "",
            l.CreatedAt
        )).ToList();

        return new UserDetailsResponse(
            user.Id,
            user.Email,
            user.Phone,
            user.FullName,
            user.AvatarUrl,
            user.Role.ToString(),
            user.Status.ToString(),
            user.CustomerStatus.ToString(),
            user.ShopStatus.ToString(),
            user.CreatedAt,
            user.UpdatedAt,
            auditLogResponses,
            roleLogResponses
        );
    }

    public async Task UpdateStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetAndValidateUserAsync(targetUserId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.IsCustomerProfile)
        {
            var newCustomerStatus = request.IsSuspended ? CustomerStatus.Suspended : CustomerStatus.Active;
            if (user.CustomerStatus == newCustomerStatus)
                throw AppException.Conflict($"Customer profile is already {newCustomerStatus.ToString().ToLower()}.");

            user.CustomerStatus = newCustomerStatus;
            LogAudit(adminId, targetUserId, request.IsSuspended ? "SuspendCustomer" : "ReactivateCustomer", request.Reason, $"{{\"CustomerStatus\":\"{newCustomerStatus}\"}}", now);

            if (request.IsSuspended)
                await _refreshTokenService.RevokeAllAsync(targetUserId, "Customer profile suspended by Administrator.", cancellationToken);

            _logger.LogInformation("Admin {AdminId} {Action} customer profile for {UserId}.", adminId, request.IsSuspended ? "suspended" : "reactivated", targetUserId);
        }
        else
        {
            var newAccountStatus = request.IsSuspended ? UserStatus.Suspended : UserStatus.Active;
            if (user.Status == newAccountStatus)
                throw AppException.Conflict($"User account is already {newAccountStatus.ToString().ToLower()}.");

            user.Status = newAccountStatus;
            LogAccountStatusChange(adminId, targetUserId, newAccountStatus, request.Reason, now);
            LogAudit(adminId, targetUserId, request.IsSuspended ? "SuspendAccount" : "ReactivateAccount", request.Reason, $"{{\"Status\":\"{newAccountStatus}\"}}", now);

            if (request.IsSuspended)
                await _refreshTokenService.RevokeAllAsync(targetUserId, "Account suspended by Administrator.", cancellationToken);

            _logger.LogInformation("Admin {AdminId} {Action} account {UserId}.", adminId, request.IsSuspended ? "suspended" : "reactivated", targetUserId);
        }

        user.UpdatedAt = now;
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetAndValidateUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (user == null) throw AppException.NotFound("User not found.");

        if (user.Role == UserRole.Admin)
            throw AppException.Forbidden("Cannot modify Administrator accounts.");

        return user;
    }

    private void LogAccountStatusChange(Guid adminId, Guid userId, UserStatus action, string reason, DateTime now)
    {
        _repository.AddAccountStatusLog(new AccountStatusLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AdminId = adminId,
            Action = action,
            Reason = reason,
            CreatedAt = now
        });
    }

    private void LogAudit(Guid adminId, Guid targetId, string action, string reason, string newValues, DateTime now)
    {
        _repository.AddAuditLog(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = adminId,
            ActorType = AuditActorType.User,
            Action = action,
            TargetType = "User",
            TargetId = targetId,
            Reason = reason,
            NewValuesJson = newValues,
            CreatedAt = now
        });
    }

    public async Task<CurrentUserResponse> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        var fullName = request.FullName.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        user.FullName = fullName;
        user.Phone = phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

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

        await _repository.SaveChangesAsync(cancellationToken);

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
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);
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

    // Map the entity to its response model.
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
