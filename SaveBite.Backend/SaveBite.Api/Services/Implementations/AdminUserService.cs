using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
using System.Linq;

namespace SaveBite.Backend.Services.Implementations;

public sealed class AdminUserService : IAdminUserService
{
    private readonly IAdminUserRepository _repository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IAdminUserRepository repository,
        IRefreshTokenService refreshTokenService,
        TimeProvider timeProvider,
        ILogger<AdminUserService> logger)
    {
        _repository = repository;
        _refreshTokenService = refreshTokenService;
        _timeProvider = timeProvider;
        _logger = logger;
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

        // Histort Log AuditLog
        var logs = await _repository.GetUserAuditLogsAsync(userId, cancellationToken);

        var logResponses = logs.Select(l => new UserLogResponse(
            l.Action,
            l.Reason ?? "",
            l.NewValuesJson ?? "",
            l.CreatedAt,
            l.ActorUserId
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
            logResponses
        );
    }

    public async Task UpdateStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetAndValidateUserAsync(targetUserId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.IsCustomerProfile)
        {
            // Customer Status
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
            // Account Status
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
}