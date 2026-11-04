using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.BackgroundJobs.Jobs;
using SaveBite.Backend.BackgroundJobs.Scheduling;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

public sealed record ResaleItemResponse(Guid OrderItemId, string ProductName, int Quantity, string Status,
    int ResaleQuantity, int UnsellableQuantity, DateTime? DecidedAt, Guid? DecidedBy);
public sealed record ResaleDecisionResponse(Guid OrderId, DateTime? DeadlineAt, bool CanDecide, IReadOnlyList<ResaleItemResponse> Items);

public sealed class OrderResaleService(AppDbContext db, ICheckoutRepository orders, IShopOrderRepository shops,
    IBackgroundJobClient jobs, TimeProvider clock, ILogger<OrderResaleService> logger)
{
    // Caller owns the order transaction; stock stays allocated until an explicit valid decision.
    public async Task BeginAsync(Order order, CancellationToken ct)
    {
        if (order.ResaleDecisionDeadlineAt.HasValue || order.Items.Count == 0) return;
        if (order.InventoryReservationActive) await orders.CompleteInventoryAsync(order, true, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var deadline = now.AddMinutes(5);
        order.ResaleDecisionDeadlineAt = deadline;
        foreach (var item in order.Items) item.ResaleDecisionStatus = ResaleDecisionStatus.Pending;
        order.ResaleDecisionJobId = SafeSchedule.Try(() => jobs.Schedule<ResaleDecisionExpirationJob>(
            job => job.ExecuteAsync(order.Id, CancellationToken.None), new DateTimeOffset(deadline)), logger, "resale-decision", order.Id);
    }

    public async Task<ResaleDecisionResponse> GetAsync(Guid actor, Guid shopId, Guid orderId, CancellationToken ct)
    {
        await shops.RequireAccessAsync(actor, shopId, ct);
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId && o.ShopId == shopId, ct)
            ?? throw AppException.NotFound();
        return Map(order);
    }

    public async Task<ResaleDecisionResponse> DecideAsync(Guid actor, Guid shopId, Guid orderId, ResaleDecisionRequest request, CancellationToken ct)
    {
        if (request.Items == null || request.Items.Count is < 1 or > 100 || request.Items.Select(i => i.OrderItemId).Distinct().Count() != request.Items.Count)
            throw AppException.BadRequest("Select distinct order items (1 to 100).");
        var expired = false;
        var result = await orders.WithOrderLockAsync(orderId, async () =>
        {
            await shops.RequireAccessAsync(actor, shopId, ct);
            var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId && o.ShopId == shopId, ct)
                ?? throw AppException.NotFound();
            if (order.Status is not (OrderStatus.Cancelled or OrderStatus.NoShow) || !order.ResaleDecisionDeadlineAt.HasValue)
                throw AppException.Conflict("This order has no resale decision window.");
            if (request.Items.All(c => order.Items.Any(i => i.Id == c.OrderItemId && i.ResaleDecisionStatus == ResaleDecisionStatus.Decided && i.ResaleQuantity == c.ResaleQuantity)))
                return Map(order); // Replays of a committed decision stay successful even after its deadline.
            var now = clock.GetUtcNow().UtcDateTime;
            if (order.ResaleDecisionDeadlineAt <= now && order.Items.Any(i => i.ResaleDecisionStatus == ResaleDecisionStatus.Pending))
            {
                Expire(order, now); expired = true;
                await db.SaveChangesAsync(ct);
                return Map(order);
            }
            foreach (var choice in request.Items.OrderBy(i => i.OrderItemId))
            {
                var item = order.Items.SingleOrDefault(i => i.Id == choice.OrderItemId) ?? throw AppException.BadRequest("Item does not belong to this order.");
                if (choice.ResaleQuantity < 0 || choice.ResaleQuantity > item.Quantity) throw AppException.BadRequest("Invalid resale quantity.");
                if (item.ResaleDecisionStatus == ResaleDecisionStatus.Decided && item.ResaleQuantity == choice.ResaleQuantity) continue;
                if (item.ResaleDecisionStatus != ResaleDecisionStatus.Pending) throw AppException.Conflict("This item has already been decided or expired.");
            }
            var variantIds = order.Items.Where(i => request.Items.Any(c => c.OrderItemId == i.Id && c.ResaleQuantity > 0)
                && i.ResaleDecisionStatus == ResaleDecisionStatus.Pending).Select(i => i.FlashDealVariantId).Distinct().ToArray();
            var dealIds = await db.FlashDealVariants.Where(v => variantIds.Contains(v.Id)).Select(v => v.FlashDealId).Distinct().OrderBy(id => id).ToListAsync(ct);
            foreach (var id in dealIds) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {id} FOR UPDATE", ct);
            foreach (var id in variantIds.OrderBy(id => id)) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deal_variants WHERE id = {id} FOR UPDATE", ct);
            now = clock.GetUtcNow().UtcDateTime;
            if (order.ResaleDecisionDeadlineAt <= now)
            {
                Expire(order, now); expired = true;
                await db.SaveChangesAsync(ct);
                return Map(order);
            }
            foreach (var choice in request.Items)
            {
                var item = order.Items.Single(i => i.Id == choice.OrderItemId);
                if (item.ResaleDecisionStatus != ResaleDecisionStatus.Pending) continue;
                if (choice.ResaleQuantity > 0)
                {
                    var variant = await db.FlashDealVariants.AsNoTracking().Include(v => v.FlashDeal).ThenInclude(d => d.Product)
                        .Include(v => v.Variant).SingleAsync(v => v.Id == item.FlashDealVariantId, ct);
                    var deal = variant.FlashDeal;
                    if (deal.Status is not (FlashDealStatus.OnSale or FlashDealStatus.SoldOut)
                        || now < deal.SaleStartTime || now >= deal.OrderEndTime || now >= deal.ShopClosingTime
                        || deal.Product.Status != ProductStatus.Active || variant.Status != FlashDealVariantStatus.Active || !variant.Variant.IsActive)
                        throw AppException.Conflict("This item cannot return to sale: the deal is unavailable or expired.");
                    var changed = await db.FlashDealVariants.Where(v => v.Id == item.FlashDealVariantId && v.SoldQuantity >= choice.ResaleQuantity)
                        .ExecuteUpdateAsync(set => set.SetProperty(v => v.SoldQuantity, v => v.SoldQuantity - choice.ResaleQuantity), ct);
                    if (changed != 1) throw AppException.Conflict("Allocated stock is inconsistent; review this order.");
                }
                item.ResaleQuantity = choice.ResaleQuantity;
                item.UnsellableQuantity = item.Quantity - choice.ResaleQuantity;
                item.ResaleDecisionStatus = ResaleDecisionStatus.Decided;
                item.ResaleDecidedAt = now; item.ResaleDecidedBy = actor;
            }
            if (variantIds.Length > 0) await LifecycleOutbox.InventoryAsync(db, variantIds, order.Id, now, ct);
            await db.SaveChangesAsync(ct);
            return Map(order);
        }, ct);
        if (expired) throw AppException.Conflict("The five-minute window expired; pending quantities were marked unsellable.");
        return result;
    }

    public Task ExpireAsync(Guid orderId, CancellationToken ct)
        => orders.WithOrderLockAsync(orderId, async () =>
        {
            var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == orderId, ct);
            var now = clock.GetUtcNow().UtcDateTime;
            if (order == null || order.ResaleDecisionDeadlineAt == null || order.ResaleDecisionDeadlineAt > now) return false;
            Expire(order, now);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);

    private static void Expire(Order order, DateTime now)
    {
        foreach (var item in order.Items.Where(i => i.ResaleDecisionStatus == ResaleDecisionStatus.Pending))
        {
            item.ResaleDecisionStatus = ResaleDecisionStatus.Expired;
            item.UnsellableQuantity = item.Quantity; item.ResaleQuantity = 0;
            item.ResaleDecidedAt = now; item.ResaleDecidedBy = null;
        }
    }
    private ResaleDecisionResponse Map(Order order) => new(order.Id, order.ResaleDecisionDeadlineAt,
        order.ResaleDecisionDeadlineAt > clock.GetUtcNow().UtcDateTime && order.Items.Any(i => i.ResaleDecisionStatus == ResaleDecisionStatus.Pending),
        order.Items.Select(i => new ResaleItemResponse(i.Id, i.ProductNameSnapshot, i.Quantity, i.ResaleDecisionStatus.ToString(),
            i.ResaleQuantity, i.UnsellableQuantity, i.ResaleDecidedAt, i.ResaleDecidedBy)).ToArray());
}
