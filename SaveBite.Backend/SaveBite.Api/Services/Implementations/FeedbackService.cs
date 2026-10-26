using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbackRepo;

    public FeedbackService(IFeedbackRepository feedbackRepo)
    {
        _feedbackRepo = feedbackRepo;
    }

    // Add Feedback
    public async Task<FeedbackResponses.FeedbackItemResponse> AddFeedbackAsync(Guid userId, FeedbackRequests.CreateFeedbackRequest request, CancellationToken ct = default)
    {
        var orderItem = await _feedbackRepo.GetOrderItemWithOrderAsync(request.OrderItemId, ct);
        if (orderItem is null)
            throw AppException.NotFound("Order item not found.");

        if (orderItem.Order.CustomerId != userId)
            throw AppException.Forbidden("You do not have permission to review this order item.");

        if (orderItem.Order.Status != OrderStatus.Completed)
            throw AppException.BadRequest("You can only submit feedback for completed orders.");

        var hasFeedback = await _feedbackRepo.HasFeedbackForOrderItemAsync(request.OrderItemId, ct);
        if (hasFeedback)
            throw AppException.Conflict("Feedback has already been submitted for this item.");

        var productId = orderItem.FlashDealVariant?.Variant?.ProductId;
        if (productId is null || productId == Guid.Empty)
            throw AppException.BadRequest("Unable to resolve product from this order item.");

        var now = DateTime.UtcNow;
        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            OrderItemId = orderItem.Id,
            ProductId = productId.Value,
            UserId = userId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            Status = FeedbackStatus.Visible,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _feedbackRepo.CreateAsync(feedback, ct);

        return MapToResponse(created);
    }

    // View Feedback History
    public async Task<PagedResult<FeedbackResponses.FeedbackItemResponse>> GetMyFeedbackHistoryAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var pagedFeedbacks = await _feedbackRepo.GetCustomerFeedbackHistoryAsync(userId, page, pageSize, ct);
        var responseItems = pagedFeedbacks.Items.Select(MapToResponse).ToList();

        return PagedResult<FeedbackResponses.FeedbackItemResponse>.Create(responseItems, pagedFeedbacks.Page, pagedFeedbacks.PageSize, pagedFeedbacks.TotalItems);
    }

    // Edit Feedback
    public async Task<FeedbackResponses.FeedbackItemResponse> EditFeedbackAsync(Guid userId, Guid feedbackId, FeedbackRequests.UpdateFeedbackRequest request, CancellationToken ct = default)
    {
        var feedback = await _feedbackRepo.GetByIdAsync(feedbackId, ct);
        if (feedback is null)
            throw AppException.NotFound("Feedback not found.");

        if (feedback.UserId != userId)
            throw AppException.Forbidden("You do not have permission to edit this feedback.");

        if (feedback.Status != FeedbackStatus.Visible)
            throw AppException.BadRequest("Cannot edit feedback that has been removed or hidden.");

        feedback.Rating = request.Rating;
        feedback.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        feedback.UpdatedAt = DateTime.UtcNow;

        await _feedbackRepo.UpdateAsync(feedback, ct);

        return MapToResponse(feedback);
    }

    // Delete Feedback (Soft Delete)
    public async Task DeleteFeedbackAsync(Guid userId, Guid feedbackId, CancellationToken ct = default)
    {
        var feedback = await _feedbackRepo.GetByIdAsync(feedbackId, ct);
        if (feedback is null)
            throw AppException.NotFound("Feedback not found.");

        if (feedback.UserId != userId)
            throw AppException.Forbidden("You do not have permission to delete this feedback.");

        if (feedback.Status != FeedbackStatus.Visible)
            return; 

        var now = DateTime.UtcNow;
        feedback.Status = FeedbackStatus.HiddenByCustomer;
        feedback.HiddenAt = now;
        feedback.UpdatedAt = now;

        await _feedbackRepo.UpdateAsync(feedback, ct);
    }

    private static FeedbackResponses.FeedbackItemResponse MapToResponse(ProductFeedback f)
    {
        return new FeedbackResponses.FeedbackItemResponse(
            f.Id,
            f.OrderItemId,
            f.ProductId,
            f.OrderItem?.ProductNameSnapshot ?? string.Empty,
            f.OrderItem?.VariantNameSnapshot,
            f.Rating,
            f.Comment,
            f.Status,
            f.CreatedAt,
            f.UpdatedAt,
            f.Reply is null ? null : new FeedbackResponses.FeedbackReplyResponse(
                f.Reply.Id,
                f.Reply.Content,
                f.Reply.CreatedAt),
            f.OrderItem?.OrderId ?? Guid.Empty,
            f.OrderItem?.Order?.OrderCode ?? string.Empty,
            f.OrderItem?.Order?.Shop?.Name ?? string.Empty,
            f.OrderItem?.Order?.Shop?.LogoUrl,
            f.Product?.Images.OrderBy(image => image.Id).Select(image => image.ImageUrl).FirstOrDefault()
        );
    }
}
