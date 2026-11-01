using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
namespace SaveBite.Backend.Services.Interfaces;

public interface IRefundService
{
    Task<RefundResponse> EnsureDeadlineScheduledAsync(Guid refundId, CancellationToken ct);
    Task<RefundResponse?> ExpireDeadlineAsync(Guid refundId, DateTime deadline, CancellationToken ct);
    Task<int> ShopPendingCountAsync(Guid ownerId, CancellationToken ct);
    Task<int> PendingDetailsCountAsync(Guid customerId, CancellationToken ct);
    Task<PagedResult<RefundResponse>> PageAsync(Guid userId, bool forShop, string? status, string? type, int page, int size, CancellationToken ct);
    Task<IReadOnlyList<RefundResponse>> ShopForOrderAsync(Guid ownerId, Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<RefundResponse>> ForOrderAsync(Guid customerId, Guid orderId, CancellationToken ct);
    Task<RefundResponse> GetAsync(Guid userId, Guid refundId, bool forShop, CancellationToken ct);
    Task<RefundResponse> RequestAsync(Guid userId, Guid orderId, CreateRefundRequest request, CancellationToken ct);
    Task<RefundResponse> DestinationAsync(Guid userId, Guid refundId, RefundBankRequest request, CancellationToken ct);
    Task<RefundResponse> VerifyAsync(Guid userId, Guid refundId, string code, CancellationToken ct);
    Task<RefundResponse> WithdrawAsync(Guid userId, Guid refundId, CancellationToken ct);
    Task<RefundResponse> ReviewAsync(Guid userId, Guid refundId, bool approve, string? reason, CancellationToken ct);
    Task<RefundResponse> TransferAsync(Guid userId, Guid refundId, RefundTransferRequest request, CancellationToken ct);
    Task<RefundQrResponse> GenerateQrAsync(Guid userId, Guid refundId, CancellationToken ct);
    Task<Guid?> SystemRefundIdAsync(Guid actorId, Guid orderId, CancellationToken ct, bool forShop = false);
}
