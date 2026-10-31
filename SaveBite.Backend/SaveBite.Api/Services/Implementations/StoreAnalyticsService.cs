using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class StoreAnalyticsService : IStoreAnalyticsService
{
    private const int TopDealCount = 8;

    private readonly IStoreAnalyticsRepository _repository;
    private readonly TimeProvider _timeProvider;

    public StoreAnalyticsService(
        IStoreAnalyticsRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// The dashboard returns the same figures twice: for the selected period (default: today) and for everything the
    /// shop has ever recorded. BR-113: only the Store Owner may read the reports of their own Shop.
    /// </summary>
    public async Task<StoreDashboardAnalyticsResponse> GetDashboardAsync(
        Guid ownerUserId,
        AnalyticsPeriodRequest request,
        CancellationToken ct = default)
    {
        var shop = await GetVerifiedOwnerShopAsync(ownerUserId, ct);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var period = AnalyticsPeriodResolver.Resolve(request, nowUtc);
        var allTime = AnalyticsPeriodResolver.AllTime(nowUtc);

        return new StoreDashboardAnalyticsResponse(
            shop.Id,
            shop.Name,
            AnalyticsCalculator.DescribePeriod(period),
            await BuildSnapshotAsync(shop.Id, period, nowUtc, ct),
            await BuildSnapshotAsync(shop.Id, allTime, nowUtc, ct));
    }

    public async Task<StoreRevenueOverviewResponse> GetRevenueOverviewAsync(
        Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var shop = await GetVerifiedOwnerShopAsync(ownerUserId, ct);
        var period = AnalyticsPeriodResolver.Resolve(request, _timeProvider.GetUtcNow().UtcDateTime);
        return await BuildRevenueOverviewAsync(shop.Id, period, ct);
    }

    public async Task<StoreOrderStatisticsResponse> GetOrderStatisticsAsync(
        Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var shop = await GetVerifiedOwnerShopAsync(ownerUserId, ct);
        var period = AnalyticsPeriodResolver.Resolve(request, _timeProvider.GetUtcNow().UtcDateTime);
        return await BuildOrderStatisticsAsync(shop.Id, period, ct);
    }

    public async Task<StoreFlashDealStatisticsResponse> GetFlashDealStatisticsAsync(
        Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var shop = await GetVerifiedOwnerShopAsync(ownerUserId, ct);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var period = AnalyticsPeriodResolver.Resolve(request, nowUtc);
        return await BuildFlashDealStatisticsAsync(shop.Id, period, nowUtc, ct);
    }

    public async Task<StorePaymentMethodRevenueResponse> GetRevenueByPaymentMethodAsync(
        Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default)
    {
        var shop = await GetVerifiedOwnerShopAsync(ownerUserId, ct);
        var period = AnalyticsPeriodResolver.Resolve(request, _timeProvider.GetUtcNow().UtcDateTime);
        return await BuildPaymentMethodRevenueAsync(shop.Id, period, ct);
    }

    private async Task<Shop> GetVerifiedOwnerShopAsync(Guid ownerUserId, CancellationToken ct)
    {
        var shop = await _repository.GetOwnedShopAsync(ownerUserId, ct);
        if (shop is null)
            throw AppException.NotFound("Shop profile not found for this Store Owner account.");
        return shop;
    }

    private async Task<StoreAnalyticsSnapshotResponse> BuildSnapshotAsync(
        Guid shopId, AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        return new StoreAnalyticsSnapshotResponse(
            await BuildRevenueOverviewAsync(shopId, period, ct),
            await BuildOrderStatisticsAsync(shopId, period, ct),
            await BuildFlashDealStatisticsAsync(shopId, period, nowUtc, ct),
            await BuildPaymentMethodRevenueAsync(shopId, period, ct));
    }

    // UC-97 & BR-114: sales revenue = amounts actually paid for Orders completed during the period, less the refunds
    // already transferred for those Orders. Paid money is split into online payments and cash taken at the shop, so the
    // owner can see "how much came in online today, how much in cash". The BR-093 platform fee is shown separately.
    private async Task<StoreRevenueOverviewResponse> BuildRevenueOverviewAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct)
    {
        var rows = await _repository.GetCompletedOrderFinancialsAsync(shopId, period, ct);

        var gross = rows.Sum(r => r.TotalAmount);
        var online = rows.Sum(r => r.PaidOnlineAmount);
        var cash = rows.Sum(r => r.PaidCashAmount);
        var paid = online + cash;
        var refunded = rows.Sum(r => r.RefundedAmount);
        var fee = rows.Sum(r => r.FeeAmount);
        var salesRevenue = paid - refunded;
        var count = rows.Count;

        return new StoreRevenueOverviewResponse(
            gross,
            paid,
            online,
            cash,
            refunded,
            salesRevenue,
            fee,
            salesRevenue - fee,
            count,
            count > 0 ? Math.Round(paid / count, 0) : 0m,
            AnalyticsCalculator.BuildRevenueTrend(rows, period));
    }

    // UC-98 & BR-115: order counts grouped by recorded order status within the selected period.
    private async Task<StoreOrderStatisticsResponse> BuildOrderStatisticsAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct)
    {
        var rows = await _repository.GetOrderStatusCountsAsync(shopId, period, ct);
        return BuildOrderStatistics(rows);
    }

    internal static StoreOrderStatisticsResponse BuildOrderStatistics(IReadOnlyList<OrderStatusCountRow> rows)
    {
        int Count(params OrderStatus[] statuses)
            => rows.Where(r => statuses.Contains(r.Status)).Sum(r => r.Count);

        var total = rows.Sum(r => r.Count);
        var completed = Count(OrderStatus.Completed);
        var cancelledOrRejected = Count(OrderStatus.Cancelled, OrderStatus.Rejected);
        var noShow = Count(OrderStatus.NoShow);

        return new StoreOrderStatisticsResponse(
            total,
            completed,
            Count(OrderStatus.Pending),
            Count(OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyForPickup),
            cancelledOrRejected,
            Count(OrderStatus.Expired),
            noShow,
            rows.Sum(r => r.LateArrivals),
            AnalyticsCalculator.Percent(completed, total),
            AnalyticsCalculator.Percent(cancelledOrRejected, total),
            AnalyticsCalculator.Percent(noShow, total),
            AnalyticsCalculator.BuildOrderStatusBreakdown(rows));
    }

    // UC-99 & BR-116: deals are summarised by the status they actually have, plus the two final states (ended on
    // schedule / cancelled early). Sales and units come from completed order items.
    private async Task<StoreFlashDealStatisticsResponse> BuildFlashDealStatisticsAsync(
        Guid shopId, AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct)
    {
        var counts = await _repository.GetDealStateCountsAsync(shopId, period, nowUtc, ct);
        var sales = await _repository.GetDealSalesTotalsAsync(shopId, period, ct);
        var top = await _repository.GetTopDealsAsync(shopId, period, TopDealCount, ct);

        return new StoreFlashDealStatisticsResponse(
            counts.Created,
            counts.OnSaleNow,
            counts.PendingNow,
            counts.HiddenNow,
            counts.CancelledEarly,
            counts.EndedOnSchedule,
            counts.SoldOut,
            counts.AllocatedUnits,
            sales.Quantity,
            sales.Revenue,
            counts.OrdersWithDeals,
            AnalyticsCalculator.Percent(counts.SoldUnits, counts.AllocatedUnits),
            AnalyticsCalculator.BuildDealStatusBreakdown(counts.StatusBreakdown, counts.Created),
            top.Select(d => new StoreTopFlashDealItemResponse(
                d.FlashDealId,
                d.ProductName,
                d.Status.ToString(),
                d.AllocatedQuantity,
                d.Quantity,
                d.Revenue,
                d.SaleStartTime,
                d.OrderEndTime)).ToList());
    }

    // UC-100 & BR-117: each successful payment is attributed to its recorded payment method and payment date.
    private async Task<StorePaymentMethodRevenueResponse> BuildPaymentMethodRevenueAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct)
    {
        var rows = await _repository.GetSucceededPaymentsByMethodAsync(shopId, period, ct);
        var (totalAmount, totalCount, methods) = AnalyticsCalculator.BuildPaymentMethods(rows);
        return new StorePaymentMethodRevenueResponse(totalAmount, totalCount, methods);
    }
}
