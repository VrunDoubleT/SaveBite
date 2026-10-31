using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IPlatformAnalyticsRepository
{
    // UC-139
    Task<PlatformFeeConfig?> GetCurrentFeeConfigAsync(DateTime nowUtc, CancellationToken ct = default);
    Task<FeeAccrualRow> GetFeeAccrualAsync(AnalyticsPeriod period, CancellationToken ct = default);
    Task<FeeStatementSummaryRow> GetFeeStatementSummaryAsync(AnalyticsPeriod period, CancellationToken ct = default);

    // UC-140
    Task<UserCountsRow> GetUserCountsAsync(AnalyticsPeriod period, CancellationToken ct = default);

    // UC-141
    Task<ShopCountsRow> GetShopCountsAsync(AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct = default);

    // UC-142
    Task<IReadOnlyList<CompletedOrderFinancialRow>> GetCompletedOrderFinancialsAsync(
        AnalyticsPeriod period, CancellationToken ct = default);
    Task<IReadOnlyList<OrderStatusCountRow>> GetOrderStatusCountsAsync(
        AnalyticsPeriod period, CancellationToken ct = default);

    // UC-143
    Task<DealStateCountsRow> GetDealStateCountsAsync(
        AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct = default);
    Task<DealSalesTotalsRow> GetDealSalesTotalsAsync(AnalyticsPeriod period, CancellationToken ct = default);

    // UC-144
    Task<IReadOnlyList<PaymentStatusRow>> GetPaymentStatusRowsAsync(AnalyticsPeriod period, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentMethodAmountRow>> GetSucceededPaymentsByMethodAsync(
        AnalyticsPeriod period, CancellationToken ct = default);
    Task<IReadOnlyList<RefundStatusRow>> GetRefundStatusRowsAsync(AnalyticsPeriod period, CancellationToken ct = default);
    Task<IReadOnlyList<RefundTypeRow>> GetRefundTypeRowsAsync(AnalyticsPeriod period, CancellationToken ct = default);
    Task<decimal> GetTransferredRefundAmountAsync(AnalyticsPeriod period, CancellationToken ct = default);
}
