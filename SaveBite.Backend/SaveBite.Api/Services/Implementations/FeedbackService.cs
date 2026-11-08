using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.DTOs;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Hubs;
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
    private readonly SaveBite.Backend.Data.AppDbContext _db;
    private readonly IFeedbackRepository _feedbackRepo;
    private readonly INotificationQueue _notifications;
    private readonly IAdminAuditWriter _audit;
    private readonly IHubContext<FlashDealsHub, IApplicationClient>? _dealHub;
    private readonly IRealtimePublisher? _realtime;

    public FeedbackService(
        IFeedbackRepository feedbackRepo,
        INotificationQueue notifications,
        SaveBite.Backend.Data.AppDbContext db,
        IAdminAuditWriter audit,
        IHubContext<FlashDealsHub, IApplicationClient>? dealHub = null,
        IRealtimePublisher? realtime = null)
    {
        _feedbackRepo = feedbackRepo;
        _notifications = notifications;
        _db = db;
        _audit = audit;
        _dealHub = dealHub;
        _realtime = realtime;
    }

    public async Task<PagedResult<FeedbackResponse>> ListAsync(int page, int pageSize, FeedbackStatus? status, string? search, CancellationToken ct = default)
    {
        var result = await _feedbackRepo.ListAsync(page, pageSize, status, search, ct);
        var items = result.Items.Select(f => new FeedbackResponse
        {
            Id = f.Id, ProductId = f.ProductId, ShopId = f.Product != null ? f.Product.ShopId : (f.OrderItem != null && f.OrderItem.Order != null ? f.OrderItem.Order.ShopId : Guid.Empty), UserId = f.UserId,
            CustomerName = f.User?.FullName ?? string.Empty, CustomerPhone = f.User?.Phone,
            CustomerEmail = f.User?.Email ?? string.Empty, AvatarUrl = f.User?.AvatarUrl, 
            ProductName = f.Product?.Name ?? string.Empty, ShopName = f.OrderItem?.Order?.Shop?.Name ?? f.Product?.Shop?.Name ?? string.Empty, 
            OrderItemId = f.OrderItemId, Rating = f.Rating, 
            Comment = f.Comment, Status = f.Status,
            CurrentReason = f.ModerationLogs.OrderByDescending(l => l.CreatedAt).FirstOrDefault()?.Reason,
            CreatedAt = f.CreatedAt, HiddenAt = f.HiddenAt
        }).ToList();
        return PagedResult<FeedbackResponse>.Create(items, result.Page, result.PageSize, result.TotalItems);
    }

    public Task<ModerationActionResponse> RemoveAsync(Guid adminId, Guid feedbackId, HideViolatingFeedbackRequest request, CancellationToken ct = default)
        => ModerateAsync(adminId, feedbackId, false, request.Reason, ct);

    public Task<ModerationActionResponse> RestoreAsync(Guid adminId, Guid feedbackId, RestoreFeedbackRequest request, CancellationToken ct = default)
        => ModerateAsync(adminId, feedbackId, true, request.Reason, ct);

    private async Task<ModerationActionResponse> ModerateAsync(Guid adminId, Guid feedbackId, bool restore, string? reason, CancellationToken ct)
    {
        return await NotificationTransaction.RunAsync(_db, async () =>
        {
            var feedback = await _feedbackRepo.GetByIdAsync(feedbackId, ct)
                ?? throw AppException.NotFound("Feedback not found.");
            if (restore ? feedback.Status != FeedbackStatus.HiddenByAdmin : feedback.Status != FeedbackStatus.Visible)
                throw AppException.Conflict(restore ? "Only feedback hidden by an admin can be restored." : "Only visible feedback can be removed.");
            var cleanReason = !string.IsNullOrWhiteSpace(reason)
                ? reason.Trim()
                : (restore ? "Restored by administrator" : "Violation of platform community guidelines");
            var now = DateTime.UtcNow;
            var oldStatus = feedback.Status;
            var newStatus = restore ? FeedbackStatus.Visible : FeedbackStatus.HiddenByAdmin;
            var moderationId = Guid.NewGuid();
            var log = new ProductFeedbackModerationLog
            {
                Id = moderationId,
                ProductFeedbackId = feedbackId,
                AdminUserId = adminId,
                Action = restore ? FeedbackModerationAction.Restore : FeedbackModerationAction.Hide,
                PreviousStatus = oldStatus,
                NewStatus = newStatus,
                Reason = cleanReason,
                CreatedAt = now
            };
            _db.ProductFeedbackModerationLogs.Add(log);
            feedback.Status = newStatus;
            feedback.HiddenAt = restore ? null : now;
            feedback.UpdatedAt = now;
            _audit.Record(new AuditLog
            {
                ActorUserId = adminId,
                Action = restore ? AuditActionTypes.FeedbackRestored : AuditActionTypes.FeedbackHidden,
                TargetType = "ProductFeedback",
                TargetId = feedbackId,
                Reason = cleanReason,
                OldValuesJson = JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    Status = newStatus.ToString(),
                    ModerationLogId = moderationId
                }),
                CreatedAt = now
            });
            await _feedbackRepo.UpdateAsync(feedback, ct);
            var productName = feedback.Product?.Name
                ?? feedback.OrderItem?.ProductNameSnapshot
                ?? "this item";
            if (!restore)
            {
                await _notifications.EnqueueAsync(NotificationTarget.User(feedback.UserId), new CreateNotificationCommand(
                    NotificationType.FeedbackRemovedByAdmin, NotificationMessages.Feedback.ReviewRemovedByAdminTitle,
                    NotificationMessages.Feedback.ReviewRemovedByAdminBody(productName, cleanReason),
                    "/account/reviews", $"feedback:{feedbackId}:moderation:{moderationId}"), ct);
            }
            else
            {
                await _notifications.EnqueueAsync(NotificationTarget.User(feedback.UserId), new CreateNotificationCommand(
                    NotificationType.FeedbackReceived, NotificationMessages.Feedback.ReviewRestoredByAdminTitle,
                    NotificationMessages.Feedback.ReviewRestoredByAdminBody(productName),
                    "/account/reviews", $"feedback:{feedbackId}:moderation:{moderationId}"), ct);
            }

            var shopId = feedback.Product?.ShopId;
            if (!shopId.HasValue || shopId == Guid.Empty)
            {
                shopId = feedback.OrderItem?.Order?.ShopId;
            }
            if (!shopId.HasValue || shopId == Guid.Empty)
            {
                shopId = await _db.Products
                    .Where(p => p.Id == feedback.ProductId)
                    .Select(p => (Guid?)p.ShopId)
                    .FirstOrDefaultAsync(ct);
            }

            var eventData = new
            {
                feedbackId = feedback.Id,
                shopId = shopId ?? Guid.Empty,
                productId = feedback.ProductId,
                rating = feedback.Rating,
                action = restore ? "restore" : "hide"
            };

            if (_realtime != null)
            {
                await _realtime.SendToAllAsync(RealtimeEvents.FeedbackChanged, eventData);
                await _realtime.SendToUserAsync(feedback.UserId, RealtimeEvents.FeedbackChanged, eventData);
            }

            if (_dealHub != null)
            {
                var envelope = new RealtimeEvent(Guid.NewGuid(), RealtimeEvents.FeedbackChanged, 1, DateTime.UtcNow, eventData);
                await _dealHub.Clients.All.ReceiveEvent(envelope);
                if (shopId.HasValue && shopId.Value != Guid.Empty)
                {
                    await _dealHub.Clients.Group(FlashDealsHub.ShopGroup(shopId.Value)).ReceiveEvent(envelope);
                }
            }

            return new ModerationActionResponse
            {
                TargetId = feedbackId, Action = restore ? "FEEDBACK_RESTORED" : "FEEDBACK_HIDDEN",
                Message = restore ? "Feedback restored." : "Feedback removed."
            };

        }, ct);
    }

    // Add Feedback
    public async Task<FeedbackResponses.FeedbackItemResponse> AddFeedbackAsync(Guid userId, FeedbackRequests.CreateFeedbackRequest request, CancellationToken ct = default)
    {
        return await NotificationTransaction.RunAsync(_db, async () =>
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
            var shopId = orderItem.Order.ShopId;
            var body = NotificationMessages.Feedback.NewCustomerReviewBody(created.User.FullName, created.Rating, created.Product.Name);
            await _notifications.EnqueueAsync(NotificationTarget.ShopOwner(shopId), new CreateNotificationCommand(
                NotificationType.FeedbackReceived, NotificationMessages.Feedback.NewCustomerReviewTitle, body,
                "/store-owner/reviews", $"feedback:{created.Id}:created:owner"), ct);
            await _notifications.EnqueueAsync(NotificationTarget.ShopStaff(shopId), new CreateNotificationCommand(
                NotificationType.FeedbackReceived, NotificationMessages.Feedback.NewCustomerReviewTitle, body,
                $"/staff/shops/{shopId}/reviews", $"feedback:{created.Id}:created:staff"), ct);

            return MapToResponse(created);

        }, ct);
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
        return await NotificationTransaction.RunAsync(_db, async () =>
        {
            var feedback = await _feedbackRepo.GetByIdAsync(feedbackId, ct);
            if (feedback is null)
                throw AppException.NotFound("Feedback not found.");

            if (feedback.UserId != userId)
                throw AppException.Forbidden("You do not have permission to edit this feedback.");

            if (feedback.Status != FeedbackStatus.Visible)
                throw AppException.BadRequest("Cannot edit feedback that has been removed or hidden.");

            var now = DateTime.UtcNow;
            if (feedback.CreatedAt.AddDays(30) < now)
                throw AppException.BadRequest("Feedback can only be edited within 30 days of submission.");

            if (feedback.EditCount >= 3)
                throw AppException.BadRequest("Feedback can only be edited a maximum of 3 times.");

            feedback.EditCount++;
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

            // Notify shop owner and staff if customer updated review with low rating (<= 3 stars)
            var shopId = feedback.OrderItem?.Order?.ShopId ?? feedback.Product?.ShopId;
            if (shopId.HasValue && request.Rating <= 3)
            {
                var customerName = feedback.User?.FullName ?? "A customer";
                var productName = feedback.Product?.Name ?? "product";
                var body = NotificationMessages.Feedback.ReviewUpdatedLowRatingBody(customerName, request.Rating, productName);
                await _notifications.EnqueueAsync(NotificationTarget.ShopOwner(shopId.Value), new CreateNotificationCommand(
                    NotificationType.FeedbackReceived, NotificationMessages.Feedback.ReviewUpdatedLowRatingTitle, body,
                    "/store-owner/reviews", $"feedback:{feedback.Id}:edited:owner:{now.Ticks}"), ct);
                await _notifications.EnqueueAsync(NotificationTarget.ShopStaff(shopId.Value), new CreateNotificationCommand(
                    NotificationType.FeedbackReceived, NotificationMessages.Feedback.ReviewUpdatedLowRatingTitle, body,
                    $"/staff/shops/{shopId.Value}/reviews", $"feedback:{feedback.Id}:edited:staff:{now.Ticks}"), ct);
            }

            return MapToResponse(feedback);
        }, ct);
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
        return await NotificationTransaction.RunAsync(_db, async () =>
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
            await AddReplyActivityLogAsync(userId, feedback, now, ct);
            await _notifications.EnqueueAsync(NotificationTarget.User(feedback.UserId), new CreateNotificationCommand(
                NotificationType.FeedbackReplied, NotificationMessages.Feedback.ReviewRepliedTitle,
                NotificationMessages.Feedback.ReviewRepliedBody(feedback.OrderItem.Order.Shop.Name, feedback.Product.Name),
                $"/account/reviews?feedbackId={feedbackId}", $"feedback:{feedbackId}:reply:{created.Id}"), ct);

            return new FeedbackResponses.FeedbackReplyResponse(
                created.Id,
                created.Content,
                created.CreatedAt,
                created.UpdatedAt,
                created.RepliedByUser?.FullName ?? "Shop",
                created.RepliedByUserId
            );

        }, ct);
    }

    // FIX-42: record in the shop's staff activity log that a staff member replied to a customer review.
    // Admins replying from the admin side are not shop members, so they are skipped.
    private async Task AddReplyActivityLogAsync(Guid userId, ProductFeedback feedback, DateTime now, CancellationToken ct)
    {
        var shop = feedback.OrderItem.Order.Shop;

        string role;
        string name;
        var staff = await _db.ShopStaffMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ShopId == shop.Id && m.UserId == userId && m.Status == ShopStaffStatus.Active, ct);
        if (staff != null)
        {
            role = "Staff";
            name = staff.DisplayName;
        }
        else if (shop.OwnerUserId == userId)
        {
            role = "StoreOwner";
            name = string.Empty;
        }
        else
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync(ct);
        }

        _db.StaffActivityLogs.Add(new StaffActivityLog
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            ActionBy = userId,
            OrderId = feedback.OrderItem.OrderId,
            Action = StaffActivityAction.FeedbackReplied,
            Note = $"Replied to a customer review of {feedback.Product.Name}.",
            ActorNameSnapshot = name.Trim(),
            ActorRoleSnapshot = role,
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);
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

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null && user.Role != UserRole.Admin && existingReply.RepliedByUserId != userId)
            throw AppException.Forbidden("You can only edit your own reply.");

        existingReply.Content = request.Content.Trim();
        existingReply.UpdatedAt = DateTime.UtcNow;

        await _feedbackRepo.UpdateReplyAsync(existingReply, ct);

        return new FeedbackResponses.FeedbackReplyResponse(
            existingReply.Id,
            existingReply.Content,
            existingReply.CreatedAt,
            existingReply.UpdatedAt,
            existingReply.RepliedByUser?.FullName ?? "Shop",
            existingReply.RepliedByUserId
        );
    }

    // Delete Reply (Shop & Admin)
    public async Task DeleteFeedbackReplyAsync(Guid userId, Guid feedbackId, CancellationToken ct = default)
    {
        await NotificationTransaction.RunAsync(_db, async () =>
        {
            var canManage = await _feedbackRepo.CanManageFeedbackReplyAsync(userId, feedbackId, ct);
            if (!canManage)
                throw AppException.Forbidden("You do not have permission to delete replies for this shop's feedback.");

            var existingReply = await _feedbackRepo.GetReplyByFeedbackIdAsync(feedbackId, ct);
            if (existingReply is null)
                throw AppException.NotFound("Reply not found.");

            var isOwner = false;
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user != null && user.Role != UserRole.Admin)
            {
                isOwner = await _feedbackRepo.IsShopOwnerForFeedbackAsync(userId, feedbackId, ct);
                if (!isOwner && existingReply.RepliedByUserId != userId)
                    throw AppException.Forbidden("You can only delete your own reply.");
            }

            var repliedStaffId = existingReply.RepliedByUserId;
            var isStaffReply = repliedStaffId != userId;

            await _feedbackRepo.DeleteReplyAsync(existingReply, ct);

            // Notify staff member if the shop owner deleted their reply
            if (isOwner && isStaffReply)
            {
                var feedback = await _feedbackRepo.GetByIdAsync(feedbackId, ct);
                var shopId = feedback?.OrderItem?.Order?.ShopId ?? feedback?.Product?.ShopId;
                var now = DateTime.UtcNow;
                var ownerName = user?.FullName ?? "The store owner";
                var url = shopId.HasValue ? $"/staff/shops/{shopId.Value}/reviews" : "/staff/reviews";
                await _notifications.EnqueueAsync(NotificationTarget.User(repliedStaffId), new CreateNotificationCommand(
                    NotificationType.FeedbackReceived,
                    NotificationMessages.Feedback.FeedbackReplyRemovedTitle,
                    NotificationMessages.Feedback.FeedbackReplyRemovedBody(ownerName),
                    url,
                    $"feedback-reply:deleted:{existingReply.Id}:{now.Ticks}"), ct);
            }
        }, ct);
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
                f.Reply.RepliedByUser?.FullName ?? "Shop", f.Reply.RepliedByUserId),
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
}
