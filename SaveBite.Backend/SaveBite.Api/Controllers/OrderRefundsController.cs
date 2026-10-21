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

// Let active customers view their own refunds and verify a bank destination before a shop refund transfer.
[ApiController]
[Route("api/orders/{orderId:guid}/refund")]
[CustomerAccess]
public sealed class OrderRefundsController : ControllerBase
{
    private readonly IShopOrderService _service;

    public OrderRefundsController(IShopOrderService service)
    {
        _service = service;
    }

    // Load the customer's own order refund and settle overdue shop processing when needed.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<OrderRefundResponse?>>> Get(Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<OrderRefundResponse?>.Ok(await _service.CustomerRefundAsync(UserId(), orderId, ct)));
    
    // Store the customer's refund bank details and email a confirmation code that expires after 15 minutes.
    [HttpPost("destination")]
    public async Task<ActionResult<ApiResponse<OrderRefundResponse>>> Destination(Guid orderId, RefundDestinationRequest request, CancellationToken ct)
        => Ok(ApiResponse<OrderRefundResponse>.Ok(await _service.DestinationAsync(UserId(), orderId, request, ct)));
    
    // Verify the email code within its expiry and attempt limit; repeated successful confirmation has no extra effect.
    [HttpPost("verify")]
    public async Task<ActionResult<ApiResponse<OrderRefundResponse>>> Verify(Guid orderId, VerifyRefundDestinationRequest request, CancellationToken ct)
        => Ok(ApiResponse<OrderRefundResponse>.Ok(await _service.VerifyDestinationAsync(UserId(), orderId, request.Code, ct)));
    
    // Read the customer ID from authenticated claims.
    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
        ? id : throw AppException.Unauthorized();
}
