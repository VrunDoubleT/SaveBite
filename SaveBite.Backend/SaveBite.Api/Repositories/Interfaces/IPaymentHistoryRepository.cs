using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Repositories.Interfaces;

// Read paginated and individual payments within the active customer's ownership scope.
public interface IPaymentHistoryRepository
{
    Task RequireCustomerAsync(Guid userId, CancellationToken ct);
    Task<(IReadOnlyList<Payment> Items, int TotalItems)> PageAsync(Guid userId, PaymentStatus? status, int page, int pageSize, CancellationToken ct);
    Task<Payment?> GetAsync(Guid userId, Guid paymentId, CancellationToken ct);
}
