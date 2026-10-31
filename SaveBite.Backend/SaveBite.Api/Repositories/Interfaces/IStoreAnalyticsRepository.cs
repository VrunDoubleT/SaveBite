using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IStoreAnalyticsRepository
{
    Task<Shop?> GetOwnedShopAsync(Guid ownerUserId, CancellationToken ct = default);

    // UC-97 (BR-114)
    Task<IReadOnlyList<CompletedOrderFinancialRow>> GetCompletedOrderFinancialsAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default);

    // UC-98 (BR-115)
    Task<IReadOnlyList<OrderStatusCountRow>> GetOrderStatusCountsAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default);

    // UC-99 (BR-116)
    Task<DealStateCountsRow> GetDealStateCountsAsync(
        Guid shopId, AnalyticsPeriod period, DateTime nowUtc, CancellationToken ct = default);

    Task<DealSalesTotalsRow> GetDealSalesTotalsAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default);

    Task<IReadOnlyList<TopDealRow>> GetTopDealsAsync(
        Guid shopId, AnalyticsPeriod period, int take, CancellationToken ct = default);

    // UC-100 (BR-117)
    Task<IReadOnlyList<PaymentMethodAmountRow>> GetSucceededPaymentsByMethodAsync(
        Guid shopId, AnalyticsPeriod period, CancellationToken ct = default);
}
