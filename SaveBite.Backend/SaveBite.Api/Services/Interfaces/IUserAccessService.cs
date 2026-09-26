using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Services.Interfaces;

public interface IUserAccessService
{
    Task<UserAccessDecision> AuthorizeAsync(
        Guid userId,
        AccessScope scope,
        bool hasAdminRoleClaim,
        CancellationToken cancellationToken = default);
}
