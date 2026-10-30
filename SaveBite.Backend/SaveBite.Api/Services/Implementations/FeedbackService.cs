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
    private readonly IFeedbackRepository _repository;
    private readonly IRabbitMqPublisher _publisher;

    public FeedbackService(IFeedbackRepository repository, IRabbitMqPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    // Map Responses.
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

    // Manage Feedbacks.
    public async Task<PagedResult<FeedbackResponse>> ListAsync(int page, int pageSize, FeedbackStatus? status, string? search, CancellationToken ct)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw AppException.BadRequest("Invalid pagination.");

        var (items, total) = await _repository.ListAsync(page, pageSize, status, search, ct);
        var feedbackIds = items.Select(x => x.Id).ToArray();
        var reasons = await _repository.GetCurrentReasonsAsync(feedbackIds, ct);
        var responses = items.Select(feedback =>
        {
            reasons.TryGetValue(feedback.Id, out var currentReason);
            return Map(feedback, currentReason);
        }).ToList();

        return PagedResult<FeedbackResponse>.Create(responses, page, pageSize, total);
    }

    // Manage Feedbacks.
    public async Task<ModerationActionResponse> RemoveAsync(Guid adminId, Guid feedbackId, HideViolatingFeedbackRequest request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5 || reason.Length > 1000)
            throw AppException.BadRequest("Removal reason must contain 5 to 1000 characters.");

        var feedback = await _repository.GetForUpdateAsync(feedbackId, ct);

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

        await _repository.SaveModerationAsync(feedback, moderationLog, ct);

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

    // Manage Feedbacks.
    public async Task<ModerationActionResponse> RestoreAsync(Guid adminId, Guid feedbackId, RestoreFeedbackRequest request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim();

        if (reason?.Length > 1000)
            throw AppException.BadRequest("Restore reason must not exceed 1000 characters.");

        var feedback = await _repository.GetForUpdateAsync(feedbackId, ct);

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

        await _repository.SaveModerationAsync(feedback, moderationLog, ct);

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
