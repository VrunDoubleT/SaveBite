using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

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
            user.UpdatedAt
        );
    }

    public async Task SuspendAccountAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetAndValidateUserAsync(userId, cancellationToken);
        if (user.Status == UserStatus.Suspended)
            throw AppException.Conflict("User account is already suspended.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.Status = UserStatus.Suspended;
        user.UpdatedAt = now;

        LogAccountStatusChange(adminId, userId, UserStatus.Suspended, request.Reason, now);
        LogAudit(adminId, userId, "SuspendAccount", request.Reason, "{\"Status\":\"Suspended\"}", now);

        await _repository.SaveChangesAsync(cancellationToken);

        // Revoke all tokens to immediately disconnect the user
        await _refreshTokenService.RevokeAllAsync(userId, "Account suspended by Administrator.", cancellationToken);

        _logger.LogInformation("Admin {AdminId} suspended account {UserId}.", adminId, userId);
    }

    public async Task ReactivateAccountAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetAndValidateUserAsync(userId, cancellationToken);
        if (user.Status == UserStatus.Active)
            throw AppException.Conflict("User account is already active.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.Status = UserStatus.Active;
        user.UpdatedAt = now;

        LogAccountStatusChange(adminId, userId, UserStatus.Active, request.Reason, now);
        LogAudit(adminId, userId, "ReactivateAccount", request.Reason, "{\"Status\":\"Active\"}", now);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin {AdminId} reactivated account {UserId}.", adminId, userId);
    }

    public async Task SuspendCustomerAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetAndValidateUserAsync(userId, cancellationToken);
        if (user.CustomerStatus == CustomerStatus.Suspended)
            throw AppException.Conflict("Customer profile is already suspended.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.CustomerStatus = CustomerStatus.Suspended;
        user.UpdatedAt = now;

        LogAudit(adminId, userId, "SuspendCustomer", request.Reason, "{\"CustomerStatus\":\"Suspended\"}", now);

        await _repository.SaveChangesAsync(cancellationToken);
        await _refreshTokenService.RevokeAllAsync(userId, "Customer profile suspended by Administrator.", cancellationToken);

        _logger.LogInformation("Admin {AdminId} suspended customer profile for {UserId}.", adminId, userId);
    }

    public async Task ReactivateCustomerAsync(Guid adminId, Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetAndValidateUserAsync(userId, cancellationToken);
        if (user.CustomerStatus == CustomerStatus.Active)
            throw AppException.Conflict("Customer profile is already active.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.CustomerStatus = CustomerStatus.Active;
        user.UpdatedAt = now;

        LogAudit(adminId, userId, "ReactivateCustomer", request.Reason, "{\"CustomerStatus\":\"Active\"}", now);

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Admin {AdminId} reactivated customer profile for {UserId}.", adminId, userId);
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