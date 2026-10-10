using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/shops")]
[AllowAnonymous]
public sealed class ShopsController : ControllerBase
{
    private readonly IShopViewService _shopViewService;

    public ShopsController(IShopViewService shopViewService)
    {
        _shopViewService = shopViewService;
    }


    [HttpGet("nearby")]
    public async Task<ActionResult<ApiResponse<PagedResult<NearbyShopResponse>>>> GetNearby(
        [FromQuery] NearbyShopsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shopViewService.GetNearbyShopsAsync(request, cancellationToken);
        var message = result.TotalItems > 0
            ? null
            : $"No shops found within a {request.RadiusInKm} km radius of your location.";

        return Ok(ApiResponse<PagedResult<NearbyShopResponse>>.Ok(result, message));
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ShopProfileResponse>>> GetProfile(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw AppException.BadRequest("Shop ID is invalid.");
        }

        var profile = await _shopViewService.GetShopProfileAsync(id, cancellationToken);
        return Ok(ApiResponse<ShopProfileResponse>.Ok(profile));
    }


    [HttpGet("{id:guid}/reviews")]
    public async Task<ActionResult<ApiResponse<StoreReviewsSummaryResponse>>> GetReviews(
        Guid id,
        [FromQuery] StoreReviewsQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw AppException.BadRequest("Shop ID is invalid.");
        }

        var reviews = await _shopViewService.GetStoreReviewsAsync(id, request, cancellationToken);
        var message = reviews.TotalReviews > 0
            ? null
            : "This shop currently has no reviews.";

        return Ok(ApiResponse<StoreReviewsSummaryResponse>.Ok(reviews, message));
    }
}
