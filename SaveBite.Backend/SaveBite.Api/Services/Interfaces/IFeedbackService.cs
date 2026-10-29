using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IFeedbackService
{
    // UC-40: Add Feedback
    Task<FeedbackResponses.FeedbackItemResponse> AddFeedbackAsync(Guid userId, FeedbackRequests.CreateFeedbackRequest request, CancellationToken ct = default);
    
    // UC-41: View Feedback History (Customer)
    Task<PagedResult<FeedbackResponses.FeedbackItemResponse>> GetMyFeedbackHistoryAsync(Guid userId, int page, int pageSize, Guid? targetFeedbackId = null, CancellationToken ct = default);

    // Get Single Feedback Detail
    Task<FeedbackResponses.FeedbackItemResponse> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken ct = default);
    
    // UC-42: Edit Feedback
    Task<FeedbackResponses.FeedbackItemResponse> EditFeedbackAsync(Guid userId, Guid feedbackId, FeedbackRequests.UpdateFeedbackRequest request, CancellationToken ct = default);
    
    // UC-43: Delete Feedback (Soft Delete)
    Task DeleteFeedbackAsync(Guid userId, Guid feedbackId, CancellationToken ct = default);

    // Shop & Admin Use Cases:
    // Reply Feedback
    Task<FeedbackResponses.FeedbackReplyResponse> ReplyFeedbackAsync(Guid userId, Guid feedbackId, FeedbackRequests.CreateFeedbackReplyRequest request, CancellationToken ct = default);
    
    // Edit Reply
    Task<FeedbackResponses.FeedbackReplyResponse> EditFeedbackReplyAsync(Guid userId, Guid feedbackId, FeedbackRequests.UpdateFeedbackReplyRequest request, CancellationToken ct = default);
    
    // Delete Reply
    Task DeleteFeedbackReplyAsync(Guid userId, Guid feedbackId, CancellationToken ct = default);

    // View Feedback for Shop
    Task<PagedResult<FeedbackResponses.FeedbackItemResponse>> GetShopFeedbacksAsync(Guid userId, FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default);

    // View Feedback for Admin
    Task<PagedResult<FeedbackResponses.FeedbackItemResponse>> GetAdminFeedbacksAsync(Guid userId, FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default);
}
