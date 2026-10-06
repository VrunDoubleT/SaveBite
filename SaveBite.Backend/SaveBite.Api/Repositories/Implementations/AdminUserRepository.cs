using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _dbContext;

    public AdminUserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<User>> GetPagedUsersAsync(GetUsersRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(searchTerm) ||
                                     u.FullName.ToLower().Contains(searchTerm));
        }

        if (request.Role.HasValue)
            query = query.Where(u => u.Role == request.Role.Value);

        if (request.Status.HasValue)
            query = query.Where(u => u.Status == request.Status.Value);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<User>.Create(items, request.Page, request.PageSize, totalItems);
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public void AddAccountStatusLog(AccountStatusLog log)
    {
        _dbContext.Set<AccountStatusLog>().Add(log);
    }

    public void AddAuditLog(AuditLog log)
    {
        _dbContext.Set<AuditLog>().Add(log);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}