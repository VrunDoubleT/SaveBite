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

// Expose customer order access and shop fulfillment operations with action-specific authorization.
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly IShopOrderService _shopOrders;

    private readonly IRefundService _refunds;
    private readonly TimeProvider _clock;
    private readonly IShopOrderEventService _events;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orders, IShopOrderService shopOrders, IRefundService refunds,
        TimeProvider clock, IShopOrderEventService events, ILogger<OrdersController> logger)
    {
        _orders = orders;
        _shopOrders = shopOrders;
        _refunds = refunds; _clock = clock; _events = events; _logger = logger;
    }

    // Return the authenticated customer's 50 most recent orders.
    [HttpGet]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CheckoutOrderResponse>>>> CustomerOrders(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<CheckoutOrderResponse>>.Ok(await _orders.ListAsync(UserId(), ct)));

    // Load cursor-paginated customer orders filtered by order status, with counts for the status tabs.
    [HttpGet("page")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<OrderHistoryPageResponse>>> CustomerPage([FromQuery] string? status, [FromQuery] string? cursor, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        => Ok(ApiResponse<OrderHistoryPageResponse>.Ok(await _orders.PageAsync(UserId(), status, cursor, pageSize, ct)));

    // Read only the customer's own recorded order without reconciling payments or updating state.
    [HttpGet("{orderId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> CustomerOrder(Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<CheckoutOrderResponse>.Ok(await _orders.GetAsync(UserId(), orderId, ct)));

    // Cancel an eligible customer order before its pickup deadline without refunding paid amounts.
    [HttpPost("{orderId:guid}/cancellations")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> Cancel(Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<CheckoutOrderResponse>.Ok(await _orders.CancelAsync(UserId(), orderId, ct)));


    // Resolve the active owner's shop for the order workspace.
    [HttpGet("my-shop")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<Guid>>> OwnerShop(CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await _shopOrders.OwnerShopAsync(UserId(), ct)));
    // Return only the number of Pending orders for the active owner or staff navbar badge.
    [HttpGet("shops/{shopId:guid}/pending-count")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<int>>> ShopPendingCount(Guid shopId, CancellationToken ct)
        => Ok(ApiResponse<int>.Ok(await _shopOrders.PendingCountAsync(UserId(), shopId, ct)));

    // List this shop's orders by page number with status counts and exact ID/code search.
    [HttpGet("shops/{shopId:guid}")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopOrderPageResponse>>> ShopPage(Guid shopId, string? status, string? search, int page = 1, int pageSize = 10, CancellationToken ct = default, DateOnly? orderDate = null)
        => Ok(ApiResponse<ShopOrderPageResponse>.Ok(await _shopOrders.PageAsync(UserId(), shopId, status, search, page, pageSize, ct, orderDate)));
    // Read recorded items, payments, deadlines, refund details and history actor identities without state changes.
    [HttpGet("shops/{shopId:guid}/{orderId:guid}")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopOrderResponse>>> ShopOrder(Guid shopId, Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<ShopOrderResponse>.Ok(await _shopOrders.GetAsync(UserId(), shopId, orderId, ct)));
    // Confirm a Pending order before five minutes have elapsed since its required payment or deposit succeeded.
    [HttpPost("shops/{shopId:guid}/{orderId:guid}/confirm")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopOrderResponse>>> Confirm(Guid shopId, Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<ShopOrderResponse>.Ok(await _shopOrders.ActionAsync(UserId(), shopId, orderId, "confirm", ct: ct)));
    // Reject only a Pending order with a reason, release applicable stock, and request refund of collected funds.
    [HttpPost("shops/{shopId:guid}/{orderId:guid}/reject")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopOrderResponse>>> Reject(Guid shopId, Guid orderId, RejectShopOrderRequest request, CancellationToken ct)
        => Ok(ApiResponse<ShopOrderResponse>.Ok(await _shopOrders.ActionAsync(UserId(), shopId, orderId, "reject", request.Reason, ct)));
    // Advance only Confirmed to Preparing or Preparing to ReadyForPickup and record the acting shop operator.
    [HttpPost("shops/{shopId:guid}/{orderId:guid}/preparation")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopOrderResponse>>> Preparation(Guid shopId, Guid orderId, PreparationStatusRequest request, CancellationToken ct)
    {
        if (request.Status is not ("Preparing" or "ReadyForPickup")) throw AppException.BadRequest("Choose Preparing or ReadyForPickup.");
        return Ok(ApiResponse<ShopOrderResponse>.Ok(await _shopOrders.ActionAsync(UserId(), shopId, orderId, request.Status, ct: ct)));
    }
    // Complete only a ReadyForPickup order before its hard deadline after handover and verification of full payment.
    [HttpPost("shops/{shopId:guid}/{orderId:guid}/complete")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopOrderResponse>>> Complete(Guid shopId, Guid orderId,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CompleteShopOrderRequest? request = null,
        CancellationToken ct = default)
        => Ok(ApiResponse<ShopOrderResponse>.Ok(await _shopOrders.ActionAsync(UserId(), shopId, orderId, "complete", ct: ct, cashReceived: request?.CashReceived ?? false)));
    // Generate or reuse a QR/link for the recorded PayAtShop balance without accepting an amount from the client.
    [HttpPost("shops/{shopId:guid}/{orderId:guid}/remaining-payment")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopOrderResponse>>> Remaining(Guid shopId, Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<ShopOrderResponse>.Ok(await _shopOrders.RemainingPaymentAsync(UserId(), shopId, orderId, ct)));

    // Upload a customer evidence image or parse/upload a bank QR image for one owned order.
    [HttpPost("{orderId:guid}/refunds/images"), HttpPost("~/api/refunds/orders/{orderId:guid}/images"), CustomerAccess, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<RefundImageResponse>>> UploadCustomerImage(Guid orderId,
        [FromForm] RefundImageUploadRequest request, [FromServices] IRefundImageService images, CancellationToken ct)
        => Ok(ApiResponse<RefundImageResponse>.Ok(await images.UploadAsync(UserId(), orderId, false, request.File, request.Purpose, ct)));

    // Upload transfer proof only for the active owner of the order's shop.
    [HttpPost("shop/{orderId:guid}/refunds/images"), StoreOwnerActiveAccess, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<RefundImageResponse>>> UploadOwnerProof(Guid orderId,
        [FromForm] RefundImageUploadRequest request, [FromServices] IRefundImageService images, CancellationToken ct)
        => Ok(ApiResponse<RefundImageResponse>.Ok(await images.UploadAsync(UserId(), orderId, true, request.File, "transfer-proof", ct)));

    // Read the owned order's refund records for its detail panel, including closed requests.
    [HttpGet("{orderId:guid}/refunds"), HttpGet("~/api/refunds/orders/{orderId:guid}"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RefundResponse>>>> OrderHistory(Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<RefundResponse>>.Ok(await _refunds.ForOrderAsync(UserId(), orderId, ct)));

    // Request the full eligible paid amount of a Completed order within six hours.
    [HttpPost("{orderId:guid}/refunds/requests"), HttpPost("~/api/refunds/orders/{orderId:guid}/requests"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> CreateRequest(Guid orderId, CreateRefundRequest request, CancellationToken ct)
        => await RefundCommittedAsync(_refunds.RequestAsync(UserId(), orderId, request, ct), ct);

    // Read visible refund records for one owner-authorized shop order without processing them.
    [HttpGet("shop/{orderId:guid}/refunds"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RefundResponse>>>> ShopOrderRefunds(Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<RefundResponse>>.Ok(await _refunds.ShopForOrderAsync(UserId(), orderId, ct)));

    // Preserve the existing order-detail read route for system-required refunds without side effects.
    [HttpGet("{orderId:guid}/refund"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse?>>> SystemRefund(Guid orderId, CancellationToken ct)
    {
        var id = await _refunds.SystemRefundIdAsync(UserId(), orderId, ct);
        return Ok(ApiResponse<RefundResponse?>.Ok(id.HasValue ? await _refunds.GetAsync(UserId(), id.Value, false, ct) : null));
    }

    // Keep the legacy system-refund destination route; bank BIN is required for VietQR.
    [HttpPost("{orderId:guid}/refund/destination"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> SystemDestination(Guid orderId, RefundBankRequest request, CancellationToken ct)
        => await RefundCommittedAsync(_refunds.DestinationAsync(UserId(), await RequiredSystemRefundIdAsync(orderId, ct), request, ct), ct);

    // Keep the legacy system-refund confirmation route without duplicating business logic.
    [HttpPost("{orderId:guid}/refund/verify"), CustomerAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> SystemConfirm(Guid orderId, VerifyRefundDestinationRequest request, CancellationToken ct)
        => await RefundCommittedAsync(_refunds.VerifyAsync(UserId(), await RequiredSystemRefundIdAsync(orderId, ct), request.Code, ct), ct);

    // Preserve the existing owner's manual transfer route while delegating to the same audited refund command.
    [HttpPost("shop/{orderId:guid}/refund/transfer"), StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<RefundResponse>>> SystemTransfer(Guid orderId, RecordRefundTransferRequest request, CancellationToken ct)
    {
        var id = await _refunds.SystemRefundIdAsync(UserId(), orderId, ct, forShop: true) ?? throw AppException.NotFound();
        return await RefundCommittedAsync(_refunds.TransferAsync(UserId(), id,
            new RefundTransferRequest { TransferReference = request.TransferReference, ProofUrl = request.ProofUrl, TransferredAt = _clock.GetUtcNow().UtcDateTime }, ct), ct);
    }
    // Notify refund subscribers only after the order-scoped refund command has committed.
    private async Task<ActionResult<ApiResponse<RefundResponse>>> RefundCommittedAsync(Task<RefundResponse> command, CancellationToken ct)
    {
        var result = await command;
        try { await _events.NotifyAsync(result.ShopId, result.OrderId, RealtimeEvents.OrderReasons.RefundChanged, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        { _logger.LogWarning(ex, "Could not notify refund changes for order {OrderId}.", result.OrderId); }
        return Ok(ApiResponse<RefundResponse>.Ok(result));
    }
    private async Task<Guid> RequiredSystemRefundIdAsync(Guid orderId, CancellationToken ct)
        => await _refunds.SystemRefundIdAsync(UserId(), orderId, ct) ?? throw AppException.NotFound("No refund is due for this order.");
    // Read the current actor from authenticated claims rather than client input.
    private Guid UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
