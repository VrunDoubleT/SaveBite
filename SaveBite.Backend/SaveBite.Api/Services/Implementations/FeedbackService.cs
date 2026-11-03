using System.Text.Json;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
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
    private readonly IRabbitMqPublisher _publisher;

    public FeedbackService(IFeedbackRepository feedbackRepo, IRabbitMqPublisher publisher)
    {
        _feedbackRepo = feedbackRepo;
        _publisher = publisher;
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

        if (request.Images is { Count: > 0 })
        {
            var sort = 0;
            foreach (var imgUrl in request.Images.Take(5))
            {
                if (!string.IsNullOrWhiteSpace(imgUrl))
                {
                    feedback.Images.Add(new ProductFeedbackImage
                    {
                        Id = Guid.NewGuid(),
                        ProductFeedbackId = feedback.Id,
                        ImageUrl = imgUrl.Trim(),
                        SortOrder = sort++,
                        CreatedAt = now
                    });
                }
            }
        }

        var created = await _feedbackRepo.CreateAsync(feedback, ct);

        return MapToResponse(created);
    }

    // View Feedback History (Customer)
    public async Task<PagedResult<FeedbackResponses.FeedbackItemResponse>> GetMyFeedbackHistoryAsync(Guid userId, int page, int pageSize, Guid? targetFeedbackId = null, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var pagedFeedbacks = await _feedbackRepo.GetCustomerFeedbackHistoryAsync(userId, page, pageSize, targetFeedbackId, ct);
        var responseItems = pagedFeedbacks.Items.Select(MapToResponse).ToList();

        return PagedResult<FeedbackResponses.FeedbackItemResponse>.Create(responseItems, pagedFeedbacks.Page, pagedFeedbacks.PageSize, pagedFeedbacks.TotalItems);
    }

    // Get Single Feedback Detail
    public async Task<FeedbackResponses.FeedbackItemResponse> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken ct = default)
    {
        var feedback = await _feedbackRepo.GetByIdAsync(feedbackId, ct);
        if (feedback is null || feedback.Status != FeedbackStatus.Visible)
            throw AppException.NotFound("Feedback not found.");

        return MapToResponse(feedback);
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

        var now = DateTime.UtcNow;
        feedback.Rating = request.Rating;
        feedback.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        feedback.UpdatedAt = now;

        if (request.Images != null)
        {
            var newImages = request.Images
                .Where(imgUrl => !string.IsNullOrWhiteSpace(imgUrl))
                .Take(5)
                .Select((imgUrl, index) => new ProductFeedbackImage
                {
                    Id = Guid.NewGuid(),
                    ProductFeedbackId = feedback.Id,
                    ImageUrl = imgUrl.Trim(),
                    SortOrder = index,
                    CreatedAt = now
                })
                .ToList();

            await _feedbackRepo.ReplaceFeedbackImagesAsync(feedback.Id, newImages, ct);
            feedback.Images = newImages;
        }

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

    // Reply Feedback (Shop & Admin)
    public async Task<FeedbackResponses.FeedbackReplyResponse> ReplyFeedbackAsync(Guid userId, Guid feedbackId, FeedbackRequests.CreateFeedbackReplyRequest request, CancellationToken ct = default)
    {
        var canManage = await _feedbackRepo.CanManageFeedbackReplyAsync(userId, feedbackId, ct);
        if (!canManage)
            throw AppException.Forbidden("You do not have permission to reply to this shop's feedback.");

        var feedback = await _feedbackRepo.GetByIdAsync(feedbackId, ct);
        if (feedback is null || feedback.Status != FeedbackStatus.Visible)
            throw AppException.NotFound("Feedback not found or is no longer visible.");

        var existingReply = await _feedbackRepo.GetReplyByFeedbackIdAsync(feedbackId, ct);
        if (existingReply != null)
            throw AppException.Conflict("A reply already exists for this feedback. You can edit the existing reply instead.");

        var now = DateTime.UtcNow;
        var reply = new ProductFeedbackReply
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            RepliedByUserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _feedbackRepo.CreateReplyAsync(reply, ct);

        return new FeedbackResponses.FeedbackReplyResponse(
            created.Id,
            created.Content,
            created.CreatedAt,
            created.UpdatedAt,
            created.RepliedByUser?.FullName ?? "Shop"
        );
    }

    // Edit Reply (Shop & Admin)
    public async Task<FeedbackResponses.FeedbackReplyResponse> EditFeedbackReplyAsync(Guid userId, Guid feedbackId, FeedbackRequests.UpdateFeedbackReplyRequest request, CancellationToken ct = default)
    {
        var canManage = await _feedbackRepo.CanManageFeedbackReplyAsync(userId, feedbackId, ct);
        if (!canManage)
            throw AppException.Forbidden("You do not have permission to edit replies for this shop's feedback.");

        var existingReply = await _feedbackRepo.GetReplyByFeedbackIdAsync(feedbackId, ct);
        if (existingReply is null)
            throw AppException.NotFound("Reply not found.");

        existingReply.Content = request.Content.Trim();
        existingReply.UpdatedAt = DateTime.UtcNow;

        await _feedbackRepo.UpdateReplyAsync(existingReply, ct);

        return new FeedbackResponses.FeedbackReplyResponse(
            existingReply.Id,
            existingReply.Content,
            existingReply.CreatedAt,
            existingReply.UpdatedAt,
            existingReply.RepliedByUser?.FullName ?? "Shop"
        );
    }

    // Delete Reply (Shop & Admin)
    public async Task DeleteFeedbackReplyAsync(Guid userId, Guid feedbackId, CancellationToken ct = default)
    {
        var canManage = await _feedbackRepo.CanManageFeedbackReplyAsync(userId, feedbackId, ct);
        if (!canManage)
            throw AppException.Forbidden("You do not have permission to delete replies for this shop's feedback.");

        var existingReply = await _feedbackRepo.GetReplyByFeedbackIdAsync(feedbackId, ct);
        if (existingReply is null)
            throw AppException.NotFound("Reply not found.");

        await _feedbackRepo.DeleteReplyAsync(existingReply, ct);
    }

    // View Feedback for Shop
    public async Task<PagedResult<FeedbackResponses.FeedbackItemResponse>> GetShopFeedbacksAsync(Guid userId, FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default)
    {
        var shopId = await _feedbackRepo.GetShopIdForUserAsync(userId, ct);
        if (!shopId.HasValue)
            throw AppException.NotFound("No shop found associated with your account.");

        var paged = await _feedbackRepo.GetShopFeedbacksAsync(shopId.Value, request, ct);
        var responseItems = paged.Items.Select(MapToResponse).ToList();

        return PagedResult<FeedbackResponses.FeedbackItemResponse>.Create(responseItems, paged.Page, paged.PageSize, paged.TotalItems);
    }

    // View Feedback for Admin
    public async Task<PagedResult<FeedbackResponses.FeedbackItemResponse>> GetAdminFeedbacksAsync(Guid userId, FeedbackRequests.ShopFeedbacksQueryRequest request, CancellationToken ct = default)
    {
        var paged = await _feedbackRepo.GetAdminFeedbacksAsync(request, ct);
        var responseItems = paged.Items.Select(MapToResponse).ToList();

        return PagedResult<FeedbackResponses.FeedbackItemResponse>.Create(responseItems, paged.Page, paged.PageSize, paged.TotalItems);
    }

    private static FeedbackResponses.FeedbackItemResponse MapToResponse(ProductFeedback f)
    {
        return new FeedbackResponses.FeedbackItemResponse(
            f.Id,
            f.OrderItemId,
            f.ProductId,
            f.OrderItem?.ProductNameSnapshot ?? f.Product?.Name ?? string.Empty,
            f.OrderItem?.VariantNameSnapshot,
            f.Rating,
            f.Comment,
            f.Status,
            f.CreatedAt,
            f.UpdatedAt,
            f.Reply is null ? null : new FeedbackResponses.FeedbackReplyResponse(
                f.Reply.Id,
                f.Reply.Content,
                f.Reply.CreatedAt,
                f.Reply.UpdatedAt,
                f.Reply.RepliedByUser?.FullName ?? "Shop"),
            f.OrderItem?.OrderId ?? Guid.Empty,
            f.OrderItem?.Order?.OrderCode ?? string.Empty,
            f.OrderItem?.Order?.Shop?.Name ?? f.Product?.Shop?.Name ?? string.Empty,
            f.OrderItem?.Order?.Shop?.LogoUrl ?? f.Product?.Shop?.LogoUrl,
            f.Product?.Images.OrderBy(image => image.Id).Select(image => image.ImageUrl).FirstOrDefault(),
            f.Images?.OrderBy(image => image.SortOrder).Select(image => image.ImageUrl).ToList() ?? new List<string>(),
            f.Product?.ShopId ?? f.OrderItem?.Order?.ShopId,
            f.User?.FullName,
            f.User?.AvatarUrl
        );
    }

    // Map Responses (moderation).
    private static FeedbackResponse Map(ProductFeedback feedback, string? currentReason)
    {
        return new FeedbackResponse
        {
            Id = feedback.Id,
            ProductId = feedback.ProductId,
            UserId = feedback.UserId,
            OrderItemId = feedback.OrderItemId,
            CustomerName = feedback.User.FullName,
            CustomerPhone = feedback.User.Phone,
            CustomerEmail = feedback.User.Email,
            ProductName = feedback.Product.Name,
            ShopName = feedback.Product.Shop.Name,
            Rating = feedback.Rating,
            Comment = feedback.Comment,
            Status = feedback.Status,
            CurrentReason = currentReason,
            CreatedAt = feedback.CreatedAt,
            HiddenAt = feedback.HiddenAt
        };
    }

    // Manage Feedbacks (moderation).
    public async Task<PagedResult<FeedbackResponse>> ListAsync(int page, int pageSize, FeedbackStatus? status, string? search, CancellationToken ct)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw AppException.BadRequest("Invalid pagination.");

        var (items, total) = await _feedbackRepo.ListAsync(page, pageSize, status, search, ct);
        var feedbackIds = items.Select(x => x.Id).ToArray();
        var reasons = await _feedbackRepo.GetCurrentReasonsAsync(feedbackIds, ct);
        var responses = items.Select(feedback =>
        {
            reasons.TryGetValue(feedback.Id, out var currentReason);
            return Map(feedback, currentReason);
        }).ToList();

        return PagedResult<FeedbackResponse>.Create(responses, page, pageSize, total);
    }

    // Manage Feedbacks (moderation).
    public async Task<ModerationActionResponse> RemoveAsync(Guid adminId, Guid feedbackId, HideViolatingFeedbackRequest request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5 || reason.Length > 1000)
            throw AppException.BadRequest("Removal reason must contain 5 to 1000 characters.");

        var feedback = await _feedbackRepo.GetForUpdateAsync(feedbackId, ct);

        if (feedback is null)
            throw AppException.NotFound("Feedback not found.");

        if (feedback.Status != FeedbackStatus.Visible)
            throw AppException.Conflict("Only visible feedback can be hidden.");

        var previousStatus = feedback.Status;
        var previousHiddenAt = feedback.HiddenAt;
        var now = DateTime.UtcNow;

        feedback.Status = FeedbackStatus.HiddenByAdmin;
        feedback.HiddenAt = now;
        feedback.UpdatedAt = now;

        var moderationLog = new ProductFeedbackModerationLog
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            AdminUserId = adminId,
            Action = FeedbackModerationAction.Hide,
            PreviousStatus = previousStatus,
            NewStatus = feedback.Status,
            Reason = reason,
            CreatedAt = now
        };

        await _feedbackRepo.SaveModerationAsync(feedback, moderationLog, ct);

        await _publisher.PublishAsync(RabbitMqRoutingKeys.ModerationAudit,
            new ModerationEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = adminId,
                Action = "FEEDBACK_REMOVED_VIOLATION",
                TargetType = "ProductFeedback",
                TargetId = feedbackId,
                Reason = reason,
                OldValuesJson = JsonSerializer.Serialize(new
                {
                    Status = previousStatus.ToString(),
                    HiddenAt = previousHiddenAt
                }),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    Status = feedback.Status.ToString(),
                    feedback.HiddenAt
                }),
                CreatedAt = now
            }, ct);

        return new ModerationActionResponse
        {
            TargetId = feedbackId,
            Action = "FEEDBACK_REMOVED_VIOLATION",
            Message = "Feedback removed."
        };
    }

    // Manage Feedbacks (moderation).
    public async Task<ModerationActionResponse> RestoreAsync(Guid adminId, Guid feedbackId, RestoreFeedbackRequest request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim();

        if (reason?.Length > 1000)
            throw AppException.BadRequest("Restore reason must not exceed 1000 characters.");

        var feedback = await _feedbackRepo.GetForUpdateAsync(feedbackId, ct);

        if (feedback is null)
            throw AppException.NotFound("Feedback not found.");

        if (feedback.Status != FeedbackStatus.HiddenByAdmin)
            throw AppException.Conflict("Only admin-hidden feedback can be restored.");

        var previousStatus = feedback.Status;
        var previousHiddenAt = feedback.HiddenAt;
        var now = DateTime.UtcNow;

        feedback.Status = FeedbackStatus.Visible;
        feedback.HiddenAt = null;
        feedback.UpdatedAt = now;

        var moderationLog = new ProductFeedbackModerationLog
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            AdminUserId = adminId,
            Action = FeedbackModerationAction.Restore,
            PreviousStatus = previousStatus,
            NewStatus = feedback.Status,
            Reason = reason ?? string.Empty,
            CreatedAt = now
        };

        await _feedbackRepo.SaveModerationAsync(feedback, moderationLog, ct);

        await _publisher.PublishAsync(RabbitMqRoutingKeys.ModerationAudit,
            new ModerationEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = adminId,
                Action = "FEEDBACK_RESTORED",
                TargetType = "ProductFeedback",
                TargetId = feedbackId,
                Reason = reason,
                OldValuesJson = JsonSerializer.Serialize(new
                {
                    Status = previousStatus.ToString(),
                    HiddenAt = previousHiddenAt
                }),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    Status = feedback.Status.ToString(),
                    feedback.HiddenAt
                }),
                CreatedAt = now
            }, ct);

        return new ModerationActionResponse
        {
            TargetId = feedbackId,
            Action = "FEEDBACK_RESTORED",
            Message = "Feedback restored."
        };
    }
}