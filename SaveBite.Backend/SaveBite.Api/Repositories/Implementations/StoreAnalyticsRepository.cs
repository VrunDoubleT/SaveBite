using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class StoreAnalyticsRepository : IStoreAnalyticsRepository
{
    private readonly AppDbContext _db;

    public StoreAnalyticsRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Shop?> GetOwnedShopAsync(Guid ownerUserId, CancellationToken ct = default)
        => _db.Shops
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerUserId == ownerUserId, ct);

    public Task<IReadOnlyList<CompletedOrderFinancialRow>> GetCompletedOrderFinancialsAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default)
        => CompletedOrderFinancialsAsync(shopId, period, ct);

    public Task<IReadOnlyList<OrderStatusCountRow>> GetOrderStatusCountsAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default)
        => OrderStatusCountsAsync(shopId, period, ct);

    public Task<DealStateCountsRow> GetDealStateCountsAsync(
        Guid shopId, AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct = default)
        => DealStateCountsAsync(shopId, period, nowUtc, ct);

    public Task<DealSalesTotalsRow> GetDealSalesTotalsAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default)
        => DealSalesTotalsAsync(shopId, period, ct);

    public Task<IReadOnlyList<TopDealRow>> GetTopDealsAsync(
        Guid shopId, AnalyticsPeriod period, int take, CancellationToken ct = default)
        => TopDealsAsync(shopId, period, take, ct);

    public Task<IReadOnlyList<PaymentMethodAmountRow>> GetSucceededPaymentsByMethodAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default)
        => SucceededPaymentsByMethodAsync(shopId, period, ct);


    // ---- Shared query building blocks (kept identical in the store and platform repositories) ----

    /// <summary>Orders that are visible to shops (submitted checkouts only) so store and platform counts agree.</summary>
    private IQueryable<Order> VisibleOrders(Guid? shopId)
    {
        var query = ShopOrderVisibility.Apply(_db.Orders.AsNoTracking());
        if (!shopId.HasValue) return query;
        var id = shopId.Value;
        return query.Where(o => o.ShopId == id);
    }

    private IQueryable<Order> CompletedOrders(Guid? shopId, AnalyticsPeriod period)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var query = _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed
                        && (o.CompletedAt ?? o.UpdatedAt) >= from
                        && (o.CompletedAt ?? o.UpdatedAt) < to);
        if (!shopId.HasValue) return query;
        var id = shopId.Value;
        return query.Where(o => o.ShopId == id);
    }

    /// <summary>
    /// BR-114: paid = succeeded payments actually collected for the Order (no fallback to the order total);
    /// refunded = Transferred/Completed refunds of the same Order; fee = BR-093 fee plus its refund adjustments.
    /// </summary>
    private async Task<IReadOnlyList<CompletedOrderFinancialRow>> CompletedOrderFinancialsAsync(
        Guid? shopId, AnalyticsPeriod period, CancellationToken ct)
    {
        var orders = CompletedOrders(shopId, period);

        var rows = await orders
            .Select(o => new
            {
                o.Id,
                o.ShopId,
                o.TotalAmount,
                CompletedAt = o.CompletedAt ?? o.UpdatedAt,
                // Online = payments taken through the gateway; cash = money handled at the shop (Cash or PayAtShop).
                PaidOnline = o.Payments
                    .Where(p => p.Status == PaymentStatus.Succeeded && p.Method == PaymentMethod.OnlinePayment)
                    .Sum(p => p.CollectedAmount > p.Amount ? p.CollectedAmount : p.Amount),
                PaidCash = o.Payments
                    .Where(p => p.Status == PaymentStatus.Succeeded && p.Method != PaymentMethod.OnlinePayment)
                    .Sum(p => p.CollectedAmount > p.Amount ? p.CollectedAmount : p.Amount),
                Refunded = o.Refunds
                    .Where(r => r.Status == RefundStatus.Transferred || r.Status == RefundStatus.Completed)
                    .Sum(r => r.TransferredAmount > 0m ? r.TransferredAmount : r.RequestedAmount)
            })
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.Id);
        var fees = await _db.OrderPlatformFees.AsNoTracking()
            .Where(f => orderIds.Contains(f.OrderId))
            .Select(f => new { f.OrderId, Fee = f.FeeAmount + f.Adjustments.Sum(a => a.Amount) })
            .ToListAsync(ct);
        var feeByOrder = fees.GroupBy(f => f.OrderId).ToDictionary(g => g.Key, g => g.Sum(f => f.Fee));

        return rows
            .Select(r => new CompletedOrderFinancialRow(
                r.Id, r.ShopId, r.TotalAmount, r.PaidOnline, r.PaidCash, r.Refunded,
                feeByOrder.GetValueOrDefault(r.Id), r.CompletedAt))
            .ToList();
    }

    /// <summary>BR-115: visible orders created in the period grouped by their recorded status.</summary>
    private async Task<IReadOnlyList<OrderStatusCountRow>> OrderStatusCountsAsync(
        Guid? shopId, AnalyticsPeriod period, CancellationToken ct)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;

        var rows = await VisibleOrders(shopId)
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to)
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Late = g.Count(o => o.IsLateArrival),
                Total = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync(ct);

        return rows.Select(r => new OrderStatusCountRow(r.Status, r.Count, r.Late, r.Total)).ToList();
    }

    private IQueryable<Payment> PaymentsInPeriod(Guid? shopId, AnalyticsPeriod period)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var query = _db.Payments.AsNoTracking()
            .Where(p => (p.PaidAt ?? p.CreatedAt) >= from && (p.PaidAt ?? p.CreatedAt) < to);
        if (!shopId.HasValue) return query;
        var id = shopId.Value;
        return query.Where(p => p.Order.ShopId == id);
    }

    private async Task<IReadOnlyList<PaymentMethodAmountRow>> SucceededPaymentsByMethodAsync(
        Guid? shopId, AnalyticsPeriod period, CancellationToken ct)
    {
        var rows = await PaymentsInPeriod(shopId, period)
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .GroupBy(p => p.Method)
            .Select(g => new
            {
                Method = g.Key,
                Count = g.Count(),
                Amount = g.Sum(p => p.CollectedAmount > p.Amount ? p.CollectedAmount : p.Amount)
            })
            .ToListAsync(ct);

        return rows.Select(r => new PaymentMethodAmountRow(r.Method, r.Count, r.Amount)).ToList();
    }

    private IQueryable<FlashDeal> Deals(Guid? shopId)
    {
        var query = _db.FlashDeals.AsNoTracking();
        if (!shopId.HasValue) return query;
        var id = shopId.Value;
        return query.Where(d => d.ShopId == id);
    }

    private IQueryable<OrderItem> CompletedItems(Guid? shopId, AnalyticsPeriod period)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var query = _db.OrderItems.AsNoTracking()
            .Where(i => i.Order.Status == OrderStatus.Completed
                        && (i.Order.CompletedAt ?? i.Order.UpdatedAt) >= from
                        && (i.Order.CompletedAt ?? i.Order.UpdatedAt) < to);
        if (!shopId.HasValue) return query;
        var id = shopId.Value;
        return query.Where(i => i.Order.ShopId == id);
    }

    /// <summary>
    /// BR-116 straight from the Deal's own status (no history table, no schema change):
    ///  - Created + StatusBreakdown: Deals created in the period, grouped by FlashDealStatus.
    ///  - OnSaleNow / PendingNow / HiddenNow: live counts.
    ///  - CancelledEarly: cancelled by the owner during the period (the cancel stamps UpdatedAt and the status is final).
    ///  - EndedOnSchedule: the sale window ran out during the period without a cancel.
    ///  - SoldOut: Deals created in the period whose every variant has sold its full allocation
    ///    (FlashDealStatus.SoldOut is never persisted by the app).
    /// </summary>
    private async Task<DealStateCountsRow> DealStateCountsAsync(
        Guid? shopId, AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var deals = Deals(shopId);

        var cohort = deals.Where(d => d.CreatedAt >= from && d.CreatedAt < to);
        var created = await cohort.CountAsync(ct);

        var statusRows = await cohort
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var onSaleNow = await deals.CountAsync(
            d => d.Status == FlashDealStatus.OnSale && d.SaleStartTime <= nowUtc && d.OrderEndTime >= nowUtc, ct);
        var pendingNow = await deals.CountAsync(
            d => d.Status == FlashDealStatus.Pending && d.OrderEndTime >= nowUtc, ct);
        var hiddenNow = await deals.CountAsync(d => d.Status == FlashDealStatus.Hidden, ct);

        var cancelledEarly = await deals.CountAsync(
            d => d.Status == FlashDealStatus.Cancelled && d.UpdatedAt >= from && d.UpdatedAt < to, ct);
        var endedOnSchedule = await deals.CountAsync(
            d => d.Status != FlashDealStatus.Cancelled
                 && d.OrderEndTime >= from && d.OrderEndTime < to && d.OrderEndTime < nowUtc, ct);

        var soldOut = await cohort.CountAsync(
            d => d.Status != FlashDealStatus.Cancelled
                 && d.Variants.Any()
                 && d.Variants.All(v => v.SoldQuantity >= v.TotalQuantity), ct);

        // Allocation and counters of the same cohort, so sell-through compares like with like.
        var variants = cohort.SelectMany(d => d.Variants);
        var allocated = await variants.SumAsync(v => v.TotalQuantity, ct);
        var sold = await variants.SumAsync(v => v.SoldQuantity, ct);
        var reserved = await variants.SumAsync(v => v.ReservedQuantity, ct);

        // Orders that actually sold deal items in the period (completed orders only, BR-116).
        var ordersWithDeals = await CompletedItems(shopId, period)
            .Select(i => i.OrderId)
            .Distinct()
            .CountAsync(ct);

        return new DealStateCountsRow(
            created, onSaleNow, pendingNow, hiddenNow, cancelledEarly, endedOnSchedule, soldOut,
            allocated, sold, reserved, ordersWithDeals,
            statusRows.Select(r => new DealStatusCountRow(r.Status, r.Count)).ToList());
    }

    /// <summary>BR-116: deal sales and quantity sold are counted from actual completed order items.</summary>
    private async Task<DealSalesTotalsRow> DealSalesTotalsAsync(
        Guid? shopId, AnalyticsPeriod period, CancellationToken ct)
    {
        var items = CompletedItems(shopId, period);
        var quantity = await items.SumAsync(i => i.Quantity, ct);
        var revenue = await items.SumAsync(i => i.Subtotal ?? i.UnitPrice * i.Quantity, ct);
        var savings = await items.SumAsync(i => (i.OriginalPriceSnapshot - i.DealPriceSnapshot) * i.Quantity, ct);
        return new DealSalesTotalsRow(quantity, revenue, savings);
    }

    private async Task<IReadOnlyList<TopDealRow>> TopDealsAsync(
        Guid shopId, AnalyticsPeriod period, int take, CancellationToken ct)
    {
        var top = await CompletedItems(shopId, period)
            .GroupBy(i => i.FlashDealVariant.FlashDealId)
            .Select(g => new
            {
                DealId = g.Key,
                Quantity = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Subtotal ?? i.UnitPrice * i.Quantity)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.Quantity)
            .Take(take)
            .ToListAsync(ct);

        if (top.Count == 0) return Array.Empty<TopDealRow>();

        var ids = top.Select(x => x.DealId).ToList();
        var dealInfo = await _db.FlashDeals.AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .Select(d => new
            {
                d.Id,
                ProductName = d.Product.Name,
                d.Status,
                d.SaleStartTime,
                d.OrderEndTime,
                Allocated = d.Variants.Sum(v => v.TotalQuantity)
            })
            .ToListAsync(ct);
        var infoById = dealInfo.ToDictionary(d => d.Id);

        // Deals that earned revenue in the period are listed even if they were created or ended outside it.
        return top
            .Where(x => infoById.ContainsKey(x.DealId))
            .Select(x =>
            {
                var info = infoById[x.DealId];
                return new TopDealRow(x.DealId, info.ProductName, info.Status, info.Allocated,
                    info.SaleStartTime, info.OrderEndTime, x.Quantity, x.Revenue);
            })
            .ToList();
    }

}
