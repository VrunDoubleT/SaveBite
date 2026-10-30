using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
namespace SaveBite.Backend.Services.Interfaces;
public interface IWarningService
{
    Task<PagedResult<WarningResponse>> GetHistoryAsync(Guid ownerId, int page, int pageSize, CancellationToken ct);
    Task<WarningResponse> GetDetailsAsync(Guid ownerId, Guid warningId, CancellationToken ct);
    Task<ModerationActionResponse> IssueWarningAsync(Guid adminId, Guid productId, IssueViolationWarningRequest request, CancellationToken ct);
}
