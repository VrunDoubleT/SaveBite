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

// Manage the authenticated customer's cart, cursor pages, and mutation responses.
[ApiController]
[Route("api/cart")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(
        ICartService cartService)
    {
        _cartService = cartService;
    }

    // Load the next cursor page of shop groups without splitting a shop across pages.
    [HttpGet("page")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CartPageResponse>>> Page([FromQuery] string? cursor, [FromQuery] int pageSize = 5, CancellationToken cancellationToken = default)
        => Ok(ApiResponse<CartPageResponse>.Ok(await _cartService.GetPageAsync(UserId(), cursor, pageSize, cancellationToken)));

    // Return whole-cart totals and the distinct Flash Deal count used by the cart badge.
    [HttpGet("summary")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CartSummaryResponse>>> Summary(CancellationToken cancellationToken)
        => Ok(ApiResponse<CartSummaryResponse>.Ok(await _cartService.GetSummaryAsync(UserId(), cancellationToken)));

    // Add a purchasable Flash Deal variant to the customer's cart without reserving stock.
    [HttpPost("items")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CartResponse>>> Add(AddCartItemRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<CartResponse>.Ok(await _cartService.AddItemAsync(UserId(), request, cancellationToken), "Item added to cart."));

    // Update the quantity of an item owned by the authenticated customer.
    [HttpPut("items/{itemId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CartMutationResponse>>> Update(Guid itemId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<CartMutationResponse>.Ok(await _cartService.UpdateQuantityAsync(UserId(), itemId, request, cancellationToken), "Quantity updated."));

    // Remove only the selected item from the authenticated customer's cart.
    [HttpDelete("items/{itemId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CartMutationResponse>>> Remove(Guid itemId, CancellationToken cancellationToken)
        => Ok(ApiResponse<CartMutationResponse>.Ok(await _cartService.RemoveItemAsync(UserId(), itemId, cancellationToken), "Item removed from cart."));

    // Read the customer ID from authenticated JWT claims rather than client input.
    private Guid UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
