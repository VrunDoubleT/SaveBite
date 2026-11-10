using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class PlatformFeeStatementRepository : IPlatformFeeStatementRepository
{
    private readonly AppDbContext _context;

    public async Task<IReadOnlyList<PlatformFeeStatement>> GetUnpaidStatementsAsync(CancellationToken ct = default)
        => await _context.PlatformFeeStatements.AsNoTracking().Include(s => s.Shop)
            .Where(s => (s.Status == PlatformFeeStatementStatus.Issued || s.Status == PlatformFeeStatementStatus.Overdue)
                && s.TotalFeeAmount > 0 && s.DueAt != null).ToListAsync(ct);


    public async Task<IReadOnlyList<PlatformFeeStatement>> GetStatementsForPeriodAsync(int year, short month, CancellationToken ct = default)
        => await _context.PlatformFeeStatements.AsNoTracking().Include(s => s.Shop)
            .Where(s => s.BillingYear == year && s.BillingMonth == month && s.Status != PlatformFeeStatementStatus.Draft)
            .ToListAsync(ct);


    public PlatformFeeStatementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PlatformFeeStatement>> GetShopStatementsAsync(
        Guid shopId,
        GetShopPlatformFeesRequest request,
        CancellationToken ct = default)
    {
        var query = _context.PlatformFeeStatements
            .AsNoTracking()
            .Where(s => s.ShopId == shopId);

        if (request.Year.HasValue)
        {
            query = query.Where(s => s.BillingYear == request.Year.Value);
        }

        if (request.Month.HasValue)
        {
            query = query.Where(s => s.BillingMonth == request.Month.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }

        var total = await query.CountAsync(ct);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var items = await query
            .OrderByDescending(s => s.BillingYear)
            .ThenByDescending(s => s.BillingMonth)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PagedResult<PlatformFeeStatement>.Create(items, page, pageSize, total);
    }

    public async Task<ShopMonthlyPlatformFeeReportData> GetShopMonthlyPlatformFeeReportAsync(
        Guid shopId,
        GetShopMonthlyPlatformFeeReportRequest request,
        CancellationToken ct = default)
    {
        var startOfMonth = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);
        var query = _context.Set<OrderPlatformFee>()
            .AsNoTracking()
            .Where(f => f.ShopId == shopId && f.CalculatedAt >= startOfMonth && f.CalculatedAt < endOfMonth);

        var feesWithAdjustments = query.Select(f => new
        {
            Fee = f,
            RefundAdjustmentAmount = f.Adjustments.Sum(a => (decimal?)a.Amount) ?? 0m
        });

        var totals = await feesWithAdjustments
            .GroupBy(_ => 1)
            .Select(group => new
            {
                OrderCount = group.Count(),
                TotalGrossAmount = group.Sum(f => (decimal?)f.Fee.GrossAmount) ?? 0m,
                TotalFeeAmount = group.Sum(f => (decimal?)Math.Max(0m, f.Fee.FeeAmount + f.RefundAdjustmentAmount)) ?? 0m,
                UnbilledFeeAmount = group
                    .Where(f => f.Fee.StatementId == null)
                    .Sum(f => (decimal?)Math.Max(0m, f.Fee.FeeAmount + f.RefundAdjustmentAmount)) ?? 0m
            })
            .FirstOrDefaultAsync(ct);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);
        var items = await feesWithAdjustments
            .OrderByDescending(f => f.Fee.CalculatedAt)
            .ThenBy(f => f.Fee.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new ShopMonthlyPlatformFeeOrderData
            {
                Id = f.Fee.Id,
                OrderId = f.Fee.OrderId,
                OrderCode = f.Fee.Order.OrderCode,
                FeeConfigId = f.Fee.FeeConfigId,
                GrossAmount = f.Fee.GrossAmount,
                FeeRate = f.Fee.FeeRate,
                OriginalFeeAmount = f.Fee.FeeAmount,
                RefundAdjustmentAmount = f.RefundAdjustmentAmount,
                FeeAmount = Math.Max(0m, f.Fee.FeeAmount + f.RefundAdjustmentAmount),
                CalculatedAt = f.Fee.CalculatedAt,
                StatementId = f.Fee.StatementId
            })
            .ToListAsync(ct);

        return new ShopMonthlyPlatformFeeReportData
        {
            OrderCount = totals?.OrderCount ?? 0,
            TotalGrossAmount = totals?.TotalGrossAmount ?? 0m,
            TotalFeeAmount = totals?.TotalFeeAmount ?? 0m,
            UnbilledFeeAmount = totals?.UnbilledFeeAmount ?? 0m,
            Orders = PagedResult<ShopMonthlyPlatformFeeOrderData>.Create(items, page, pageSize, totals?.OrderCount ?? 0)
        };
    }

    public async Task<PagedResult<PlatformFeeStatement>> GetAdminStatementsAsync(
        GetAdminPlatformFeesRequest request,
        CancellationToken ct = default)
    {
        var query = _context.PlatformFeeStatements
            .AsNoTracking()
            .Include(s => s.Shop)
            .AsQueryable();
        if (request.Year.HasValue)
        {
            query = query.Where(s => s.BillingYear == request.Year.Value);
        }
        if (request.Month.HasValue)
        {
            query = query.Where(s => s.BillingMonth == request.Month.Value);
        }
        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s => (s.Shop != null && s.Shop.Name.ToLower().Contains(search)) || s.Id.ToString().Contains(search));
        }
        var total = await query.CountAsync(ct);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);
        var items = await query
            .OrderByDescending(s => s.BillingYear)
            .ThenByDescending(s => s.BillingMonth)
            .ThenByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return PagedResult<PlatformFeeStatement>.Create(items, page, pageSize, total);
    }

    public async Task<PlatformFeeStatement?> GetStatementDetailAsync(Guid statementId, CancellationToken ct = default)
    {
        return await _context.PlatformFeeStatements
            .Include(s => s.Shop)
            .Include(s => s.OrderPlatformFees)
            .Include(s => s.Adjustments)
            .Include(s => s.Payments)
            .Include(s => s.Histories)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == statementId, ct);
    }

    public async Task<PlatformFeePayment?> GetPaymentByGatewayCodeAsync(string orderCodeStr, CancellationToken ct = default)
    {
        return await _context.Set<PlatformFeePayment>()
            .Include(p => p.Statement)
            .Include(p => p.Shop)
            .FirstOrDefaultAsync(p => p.GatewayTransactionId == orderCodeStr, ct);
    }

    public async Task<Shop?> GetShopByIdAsync(Guid shopId, CancellationToken ct = default)
    {
        return await _context.Shops
            .FirstOrDefaultAsync(s => s.Id == shopId, ct);
    }

    public async Task<IReadOnlyList<PlatformFeeStatement>> GetOverdueStatementsAsync(DateTime asOf, CancellationToken ct = default)
    {
        return await _context.PlatformFeeStatements
            .Include(s => s.Shop)
            .Where(s => s.Status == PlatformFeeStatementStatus.Issued && s.PaymentPauseReason == null && s.TotalFeeAmount > 0 && s.DueAt != null && s.DueAt < asOf)
            .ToListAsync(ct);
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Refunds)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<PlatformFeeConfig?> GetActiveFeeConfigAsync(DateTime asOf, CancellationToken ct = default)
    {
        return await _context.PlatformFeeConfigs
            .Where(c => c.LifecycleStatus == "Published" && c.EffectiveFrom <= asOf && (c.EffectiveTo == null || c.EffectiveTo > asOf))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddPaymentAsync(PlatformFeePayment payment, CancellationToken ct = default)
    {
        await _context.Set<PlatformFeePayment>().AddAsync(payment, ct);
    }

    public async Task AddOrderPlatformFeeAsync(OrderPlatformFee orderFee, CancellationToken ct = default)
    {
        await _context.Set<OrderPlatformFee>().AddAsync(orderFee, ct);
    }

    public async Task<IReadOnlyList<OrderPlatformFee>> GetUnbilledOrderFeesAsync(int billingYear, short billingMonth, CancellationToken ct = default)
    {
        var startOfMonth = new DateTime(billingYear, billingMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);
        return await _context.Set<OrderPlatformFee>()
            .Include(f => f.Order)
            .ThenInclude(o => o.Refunds)
            .Where(f => f.StatementId == null && f.CalculatedAt < endOfMonth)
            .ToListAsync(ct);
    }
    
    public async Task<IReadOnlyList<PlatformFeeAdjustment>> GetUnbilledAdjustmentsAsync(int billingYear, short billingMonth, CancellationToken ct = default)
    {
        var startOfMonth = new DateTime(billingYear, billingMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);
        return await _context.Set<PlatformFeeAdjustment>()
            .Where(a => a.StatementId == null && a.CreatedAt < endOfMonth)
            .ToListAsync(ct);
    }

    public async Task<bool> HasOrderPlatformFeeAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _context.Set<OrderPlatformFee>()
            .AnyAsync(f => f.OrderId == orderId, ct);
    }

    public async Task<bool> HasOtherOverdueStatementsAsync(Guid shopId, Guid currentStatementId, CancellationToken ct = default)
    {
        return await _context.PlatformFeeStatements
            .AnyAsync(s => s.ShopId == shopId && s.Id != currentStatementId && (s.Status == PlatformFeeStatementStatus.Overdue
                    || (s.Status == PlatformFeeStatementStatus.Issued && s.TotalFeeAmount > 0 && s.DueAt <= DateTime.UtcNow)), ct);
    }
    public async Task<PlatformFeeStatement?> GetShopStatementForPeriodAsync(Guid shopId, int billingYear, short billingMonth, CancellationToken ct = default)
    {
        return await _context.PlatformFeeStatements
            .FirstOrDefaultAsync(s => s.ShopId == shopId && s.BillingYear == billingYear && s.BillingMonth == billingMonth, ct);
    }
     public async Task CancelPendingPaymentsForStatementAsync(Guid statementId, CancellationToken ct = default)
        {
            var pendingPayments = await _context.Set<PlatformFeePayment>()
                .Where(p => p.StatementId == statementId && p.Status == PaymentStatus.Pending)
                .ToListAsync(ct);
            var now = DateTime.UtcNow;
            foreach (var p in pendingPayments)
            {
                p.Status = PaymentStatus.Cancelled;
                p.UpdatedAt = now;
            }
        }
    public async Task AddStatementAsync(PlatformFeeStatement statement, CancellationToken ct = default)
    {
        await _context.PlatformFeeStatements.AddAsync(statement, ct);
    }
    
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
