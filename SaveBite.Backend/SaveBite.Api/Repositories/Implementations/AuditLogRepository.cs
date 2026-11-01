using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLog>> GetPagedLogsAsync(
        GetAuditLogsRequest request,
        DateTime? fromUtc,
        DateTime? toUtcExclusive,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogs
            .AsNoTracking()
            .Include(x => x.ActorUser)
            .AsQueryable();

        if (request.ActorType.HasValue)
        {
            query = query.Where(x => x.ActorType == request.ActorType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TargetType))
        {
            var targetType = request.TargetType.Trim();
            query = query.Where(x => x.TargetType == targetType);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            var action = request.Action.Trim();
            query = query.Where(x => x.Action == action);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        }

        if (toUtcExclusive.HasValue)
        {
            query = query.Where(x => x.CreatedAt < toUtcExclusive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var isGuid = Guid.TryParse(term, out var parsedGuid);

            query = query.Where(x =>
                x.Action.ToLower().Contains(term) ||
                x.TargetType.ToLower().Contains(term) ||
                (x.Reason != null && x.Reason.ToLower().Contains(term)) ||
                (x.ActorUser != null && (
                    x.ActorUser.FullName.ToLower().Contains(term) ||
                    x.ActorUser.Email.ToLower().Contains(term))) ||
                (isGuid && (x.TargetId == parsedGuid || x.ActorUserId == parsedGuid || x.CorrelationId == parsedGuid)));
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var totalItems = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<AuditLog>.Create(items, page, pageSize, totalItems);
    }

    public Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.AuditLogs
            .AsNoTracking()
            .Include(x => x.ActorUser)
            .SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<(IReadOnlyList<string> Actions, IReadOnlyList<string> TargetTypes)> GetDistinctFiltersAsync(
        CancellationToken ct = default)
    {
        var actions = await _db.AuditLogs
            .AsNoTracking()
            .Select(x => x.Action)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        var targetTypes = await _db.AuditLogs
            .AsNoTracking()
            .Select(x => x.TargetType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        return (actions, targetTypes);
    }
}