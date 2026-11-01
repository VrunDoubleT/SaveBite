using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IFeedbackRepository
{
    Task<OrderItem?> GetOrderItemWithOrderAsync(Guid orderItemId, CancellationToken ct = default);
    Task<bool> HasFeedbackForOrderItemAsync(Guid orderItemId, CancellationToken ct = default);
    Task<ProductFeedback?> GetByIdAsync(Guid feedbackId, CancellationToken ct = default);
    Task<ProductFeedback> CreateAsync(ProductFeedback feedback, CancellationToken ct = default);
    Task UpdateAsync(ProductFeedback feedback, CancellationToken ct = default);
    Task<PagedResult<ProductFeedback>> GetCustomerFeedbackHistoryAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
}
