using SaveBite.Backend.Models.DTOs;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IUserAccessRepository
{
    Task<UserAccessState?> GetAccessStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasStoreOwnerAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasStaffAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasStoreOwnerOrStaffAccessAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
