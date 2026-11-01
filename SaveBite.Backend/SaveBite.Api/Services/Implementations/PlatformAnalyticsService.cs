using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

/// <summary>
/// FE-23 (BR-190). Uses the same period resolver, order visibility and revenue definition as the store dashboard,
/// so platform figures are the sum of what individual shops see.
/// </summary>
public sealed class PlatformAnalyticsService : IPlatformAnalyticsService
{
    private readonly IPlatformAnalyticsRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PlatformAnalyticsService(
        IPlatformAnalyticsRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// The dashboard returns the same figures twice: for the selected period (default: today) and for everything the
    /// platform has ever recorded.
    /// </summary>
    public async Task<PlatformDashboardAnalyticsResponse> GetDashboardAsync(
        AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var (nowUtc, period) = Resolve(request);
        var allTime = AnalyticsPeriodResolver.AllTime(nowUtc);

        return new PlatformDashboardAnalyticsResponse(
            AnalyticsCalculator.DescribePeriod(period),
            await BuildSnapshotAsync(period, nowUtc, ct),
            await BuildSnapshotAsync(allTime, nowUtc, ct));
    }

    private async Task<PlatformAnalyticsSnapshotResponse> BuildSnapshotAsync(
        AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        return new PlatformAnalyticsSnapshotResponse(
            await BuildFeeStatisticsAsync(period, nowUtc, ct),
            await BuildUserStatisticsAsync(period, ct),
            await BuildShopStatisticsAsync(period, nowUtc, ct),
            await BuildRevenueAndOrderStatisticsAsync(period, ct),
            await BuildFlashDealStatisticsAsync(period, nowUtc, ct),
            await BuildPaymentAndRefundStatisticsAsync(period, ct));
    }

    public async Task<PlatformFeeStatisticsResponse> GetPlatformFeeStatisticsAsync(
        AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var (nowUtc, period) = Resolve(request);
        return await BuildFeeStatisticsAsync(period, nowUtc, ct);
    }

    public async Task<PlatformUserStatisticsResponse> GetUserStatisticsAsync(
        AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var (_, period) = Resolve(request);
        return await BuildUserStatisticsAsync(period, ct);
    }

    public async Task<PlatformShopStatisticsResponse> GetShopStatisticsAsync(
        AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var (nowUtc, period) = Resolve(request);
        return await BuildShopStatisticsAsync(period, nowUtc, ct);
    }

    public async Task<PlatformRevenueAndOrderStatisticsResponse> GetRevenueAndOrderStatisticsAsync(
        AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var (_, period) = Resolve(request);
        return await BuildRevenueAndOrderStatisticsAsync(period, ct);
    }

    public async Task<PlatformFlashDealStatisticsResponse> GetFlashDealStatisticsAsync(
        AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var (nowUtc, period) = Resolve(request);
        return await BuildFlashDealStatisticsAsync(period, nowUtc, ct);
    }

    public async Task<PlatformPaymentAndRefundStatisticsResponse> GetPaymentAndRefundStatisticsAsync(
        AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var (_, period) = Resolve(request);
        return await BuildPaymentAndRefundStatisticsAsync(period, ct);
    }

    private (DateTime NowUtc, AnalyticsPeriod Period) Resolve(AnalyticsPeriodRequest request)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return (nowUtc, AnalyticsPeriodResolver.Resolve(request, nowUtc));
    }

    // UC-139 & BR-093 / BR-164: fees accrue on Completed orders; monthly statements aggregate them.
    private async Task<PlatformFeeStatisticsResponse> BuildFeeStatisticsAsync(
        AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var config = await _repository.GetCurrentFeeConfigAsync(nowUtc, ct);
        var accrual = await _repository.GetFeeAccrualAsync(period, ct);
        var statements = await _repository.GetFeeStatementSummaryAsync(period, ct);

        // When no fee configuration is effective the rate is reported as unknown instead of an invented default.
        decimal? currentRatePercent = config is null ? null : Math.Round(config.FeeRate * 100m, 2);

        var statementCount = statements.StatusBreakdown.Sum(s => s.Count);
        var breakdown = Enum.GetValues<PlatformFeeStatementStatus>()
            .Select(status =>
            {
                var row = statements.StatusBreakdown.FirstOrDefault(s => s.Status == status);
                var count = row?.Count ?? 0;
                return new StatusMetricItemResponse(
                    status.ToString(), count, row?.Amount ?? 0m, AnalyticsCalculator.Percent(count, statementCount));
            })
            .ToList();

        var recent = statements.Recent
            .Select(s => new PlatformFeeStatementSummaryItemResponse(
                s.Id, s.ShopId, s.ShopName, s.BillingYear, s.BillingMonth, s.Status.ToString(),
                s.TotalFeeAmount, s.IssuedAt, s.DueAt, s.PaidAt))
            .ToList();

        return new PlatformFeeStatisticsResponse(
            currentRatePercent,
            accrual.GrossFee,
            accrual.Adjustments,
            accrual.GrossFee + accrual.Adjustments,
            statements.PaidInPeriod,
            statements.OutstandingBalance,
            accrual.FeeBearingOrders,
            breakdown,
            recent);
    }

    // UC-140: user growth and account activity.
    private async Task<PlatformUserStatisticsResponse> BuildUserStatisticsAsync(AnalyticsPeriod period, CancellationToken ct)
    {
        var counts = await _repository.GetUserCountsAsync(period, ct);

        var roles = Enum.GetValues<UserRole>()
            .Select(role =>
            {
                var count = counts.Roles.Where(r => r.Key == role).Sum(r => r.Value);
                return new StatusMetricItemResponse(role.ToString(), count, 0m, AnalyticsCalculator.Percent(count, counts.Total));
            })
            .ToList();

        // Percentages are relative to the users that actually have a trust level, so they add up to 100%.
        var trust = counts.TrustLevels
            .Select(t => new StatusMetricItemResponse(
                t.Key, t.Value, 0m, AnalyticsCalculator.Percent(t.Value, counts.UsersWithTrustLevel)))
            .ToList();

        return new PlatformUserStatisticsResponse(
            counts.Total,
            counts.New,
            counts.Verified,
            counts.ActiveAccounts,
            counts.SuspendedAccounts,
            counts.ActiveCustomerProfiles,
            counts.SuspendedCustomerProfiles,
            counts.ActiveBuyers,
            AnalyticsCalculator.BuildCountTrend(counts.NewUserCreatedAtUtc, period),
            roles,
            trust);
    }

    // UC-141: shop inventory by status plus activity indicators.
    private async Task<PlatformShopStatisticsResponse> BuildShopStatisticsAsync(
        AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var counts = await _repository.GetShopCountsAsync(period, nowUtc, ct);

        int ShopCount(ShopStatus status) => counts.ShopStatuses.Where(s => s.Key == status).Sum(s => s.Value);

        var applicationTotal = counts.ApplicationStatuses.Sum(a => a.Value);

        return new PlatformShopStatisticsResponse(
            counts.Total,
            counts.New,
            counts.NewApplications,
            ShopCount(ShopStatus.Active),
            ShopCount(ShopStatus.Paused),
            ShopCount(ShopStatus.Suspended),
            counts.ShopsWithActiveDeals,
            counts.ShopsWithCompletedOrders,
            Enum.GetValues<ShopStatus>()
                .Select(status => new StatusMetricItemResponse(
                    status.ToString(), ShopCount(status), 0m, AnalyticsCalculator.Percent(ShopCount(status), counts.Total)))
                .ToList(),
            Enum.GetValues<ShopApplicationStatus>()
                .Select(status =>
                {
                    var count = counts.ApplicationStatuses.Where(a => a.Key == status).Sum(a => a.Value);
                    return new StatusMetricItemResponse(
                        status.ToString(), count, 0m, AnalyticsCalculator.Percent(count, applicationTotal));
                })
                .ToList());
    }

    // UC-142: platform revenue and order statistics (same definitions as the store dashboard).
    private async Task<PlatformRevenueAndOrderStatisticsResponse> BuildRevenueAndOrderStatisticsAsync(
        AnalyticsPeriod period, CancellationToken ct)
    {
        var financials = await _repository.GetCompletedOrderFinancialsAsync(period, ct);
        var statusRows = await _repository.GetOrderStatusCountsAsync(period, ct);

        var totalOrders = statusRows.Sum(r => r.Count);
        var completedFromCohort = statusRows.Where(r => r.Status == OrderStatus.Completed).Sum(r => r.Count);

        // Orders that never produced a sale do not count towards merchandise value.
        var gmv = statusRows
            .Where(r => r.Status is not (OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired or OrderStatus.NoShow))
            .Sum(r => r.TotalAmount);

        var online = financials.Sum(r => r.PaidOnlineAmount);
        var cash = financials.Sum(r => r.PaidCashAmount);
        var paid = online + cash;
        var refunded = financials.Sum(r => r.RefundedAmount);
        var completedCount = financials.Count;

        var breakdown = AnalyticsCalculator.BuildOrderStatusBreakdown(statusRows)
            .OrderByDescending(item => item.Count)
            .ToList();

        return new PlatformRevenueAndOrderStatisticsResponse(
            totalOrders,
            completedCount,
            gmv,
            paid,
            online,
            cash,
            refunded,
            paid - refunded,
            completedCount > 0 ? Math.Round(paid / completedCount, 0) : 0m,
            AnalyticsCalculator.Percent(completedFromCohort, totalOrders),
            breakdown,
            AnalyticsCalculator.BuildRevenueTrend(financials, period));
    }

    // UC-143 & BR-116: platform-wide Flash Deal statistics, defined exactly like the store dashboard's.
    private async Task<PlatformFlashDealStatisticsResponse> BuildFlashDealStatisticsAsync(
        AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var counts = await _repository.GetDealStateCountsAsync(period, nowUtc, ct);
        var sales = await _repository.GetDealSalesTotalsAsync(period, ct);

        return new PlatformFlashDealStatisticsResponse(
            counts.Created,
            counts.OnSaleNow,
            counts.PendingNow,
            counts.HiddenNow,
            counts.CancelledEarly,
            counts.EndedOnSchedule,
            counts.SoldOut,
            counts.AllocatedUnits,
            counts.ReservedUnits,
            sales.Quantity,
            counts.OrdersWithDeals,
            sales.Revenue,
            sales.CustomerSavings,
            AnalyticsCalculator.Percent(counts.SoldUnits, counts.AllocatedUnits),
            AnalyticsCalculator.BuildDealStatusBreakdown(counts.StatusBreakdown, counts.Created));
    }

    // UC-144: payment and refund statistics.
    private async Task<PlatformPaymentAndRefundStatisticsResponse> BuildPaymentAndRefundStatisticsAsync(
        AnalyticsPeriod period, CancellationToken ct)
    {
        var paymentRows = await _repository.GetPaymentStatusRowsAsync(period, ct);
        var methodRows = await _repository.GetSucceededPaymentsByMethodAsync(period, ct);
        var refundStatusRows = await _repository.GetRefundStatusRowsAsync(period, ct);
        var refundTypeRows = await _repository.GetRefundTypeRowsAsync(period, ct);
        var transferred = await _repository.GetTransferredRefundAmountAsync(period, ct);

        var totalPayments = paymentRows.Sum(r => r.Count);
        int PaymentCount(params PaymentStatus[] statuses)
            => paymentRows.Where(r => statuses.Contains(r.Status)).Sum(r => r.Count);

        var paymentBreakdown = Enum.GetValues<PaymentStatus>()
            .Select(status =>
            {
                var count = PaymentCount(status);
                var amount = paymentRows.Where(r => r.Status == status).Sum(r => r.Amount);
                return new StatusMetricItemResponse(
                    status.ToString(), count, amount, AnalyticsCalculator.Percent(count, totalPayments));
            })
            .ToList();

        var methods = AnalyticsCalculator.BuildPaymentMethods(methodRows);

        var totalRefunds = refundStatusRows.Sum(r => r.Count);
        int RefundCount(params RefundStatus[] statuses)
            => refundStatusRows.Where(r => statuses.Contains(r.Status)).Sum(r => r.Count);

        var refundStatusBreakdown = Enum.GetValues<RefundStatus>()
            .Select(status =>
            {
                var count = RefundCount(status);
                var amount = refundStatusRows.Where(r => r.Status == status).Sum(r => r.RequestedAmount);
                return new StatusMetricItemResponse(
                    status.ToString(), count, amount, AnalyticsCalculator.Percent(count, totalRefunds));
            })
            .ToList();

        var refundTypeBreakdown = Enum.GetValues<RefundType>()
            .Select(type =>
            {
                var rows = refundTypeRows.Where(r => r.Type == type).ToList();
                var count = rows.Sum(r => r.Count);
                return new StatusMetricItemResponse(
                    type.ToString(), count, rows.Sum(r => r.RequestedAmount), AnalyticsCalculator.Percent(count, totalRefunds));
            })
            .ToList();

        return new PlatformPaymentAndRefundStatisticsResponse(
            totalPayments,
            PaymentCount(PaymentStatus.Succeeded),
            PaymentCount(PaymentStatus.Pending),
            PaymentCount(PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Expired),
            methods.TotalAmount,
            paymentBreakdown,
            methods.Methods,
            totalRefunds,
            RefundCount(RefundStatus.Transferred, RefundStatus.Completed),
            RefundCount(RefundStatus.AwaitingCustomerDetails, RefundStatus.PendingVerification, RefundStatus.AwaitingTransfer),
            RefundCount(RefundStatus.Rejected, RefundStatus.Withdrawn),
            refundStatusRows.Sum(r => r.RequestedAmount),
            transferred,
            refundStatusBreakdown,
            refundTypeBreakdown);
    }
}
