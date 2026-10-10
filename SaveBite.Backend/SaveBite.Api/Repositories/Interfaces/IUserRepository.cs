using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserWithAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<User>> GetPagedUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void AddAccountStatusLog(AccountStatusLog log);
    void AddAuditLog(AuditLog log);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserRoleChangeLog>> GetUserRoleLogsAsync(Guid userId, CancellationToken cancellationToken = default);
}