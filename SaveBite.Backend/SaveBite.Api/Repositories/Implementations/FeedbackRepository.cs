using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _context;

    public FeedbackRepository(AppDbContext context)
    {
        _context = context;
    }

    // Load feedback with the order, shop, product image, attached feedback images, and shop reply needed for the response.
    private IQueryable<ProductFeedback> FeedbackWithDetails => _context.ProductFeedbacks
        .Include(f => f.User)
        .Include(f => f.OrderItem).ThenInclude(oi => oi.Order).ThenInclude(o => o.Shop)
        .Include(f => f.Product).ThenInclude(p => p.Images)
        .Include(f => f.Reply).ThenInclude(r => r!.RepliedByUser)
        .Include(f => f.Images)
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

    public async Task ReplaceFeedbackImagesAsync(Guid feedbackId, IReadOnlyList<ProductFeedbackImage> newImages, CancellationToken ct = default)
    {
        var existing = await _context.ProductFeedbackImages
            .Where(i => i.ProductFeedbackId == feedbackId)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            _context.ProductFeedbackImages.RemoveRange(existing);
        }

        if (newImages.Count > 0)
        {
            await _context.ProductFeedbackImages.AddRangeAsync(newImages, ct);
        }
    }

    public async Task UpdateAsync(ProductFeedback feedback, CancellationToken ct = default)
    {
        if (_context.Entry(feedback).State == EntityState.Detached)
        {
            _context.ProductFeedbacks.Update(feedback);
        }
        await _context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ProductFeedback>> GetCustomerFeedbackHistoryAsync(Guid userId, int page, int pageSize, Guid? targetFeedbackId = null, CancellationToken ct = default)
    {
        var baseQuery = _context.ProductFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Status == FeedbackStatus.Visible);

        var totalItems = await baseQuery.CountAsync(ct);

        // If deep-linking to a specific feedback (e.g. from Order details), find its page position
        if (targetFeedbackId.HasValue && targetFeedbackId.Value != Guid.Empty)
        {
            var target = await baseQuery.FirstOrDefaultAsync(f => f.Id == targetFeedbackId.Value, ct);
            if (target != null)
            {
                var rank = await baseQuery
                    .Where(f => f.CreatedAt > target.CreatedAt || (f.CreatedAt == target.CreatedAt && f.Id > target.Id))
                    .CountAsync(ct);

                page = (rank / pageSize) + 1;
            }
        }

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

    public async Task<ProductFeedbackReply?> GetReplyByFeedbackIdAsync(Guid feedbackId, CancellationToken ct = default)
    {
        return await _context.ProductFeedbackReplies
            .Include(r => r.RepliedByUser)
            .FirstOrDefaultAsync(r => r.ProductFeedbackId == feedbackId, ct);
    }

    public async Task<ProductFeedbackReply> CreateReplyAsync(ProductFeedbackReply reply, CancellationToken ct = default)
    {
        _context.ProductFeedbackReplies.Add(reply);
        await _context.SaveChangesAsync(ct);
        return await GetReplyByFeedbackIdAsync(reply.ProductFeedbackId, ct) ?? reply;
    }

    public async Task UpdateReplyAsync(ProductFeedbackReply reply, CancellationToken ct = default)
    {
        if (_context.Entry(reply).State == EntityState.Detached)
        {
            _context.ProductFeedbackReplies.Update(reply);
        }
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteReplyAsync(ProductFeedbackReply reply, CancellationToken ct = default)
    {
        _context.ProductFeedbackReplies.Remove(reply);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Guid?> GetShopIdForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Check if owner of active shop
        var ownerShop = await _context.Shops
            .AsNoTracking()
            .Where(s => s.OwnerUserId == userId && s.Status == ShopStatus.Active)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (ownerShop.HasValue) return ownerShop.Value;

        // Check if staff of active shop
        var staffShop = await _context.ShopStaffMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == ShopStaffStatus.Active)
            .Select(m => (Guid?)m.ShopId)
            .FirstOrDefaultAsync(ct);

        return staffShop;
    }

    public async Task<bool> CanManageFeedbackReplyAsync(Guid userId, Guid feedbackId, CancellationToken ct = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;
        if (user.Role == UserRole.Admin) return true;

        var feedback = await _context.ProductFeedbacks
            .AsNoTracking()
            .Include(f => f.Product)
            .FirstOrDefaultAsync(f => f.Id == feedbackId, ct);

        if (feedback == null) return false;

        var shopId = feedback.Product.ShopId;

        var isOwner = await _context.Shops
            .AsNoTracking()
            .AnyAsync(s => s.Id == shopId && s.OwnerUserId == userId && s.Status == ShopStatus.Active, ct);
        if (isOwner) return true;

        var isStaff = await _context.ShopStaffMembers
            .AsNoTracking()
            .AnyAsync(m => m.ShopId == shopId && m.UserId == userId && m.Status == ShopStaffStatus.Active, ct);

        return isStaff;
    }

    public async Task<PagedResult<ProductFeedback>> GetShopFeedbacksAsync(Guid shopId, FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default)
    {
        var query = _context.ProductFeedbacks
            .AsNoTracking()
            .Where(f => f.Product.ShopId == shopId && f.Status == FeedbackStatus.Visible);

        if (request.Rating is >= 1 and <= 5)
        {
            query = query.Where(f => f.Rating == request.Rating.Value);
        }

        if (request.HasReply.HasValue)
        {
            query = request.HasReply.Value
                ? query.Where(f => f.Reply != null)
                : query.Where(f => f.Reply == null);
        }

        if (request.IsToday == true)
        {
            var tz = GetVietnamTimeZone();
            var nowInTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(nowInTz.Date, tz);
            var endOfDayUtc = startOfDayUtc.AddDays(1);
            query = query.Where(f => f.CreatedAt >= startOfDayUtc && f.CreatedAt < endOfDayUtc);
        }

        var totalItems = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var items = await query
            .Include(f => f.User)
            .Include(f => f.Images)
            .Include(f => f.Product).ThenInclude(p => p.Images)
            .Include(f => f.OrderItem).ThenInclude(oi => oi.Order).ThenInclude(o => o.Shop)
            .Include(f => f.Reply).ThenInclude(r => r!.RepliedByUser)
            .OrderByDescending(f => f.CreatedAt)
            .ThenByDescending(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<ProductFeedback>.Create(items, page, pageSize, totalItems);
    }

    public async Task<PagedResult<ProductFeedback>> GetAdminFeedbacksAsync(FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default)
    {
        var query = _context.ProductFeedbacks
            .AsNoTracking()
            .Where(f => f.Status == FeedbackStatus.Visible);

        if (request.ShopId.HasValue && request.ShopId.Value != Guid.Empty)
        {
            query = query.Where(f => f.Product.ShopId == request.ShopId.Value);
        }

        if (request.Rating is >= 1 and <= 5)
        {
            query = query.Where(f => f.Rating == request.Rating.Value);
        }

        if (request.HasReply.HasValue)
        {
            query = request.HasReply.Value
                ? query.Where(f => f.Reply != null)
                : query.Where(f => f.Reply == null);
        }

        if (request.IsToday == true)
        {
            var tz = GetVietnamTimeZone();
            var nowInTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(nowInTz.Date, tz);
            var endOfDayUtc = startOfDayUtc.AddDays(1);
            query = query.Where(f => f.CreatedAt >= startOfDayUtc && f.CreatedAt < endOfDayUtc);
        }

        var totalItems = await query.CountAsync(ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var items = await query
            .Include(f => f.User)
            .Include(f => f.Images)
            .Include(f => f.Product).ThenInclude(p => p.Images)
            .Include(f => f.OrderItem).ThenInclude(oi => oi.Order).ThenInclude(o => o.Shop)
            .Include(f => f.Reply).ThenInclude(r => r!.RepliedByUser)
            .OrderByDescending(f => f.CreatedAt)
            .ThenByDescending(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<ProductFeedback>.Create(items, page, pageSize, totalItems);
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "ICT", "ICT");
            }
        }
    }
}
