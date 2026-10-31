using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IStoreAnalyticsService
{
    Task<StoreDashboardAnalyticsResponse> GetDashboardAsync(Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<StoreRevenueOverviewResponse> GetRevenueOverviewAsync(Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<StoreOrderStatisticsResponse> GetOrderStatisticsAsync(Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<StoreFlashDealStatisticsResponse> GetFlashDealStatisticsAsync(Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default);
    Task<StorePaymentMethodRevenueResponse> GetRevenueByPaymentMethodAsync(Guid ownerUserId, AnalyticsPeriodRequest request, CancellationToken ct = default);
}