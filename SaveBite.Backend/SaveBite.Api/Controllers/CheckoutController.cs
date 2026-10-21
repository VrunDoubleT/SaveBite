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

// Prepare a checkout quote and place one shop's order.
[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkout;

    public CheckoutController(ICheckoutService checkout)
    {
        _checkout = checkout;
    }

    // Load one shop's current items, totals, deposit policy, and payment availability before GPS is supplied.
    [HttpGet("context")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutContextResponse>>> Context([FromQuery] Guid shopId, CancellationToken ct)
        => Ok(ApiResponse<CheckoutContextResponse>.Ok(await _checkout.ContextAsync(UserId(), shopId, ct)));

    // Validate GPS and create a Redis quote valid for up to 60 seconds with route, deadlines, and deposit details.
    [HttpPost("quotes")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutQuoteResponse>>> Quote(CheckoutQuoteRequest request, CancellationToken ct)
        => Ok(ApiResponse<CheckoutQuoteResponse>.Ok(await _checkout.QuoteAsync(UserId(), request, ct)));

    // Revalidate the Redis quote, reuse an existing order on retry, or create one shop's order and reserve stock.
    [HttpPost("orders")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CheckoutOrderResponse>>> Place(PlaceCheckoutOrderRequest request, CancellationToken ct)
        => Ok(ApiResponse<CheckoutOrderResponse>.Ok(await _checkout.PlaceAsync(UserId(), request, ct)));

    // Read the current actor from authenticated claims rather than client input.
    private Guid UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
