using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

// Create customer payment links and receive verified provider notifications.
[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ICheckoutPaymentService _payments;
    private readonly IPaymentHistoryService _history;

    public PaymentsController(ICheckoutPaymentService payments, IPaymentHistoryService history)
    {
        _payments = payments;
        _history = history;
    }

    // List only the active customer's own payment records with numbered pagination and an optional status filter.
    [HttpGet]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerPaymentResponse>>>> Get(int page = 1, int pageSize = 10, string? status = null, CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<CustomerPaymentResponse>>.Ok(await _history.PageAsync(UserId(), page, pageSize, status, ct)));

    // Return one owned payment together with its order, purchased products and recorded totals.
    [HttpGet("{paymentId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CustomerPaymentDetailResponse>>> Details(Guid paymentId, CancellationToken ct)
        => Ok(ApiResponse<CustomerPaymentDetailResponse>.Ok(await _history.GetAsync(UserId(), paymentId, ct)));

    // Create or resume the order's required initial payment link using its recorded full-payment or deposit amount.
    [HttpPost("orders/{orderId:guid}/payment-link")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> Payment(Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<CheckoutOrderResponse>.Ok(await _payments.StartAsync(UserId(), orderId, ct)));

    // Explicitly query the provider and apply verified payment changes for the customer's own order.
    [HttpPost("orders/{orderId:guid}/reconcile")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> Reconcile(Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<CheckoutOrderResponse>.Ok(await _payments.ReconcileAsync(UserId(), orderId, ct)));

    // Verify the PayOS webhook signature and query the provider before applying payment and inventory changes.
    [HttpPost("payos/webhook")]
    [HttpPost("~/api/checkout/payos/webhook")]
    [AllowAnonymous]
    [RequestSizeLimit(65536)]
    public async Task<ActionResult> Webhook([FromBody] JsonElement body, CancellationToken ct)
    {
        await _payments.WebhookAsync(body, ct);
        return Ok(new { code = "00", desc = "success" });
    }

    // Read the current actor from authenticated claims rather than client input.
    private Guid UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
