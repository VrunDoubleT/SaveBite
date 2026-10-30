using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IFeedbackService
{
    Task<PagedResult<FeedbackResponse>> ListAsync(int page, int pageSize, FeedbackStatus? status, string? search, CancellationToken ct);
    Task<ModerationActionResponse> RemoveAsync(Guid adminId, Guid feedbackId, HideViolatingFeedbackRequest request, CancellationToken ct);
    Task<ModerationActionResponse> RestoreAsync(Guid adminId, Guid feedbackId, RestoreFeedbackRequest request, CancellationToken ct);
}