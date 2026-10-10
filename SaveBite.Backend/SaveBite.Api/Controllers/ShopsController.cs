using SaveBite.Backend.Authorization;
using System.Security.Claims;
using SaveBite.Backend.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/shops")]
public sealed class ShopsController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopsController(
        IShopService shopService)
    {
        _shopService = shopService;
    }

    [HttpGet]
    [GuestAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<NearbyShopResponse>>>> GetNearby(
        [FromQuery] NearbyShopsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shopService.GetNearbyShopsAsync(request, cancellationToken);
        var message = result.TotalItems > 0
            ? null
            : $"No shops found within a {request.RadiusInKm} km radius of your location.";

        return Ok(ApiResponse<PagedResult<NearbyShopResponse>>.Ok(result, message));
    }

    [HttpGet("{id:guid}")]
    [GuestAccess]
    public async Task<ActionResult<ApiResponse<ShopProfileResponse>>> GetProfile(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw AppException.BadRequest("Shop ID is invalid.");
        }

        var profile = await _shopService.GetShopProfileAsync(id, cancellationToken);
        return Ok(ApiResponse<ShopProfileResponse>.Ok(profile));
    }

    [HttpGet("{id:guid}/reviews")]
    [GuestAccess]
    public async Task<ActionResult<ApiResponse<StoreReviewsSummaryResponse>>> GetReviews(
        Guid id,
        [FromQuery] StoreReviewsQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw AppException.BadRequest("Shop ID is invalid.");
        }

        var reviews = await _shopService.GetStoreReviewsAsync(id, request, cancellationToken);
        var message = reviews.TotalReviews > 0
            ? null
            : "This shop currently has no reviews.";

        return Ok(ApiResponse<StoreReviewsSummaryResponse>.Ok(reviews, message));
    }

    [HttpGet("me")]
    [CustomerAnyStatusAccess]
    public async Task<IActionResult> GetMyShop(
        CancellationToken cancellationToken)
    {
        var result = await _shopService.GetOwnerShopAsync(
            GetCurrentUserId(),
            cancellationToken
        );

        return Ok(
            ApiResponse<ShopSummaryResponse>.Ok(
                result,
                "Shop retrieved successfully"
            )
        );
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw AppException.Unauthorized();
    }
}
