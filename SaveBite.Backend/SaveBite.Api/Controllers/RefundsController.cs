using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Controllers;

// Expose refund lists, details and refund-specific commands with action-specific authorization.
[ApiController]
[Route("api/refunds")]
public class RefundsController : ControllerBase
{
    private readonly IRefundService _service;
    private readonly IShopOrderEventService _events;
    private readonly ILogger<RefundsController> _logger;
    public RefundsController(IRefundService service, IShopOrderEventService events, ILogger<RefundsController> logger)
    { _service = service; _events = events; _logger = logger; }

    // Count only shop-cancellation refunds waiting for customer bank details or email confirmation.
    [HttpGet("pending-details-count"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<int>>> PendingDetailsCount(CancellationToken ct)
        => Ok(ApiResponse<int>.Ok(await _service.PendingDetailsCountAsync(UserId(), ct)));

    // Read only the customer's own refund history, optionally filtered by status and refund type.
    [HttpGet, CustomerAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<RefundResponse>>>> History(int page = 1, int pageSize = 10, string? status = null, string? type = null, CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<RefundResponse>>.Ok(await _service.PageAsync(UserId(), false, status, type, page, pageSize, ct)));

    // Read one owned refund with order identity, destination, decision and immutable history.
    [HttpGet("{refundId:guid}"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> Details(Guid refundId, CancellationToken ct)
        => Ok(ApiResponse<RefundResponse>.Ok(await _service.GetAsync(UserId(), refundId, false, ct)));

    // Supply bank details and issue a fifteen-minute email confirmation code.
    [HttpPost("{refundId:guid}/destination"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> Destination(Guid refundId, RefundBankRequest request, CancellationToken ct)
        => await CommittedAsync(_service.DestinationAsync(UserId(), refundId, request, ct), ct);

    // Confirm bank details; customer requests enter owner review while system refunds go to transfer.
    [HttpPost("{refundId:guid}/destination/confirm"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> ConfirmDestination(Guid refundId, VerifyRefundDestinationRequest request, CancellationToken ct)
        => await CommittedAsync(_service.VerifyAsync(UserId(), refundId, request.Code, ct), ct);

    // Withdraw only an owned request in an eligible pre-transfer status.
    [HttpPost("{refundId:guid}/withdraw"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> Withdraw(Guid refundId, CancellationToken ct)
        => await CommittedAsync(_service.WithdrawAsync(UserId(), refundId, ct), ct);

    // Return only the number of unresolved shop refunds for the owner's navigation badge.
    [HttpGet("shop/pending-count"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<int>>> ShopPendingCount(CancellationToken ct)
        => Ok(ApiResponse<int>.Ok(await _service.ShopPendingCountAsync(UserId(), ct)));

    // List this owner's refunds; unconfirmed customer requests are not visible to the shop.
    [HttpGet("shop"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<RefundResponse>>>> ShopHistory(int page = 1, int pageSize = 10, string? status = null, string? type = null, CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<RefundResponse>>.Ok(await _service.PageAsync(UserId(), true, status, type, page, pageSize, ct)));

    // Read recorded refund detail without reviewing, transferring or modifying it.
    [HttpGet("shop/{refundId:guid}"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> ShopDetails(Guid refundId, CancellationToken ct)
        => Ok(ApiResponse<RefundResponse>.Ok(await _service.GetAsync(UserId(), refundId, true, ct)));

    // Approve the full amount of an email-confirmed customer request before its deadline.
    [HttpPost("shop/{refundId:guid}/approve"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> Approve(Guid refundId, CancellationToken ct)
        => await CommittedAsync(_service.ReviewAsync(UserId(), refundId, true, null, ct), ct);

    // Reject a customer request with a reason visible to the customer; never reject a mandatory system refund.
    [HttpPost("shop/{refundId:guid}/reject"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> Reject(Guid refundId, RejectShopOrderRequest request, CancellationToken ct)
        => await CommittedAsync(_service.ReviewAsync(UserId(), refundId, false, request.Reason, ct), ct);

    // Record the owner's full manual transfer, timestamp and required proof; update accumulated value once.
    [HttpPost("shop/{refundId:guid}/transfer"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> Transfer(Guid refundId, RefundTransferRequest request, CancellationToken ct)
        => await CommittedAsync(_service.TransferAsync(UserId(), refundId, request, ct), ct);

    // Generate VietQR using only the approved amount and email-confirmed bank destination.
    [HttpPost("shop/{refundId:guid}/vietqr"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<RefundQrResponse>>> VietQr(Guid refundId, CancellationToken ct)
        => Ok(ApiResponse<RefundQrResponse>.Ok(await _service.GenerateQrAsync(UserId(), refundId, ct)));

    // Refund commands have committed before notifying customer and shop subscribers; notification errors do not undo money records.
    private async Task<ActionResult<ApiResponse<RefundResponse>>> CommittedAsync(Task<RefundResponse> command, CancellationToken ct)
    {
        var result = await command;
        try { await _events.NotifyAsync(result.ShopId, result.OrderId, RealtimeEvents.OrderReasons.RefundChanged, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        { _logger.LogWarning(ex, "Could not notify refund changes for order {OrderId}.", result.OrderId); }
        return Ok(ApiResponse<RefundResponse>.Ok(result));
    }
    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
        ? id : throw AppException.Unauthorized();
}
