
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _db;

    public FeedbackRepository(AppDbContext db)
    {
        _db = db;
    }

    // Manage Feedbacks.
    public async Task<(IReadOnlyList<ProductFeedback> Items, int Total)> ListAsync(int page, int pageSize, FeedbackStatus? status, string? search, CancellationToken ct)
    {
        var query = _db.Set<ProductFeedback>().AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";

            query = query.Where(x => (x.Comment != null && EF.Functions.ILike(x.Comment, pattern)) ||
                                     EF.Functions.ILike(x.Product.Name, pattern) || EF.Functions.ILike(x.Product.Shop.Name, pattern));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(x => x.User)
            .Include(x => x.Product).ThenInclude(p => p.Shop)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // Manage Feedbacks.
    public async Task<ProductFeedback?> GetForUpdateAsync(Guid id, CancellationToken ct)
    {
        return await _db.Set<ProductFeedback>().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    // Manage Feedbacks.
    public async Task<string?> GetCurrentReasonAsync(Guid feedbackId, CancellationToken ct)
    {
        return await _db.Set<ProductFeedbackModerationLog>().AsNoTracking()
            .Where(x => x.ProductFeedbackId == feedbackId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => x.Reason)
            .FirstOrDefaultAsync(ct);
    }

    // Manage Feedbacks.
    public async Task<Dictionary<Guid, string?>> GetCurrentReasonsAsync(IEnumerable<Guid> feedbackIds, CancellationToken ct)
    {
        var ids = feedbackIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string?>();
        }

        var logs = await _db.Set<ProductFeedbackModerationLog>().AsNoTracking()
            .Where(x => ids.Contains(x.ProductFeedbackId))
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.ProductFeedbackId,
                x.Reason
            }).ToListAsync(ct);

        return logs.GroupBy(x => x.ProductFeedbackId).ToDictionary(
            x => x.Key,
            x => (string?)x.First().Reason);
    }

    // Manage Feedbacks.
    public async Task SaveModerationAsync(ProductFeedback feedback, ProductFeedbackModerationLog log, CancellationToken ct)
    {
        _db.Set<ProductFeedbackModerationLog>().Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
