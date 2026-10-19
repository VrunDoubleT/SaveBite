using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

// Expose read-only customer payment history with associated order details.
public interface IPaymentHistoryService
{
    Task<PagedResult<CustomerPaymentResponse>> PageAsync(Guid userId, int page, int pageSize, string? status, CancellationToken ct = default);
    Task<CustomerPaymentDetailResponse> GetAsync(Guid userId, Guid paymentId, CancellationToken ct = default);
}
