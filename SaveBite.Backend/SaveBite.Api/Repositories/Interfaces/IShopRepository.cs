using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IShopRepository
{
    Task<Shop?> GetShopByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<List<Shop>> GetActiveShopsAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<Shop?> GetActiveShopByIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<bool> ShopExistsAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, (double AvgRating, int TotalCount)>> GetReviewsStatsForShopsAsync(
        List<Guid> shopIds, 
        CancellationToken cancellationToken = default);
    Task<(double AvgRating, int TotalReviews, Dictionary<int, int> RatingBreakdown)> GetShopReviewStatsAsync(
        Guid shopId, 
        CancellationToken cancellationToken = default);
    Task<(List<ProductFeedback> Items, int TotalItems)> GetPagedStoreReviewsAsync(
        Guid shopId,
        int? rating,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
