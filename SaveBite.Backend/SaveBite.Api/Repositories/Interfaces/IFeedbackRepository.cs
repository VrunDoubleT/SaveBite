using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IFeedbackRepository
{
    Task<(IReadOnlyList<ProductFeedback> Items, int Total)> ListAsync(int page, int pageSize, FeedbackStatus? status, string? search, CancellationToken ct);
    Task<ProductFeedback?> GetForUpdateAsync(Guid id, CancellationToken ct);
    Task<string?> GetCurrentReasonAsync(Guid feedbackId, CancellationToken ct);
    Task<Dictionary<Guid, string?>> GetCurrentReasonsAsync(IEnumerable<Guid> feedbackIds, CancellationToken ct);
    Task SaveModerationAsync(ProductFeedback feedback, ProductFeedbackModerationLog log, CancellationToken ct);
}