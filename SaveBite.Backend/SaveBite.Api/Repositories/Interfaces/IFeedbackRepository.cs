using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IFeedbackRepository
{
    Task<OrderItem?> GetOrderItemWithOrderAsync(Guid orderItemId, CancellationToken ct = default);
    Task<bool> HasFeedbackForOrderItemAsync(Guid orderItemId, CancellationToken ct = default);
    Task<ProductFeedback?> GetByIdAsync(Guid feedbackId, CancellationToken ct = default);
    Task<ProductFeedback> CreateAsync(ProductFeedback feedback, CancellationToken ct = default);
    Task ReplaceFeedbackImagesAsync(Guid feedbackId, IReadOnlyList<ProductFeedbackImage> newImages, CancellationToken ct = default);
    Task UpdateAsync(ProductFeedback feedback, CancellationToken ct = default);
    Task<PagedResult<ProductFeedback>> GetCustomerFeedbackHistoryAsync(Guid userId, int page, int pageSize, Guid? targetFeedbackId = null, CancellationToken ct = default);

    // Reply operations
    Task<ProductFeedbackReply?> GetReplyByFeedbackIdAsync(Guid feedbackId, CancellationToken ct = default);
    Task<ProductFeedbackReply> CreateReplyAsync(ProductFeedbackReply reply, CancellationToken ct = default);
    Task UpdateReplyAsync(ProductFeedbackReply reply, CancellationToken ct = default);
    Task DeleteReplyAsync(ProductFeedbackReply reply, CancellationToken ct = default);

    // Shop & Admin queries
    Task<PagedResult<ProductFeedback>> GetShopFeedbacksAsync(Guid shopId, FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default);
    Task<PagedResult<ProductFeedback>> GetAdminFeedbacksAsync(FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default);
    Task<Guid?> GetShopIdForUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CanManageFeedbackReplyAsync(Guid userId, Guid feedbackId, CancellationToken ct = default);
}
