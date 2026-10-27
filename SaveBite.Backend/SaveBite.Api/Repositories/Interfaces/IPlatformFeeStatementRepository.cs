using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IPlatformFeeStatementRepository
{
    Task<PagedResult<PlatformFeeStatement>> GetShopStatementsAsync(Guid shopId, GetShopPlatformFeesRequest request, CancellationToken ct = default);
    Task<PagedResult<PlatformFeeStatement>> GetAdminStatementsAsync(GetAdminPlatformFeesRequest request, CancellationToken ct = default);
    Task<PlatformFeeStatement?> GetStatementDetailAsync(Guid statementId, CancellationToken ct = default);
    Task<PlatformFeePayment?> GetPaymentByGatewayCodeAsync(string orderCodeStr, CancellationToken ct = default);
    Task<Shop?> GetShopByIdAsync(Guid shopId, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformFeeStatement>> GetOverdueStatementsAsync(DateTime asOf, CancellationToken ct = default);
    Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<PlatformFeeConfig?> GetActiveFeeConfigAsync(DateTime asOf, CancellationToken ct = default);
    Task AddPaymentAsync(PlatformFeePayment payment, CancellationToken ct = default);
    Task AddOrderPlatformFeeAsync(OrderPlatformFee orderFee, CancellationToken ct = default);
    Task<IReadOnlyList<OrderPlatformFee>> GetUnbilledOrderFeesAsync(int billingYear, short billingMonth, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformFeeAdjustment>> GetUnbilledAdjustmentsAsync(int billingYear, short billingMonth, CancellationToken ct = default);
    Task<bool> HasOrderPlatformFeeAsync(Guid orderId, CancellationToken ct = default);
    Task<bool> HasOtherOverdueStatementsAsync(Guid shopId, Guid currentStatementId, CancellationToken ct = default);
    Task<PlatformFeeStatement?> GetShopStatementForPeriodAsync(Guid shopId, int billingYear, short billingMonth, CancellationToken ct = default);
    Task CancelPendingPaymentsForStatementAsync(Guid statementId, CancellationToken ct = default);
    Task AddFeeAdjustmentAsync(PlatformFeeAdjustment adjustment, CancellationToken ct = default);
    Task AddStatementAsync(PlatformFeeStatement statement, CancellationToken ct = default);
    void AddAuditLog(AuditLog audit);
    Task SaveChangesAsync(CancellationToken ct = default);
}
