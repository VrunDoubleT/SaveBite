using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
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

    public OrdersController(IOrderService orders, IShopOrderService shopOrders)
    {
        _orders = orders;
        _shopOrders = shopOrders;
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

    // Load only the customer's own order and reconcile initial or remaining payments with the provider.
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
    // Reconcile the order and return recorded items, payments, deadlines, refund details, and history actor identities.
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
    // Allow only the owner to record a manual refund transfer to the customer's email-verified bank destination.
    [HttpPost("shops/{shopId:guid}/{orderId:guid}/refund/transfer")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<OrderRefundResponse>>> Transfer(Guid shopId, Guid orderId, RecordRefundTransferRequest request, CancellationToken ct)
        => Ok(ApiResponse<OrderRefundResponse>.Ok(await _shopOrders.TransferAsync(UserId(), shopId, orderId, request.TransferReference, ct)));
    // Read the current actor from authenticated claims rather than client input.
    private Guid UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
