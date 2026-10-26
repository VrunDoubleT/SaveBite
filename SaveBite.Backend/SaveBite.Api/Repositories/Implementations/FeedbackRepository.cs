using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _context;

    public FeedbackRepository(AppDbContext context)
    {
        _context = context;
    }

    // Load feedback with the order, shop, product image and shop reply needed for the response.
    private IQueryable<ProductFeedback> FeedbackWithDetails => _context.ProductFeedbacks
        .Include(f => f.OrderItem).ThenInclude(oi => oi.Order).ThenInclude(o => o.Shop)
        .Include(f => f.Product).ThenInclude(p => p.Images)
        .Include(f => f.Reply)
        .AsSplitQuery();

    public async Task<OrderItem?> GetOrderItemWithOrderAsync(Guid orderItemId, CancellationToken ct = default)
    {
        return await _context.OrderItems
            .Include(oi => oi.Order).ThenInclude(o => o.Shop)
            .Include(oi => oi.FlashDealVariant)
                .ThenInclude(fdv => fdv.Variant)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId, ct);
    }

    public async Task<bool> HasFeedbackForOrderItemAsync(Guid orderItemId, CancellationToken ct = default)
    {
        return await _context.ProductFeedbacks
            .AnyAsync(f => f.OrderItemId == orderItemId, ct);
    }

    public async Task<ProductFeedback?> GetByIdAsync(Guid feedbackId, CancellationToken ct = default)
    {
        return await FeedbackWithDetails
            .FirstOrDefaultAsync(f => f.Id == feedbackId, ct);
    }

    public async Task<ProductFeedback> CreateAsync(ProductFeedback feedback, CancellationToken ct = default)
    {
        _context.ProductFeedbacks.Add(feedback);
        await _context.SaveChangesAsync(ct);
        return await GetByIdAsync(feedback.Id, ct) ?? feedback;
    }

    public async Task UpdateAsync(ProductFeedback feedback, CancellationToken ct = default)
    {
        _context.ProductFeedbacks.Update(feedback);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ProductFeedback>> GetCustomerFeedbackHistoryAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var baseQuery = _context.ProductFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Status == FeedbackStatus.Visible);

        var totalItems = await baseQuery.CountAsync(ct);
        var items = await FeedbackWithDetails
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Status == FeedbackStatus.Visible)
            .OrderByDescending(f => f.CreatedAt)
            .ThenByDescending(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<ProductFeedback>.Create(items, page, pageSize, totalItems);
    }
}
