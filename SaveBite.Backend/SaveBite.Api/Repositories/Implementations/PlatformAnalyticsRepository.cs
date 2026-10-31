using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class PlatformAnalyticsRepository : IPlatformAnalyticsRepository
{
    private readonly AppDbContext _db;

    public PlatformAnalyticsRepository(AppDbContext db)
    {
        _db = db;
    }

    // ------------------------------------------------------------------------------------------ UC-139

    public Task<PlatformFeeConfig?> GetCurrentFeeConfigAsync(DateTime nowUtc, CancellationToken ct = default)
        => _db.PlatformFeeConfigs
            .AsNoTracking()
            .Where(x => x.EffectiveFrom <= nowUtc && (x.EffectiveTo == null || x.EffectiveTo > nowUtc))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Fees calculated in the period (BR-093) and the refund adjustments attached to those same fees, so gross, adjustments
    /// and net always describe one consistent set of orders.
    /// </summary>
    public async Task<FeeAccrualRow> GetFeeAccrualAsync(AnalyticsPeriod period, CancellationToken ct = default)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var fees = _db.OrderPlatformFees.AsNoTracking().Where(f => f.CalculatedAt >= from && f.CalculatedAt < to);

        var count = await fees.CountAsync(ct);
        var gross = await fees.SumAsync(f => f.FeeAmount, ct);
        var adjustments = await fees.SelectMany(f => f.Adjustments).SumAsync(a => a.Amount, ct);
        return new FeeAccrualRow(count, gross, adjustments);
    }

    public async Task<FeeStatementSummaryRow> GetFeeStatementSummaryAsync(AnalyticsPeriod period, CancellationToken ct = default)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var startKey = period.StartDate.Year * 12 + period.StartDate.Month;
        var endKey = period.EndDate.Year * 12 + period.EndDate.Month;

        var statements = _db.PlatformFeeStatements.AsNoTracking();

        // Settlement received during the period.
        var paidInPeriod = await statements
            .Where(s => s.Status == PlatformFeeStatementStatus.Paid && s.PaidAt >= from && s.PaidAt < to)
            .SumAsync(s => s.TotalFeeAmount, ct);

        // Receivable balance: all statements still unpaid, regardless of billing month.
        var outstanding = await statements
            .Where(s => s.Status == PlatformFeeStatementStatus.Issued || s.Status == PlatformFeeStatementStatus.Overdue)
            .SumAsync(s => s.TotalFeeAmount, ct);

        var inRange = statements.Where(s => s.BillingYear * 12 + s.BillingMonth >= startKey
                                            && s.BillingYear * 12 + s.BillingMonth <= endKey);

        var breakdown = await inRange
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(s => s.TotalFeeAmount) })
            .ToListAsync(ct);

        var recent = await inRange
            .OrderByDescending(s => s.BillingYear)
            .ThenByDescending(s => s.BillingMonth)
            .ThenByDescending(s => s.UpdatedAt)
            .Take(10)
            .Select(s => new
            {
                s.Id,
                s.ShopId,
                ShopName = s.Shop.Name,
                s.BillingYear,
                s.BillingMonth,
                s.Status,
                s.TotalFeeAmount,
                s.IssuedAt,
                s.DueAt,
                s.PaidAt
            })
            .ToListAsync(ct);

        return new FeeStatementSummaryRow(
            paidInPeriod,
            outstanding,
            breakdown.Select(b => new StatementStatusRow(b.Status, b.Count, b.Amount)).ToList(),
            recent.Select(r => new FeeStatementRow(
                r.Id, r.ShopId, r.ShopName, r.BillingYear, r.BillingMonth, r.Status,
                r.TotalFeeAmount, r.IssuedAt, r.DueAt, r.PaidAt)).ToList());
    }

    // ------------------------------------------------------------------------------------------ UC-140

    public async Task<UserCountsRow> GetUserCountsAsync(AnalyticsPeriod period, CancellationToken ct = default)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var users = _db.Users.AsNoTracking();

        var total = await users.CountAsync(ct);
        var verified = await users.CountAsync(u => u.EmailVerifiedAt != null, ct);
        var activeAccounts = await users.CountAsync(u => u.Status == UserStatus.Active, ct);
        var suspendedAccounts = await users.CountAsync(u => u.Status == UserStatus.Suspended, ct);
        var activeCustomers = await users.CountAsync(u => u.CustomerStatus == CustomerStatus.Active, ct);
        var suspendedCustomers = await users.CountAsync(u => u.CustomerStatus == CustomerStatus.Suspended, ct);

        var newUsers = await users
            .Where(u => u.CreatedAt >= from && u.CreatedAt < to)
            .Select(u => u.CreatedAt)
            .ToListAsync(ct);

        var roles = await users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var trust = await users
            .Where(u => u.TrustScore != null)
            .GroupBy(u => u.TrustScore!.TrustLevel.Name)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var activeBuyers = await ActiveBuyersAsync(period, ct);

        return new UserCountsRow(
            total, newUsers.Count, verified, activeAccounts, suspendedAccounts, activeCustomers, suspendedCustomers,
            activeBuyers,
            roles.Select(r => new KeyValuePair<UserRole, int>(r.Role, r.Count)).ToList(),
            trust.OrderByDescending(t => t.Count).Select(t => new KeyValuePair<string, int>(t.Level, t.Count)).ToList(),
            trust.Sum(t => t.Count),
            newUsers);
    }

    // ------------------------------------------------------------------------------------------ UC-141

    public async Task<ShopCountsRow> GetShopCountsAsync(AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct = default)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var shops = _db.Shops.AsNoTracking();

        var total = await shops.CountAsync(ct);
        var newShops = await shops.CountAsync(s => s.CreatedAt >= from && s.CreatedAt < to, ct);
        var newApplications = await _db.ShopApplications.AsNoTracking()
            .CountAsync(a => a.CreatedAt >= from && a.CreatedAt < to, ct);

        var shopStatuses = await shops
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var applicationStatuses = await _db.ShopApplications.AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var withActiveDeals = await _db.FlashDeals.AsNoTracking()
            .Where(d => d.Status == FlashDealStatus.OnSale && d.SaleStartTime <= nowUtc && d.OrderEndTime >= nowUtc)
            .Select(d => d.ShopId)
            .Distinct()
            .CountAsync(ct);

        var withCompletedOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed
                        && (o.CompletedAt ?? o.UpdatedAt) >= from
                        && (o.CompletedAt ?? o.UpdatedAt) < to)
            .Select(o => o.ShopId)
            .Distinct()
            .CountAsync(ct);

        return new ShopCountsRow(
            total, newShops, newApplications, withActiveDeals, withCompletedOrders,
            shopStatuses.Select(s => new KeyValuePair<ShopStatus, int>(s.Status, s.Count)).ToList(),
            applicationStatuses.Select(a => new KeyValuePair<ShopApplicationStatus, int>(a.Status, a.Count)).ToList());
    }

    // ------------------------------------------------------------------------------------------ UC-142

    public Task<IReadOnlyList<CompletedOrderFinancialRow>> GetCompletedOrderFinancialsAsync(
        AnalyticsPeriod period, CancellationToken ct = default)
        => CompletedOrderFinancialsAsync(null, period, ct);

    public Task<IReadOnlyList<OrderStatusCountRow>> GetOrderStatusCountsAsync(
        AnalyticsPeriod period, CancellationToken ct = default)
        => OrderStatusCountsAsync(null, period, ct);

    // ------------------------------------------------------------------------------------------ UC-143

    public Task<DealStateCountsRow> GetDealStateCountsAsync(
        AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct = default)
        => DealStateCountsAsync(null, period, nowUtc, ct);

    public Task<DealSalesTotalsRow> GetDealSalesTotalsAsync(AnalyticsPeriod period, CancellationToken ct = default)
        => DealSalesTotalsAsync(null, period, ct);

    // ------------------------------------------------------------------------------------------ UC-144

    public Task<IReadOnlyList<PaymentStatusRow>> GetPaymentStatusRowsAsync(AnalyticsPeriod period, CancellationToken ct = default)
        => PaymentStatusRowsAsync(period, ct);

    public Task<IReadOnlyList<PaymentMethodAmountRow>> GetSucceededPaymentsByMethodAsync(
        AnalyticsPeriod period, CancellationToken ct = default)
        => SucceededPaymentsByMethodAsync(null, period, ct);

    public async Task<IReadOnlyList<RefundStatusRow>> GetRefundStatusRowsAsync(AnalyticsPeriod period, CancellationToken ct = default)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var rows = await _db.Refunds.AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(r => r.RequestedAmount) })
            .ToListAsync(ct);
        return rows.Select(r => new RefundStatusRow(r.Status, r.Count, r.Amount)).ToList();
    }

    public async Task<IReadOnlyList<RefundTypeRow>> GetRefundTypeRowsAsync(AnalyticsPeriod period, CancellationToken ct = default)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        var rows = await _db.Refunds.AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt < to)
            .GroupBy(r => r.RefundType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Amount = g.Sum(r => r.RequestedAmount) })
            .ToListAsync(ct);
        return rows.Select(r => new RefundTypeRow(r.Type, r.Count, r.Amount)).ToList();
    }

    /// <summary>Money actually transferred back to customers during the period (by transfer date).</summary>
    public Task<decimal> GetTransferredRefundAmountAsync(AnalyticsPeriod period, CancellationToken ct = default)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        return _db.Refunds.AsNoTracking()
            .Where(r => (r.Status == RefundStatus.Transferred || r.Status == RefundStatus.Completed)
                        && r.TransferredAt >= from && r.TransferredAt < to)
            .SumAsync(r => r.TransferredAmount > 0m ? r.TransferredAmount : r.RequestedAmount, ct);
    }


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

    /// <summary>Distinct buyers who submitted at least one order in the period (account activity, UC-140).</summary>
    private Task<int> ActiveBuyersAsync(AnalyticsPeriod period, CancellationToken ct)
    {
        var from = period.FromUtc;
        var to = period.ToUtcExclusive;
        return VisibleOrders(null)
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to)
            .Select(o => o.CustomerId)
            .Distinct()
            .CountAsync(ct);
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

    private async Task<IReadOnlyList<PaymentStatusRow>> PaymentStatusRowsAsync(
        AnalyticsPeriod period, CancellationToken ct)
    {
        var rows = await PaymentsInPeriod(null, period)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        return rows.Select(r => new PaymentStatusRow(r.Status, r.Count, r.Amount)).ToList();
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
}
