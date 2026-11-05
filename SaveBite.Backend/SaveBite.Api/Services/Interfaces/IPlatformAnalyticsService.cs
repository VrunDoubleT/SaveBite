using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IPlatformAnalyticsService
{
    Task<PlatformDashboardAnalyticsResponse> GetDashboardAsync(AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<PlatformFeeStatisticsResponse> GetPlatformFeeStatisticsAsync(AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<PlatformUserStatisticsResponse> GetUserStatisticsAsync(AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<PlatformShopStatisticsResponse> GetShopStatisticsAsync(AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<PlatformRevenueAndOrderStatisticsResponse> GetRevenueAndOrderStatisticsAsync(AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<PlatformShopRevenueResponse> GetShopRevenueAsync(AnalyticsPeriodRequest request, bool allTime = false, CancellationToken ct = default);
    Task<PlatformFlashDealStatisticsResponse> GetFlashDealStatisticsAsync(AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<PlatformPaymentAndRefundStatisticsResponse> GetPaymentAndRefundStatisticsAsync(AnalyticsPeriodRequest request, CancellationToken ct = default);
}