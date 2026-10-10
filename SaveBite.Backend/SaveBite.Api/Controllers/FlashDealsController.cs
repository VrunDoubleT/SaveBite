using SaveBite.Backend.Authorization;
using SaveBite.Backend.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

// Retrieve flash deals by location, identifier, or shop.
[ApiController]
[Route("api")]
public sealed class FlashDealsController : ControllerBase
{
    private readonly IFlashDealService _flashDealService;

    public FlashDealsController(IFlashDealService flashDealService)
    {
        _flashDealService = flashDealService;
    }

    [HttpGet("flash-deals")]
    [GuestAccess]
    public async Task<ActionResult<ApiResponse<FlashDealCursorListResponse>>> GetNearby(
        [FromQuery] FlashDealCursorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _flashDealService.GetNearbyAsync(request, cancellationToken);

        var message = result.Deals.Count > 0
            ? null
            : $"No flash deals found within a {request.RadiusInKm} km radius of your location.";
        return Ok(ApiResponse<FlashDealCursorListResponse>.Ok(result, message));
    }

    [HttpGet("flash-deals/{id:guid}")]
    [GuestAccess]
    public async Task<ActionResult<ApiResponse<FlashDealResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw AppException.BadRequest("Flash deal ID is invalid.");
        }

        var deal = await _flashDealService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<FlashDealResponse>.Ok(deal));
    }

    [HttpGet("shops/{shopId:guid}/flash-deals")]
    [GuestAccess]
    public async Task<ActionResult<ApiResponse<List<FlashDealResponse>>>> GetByShop(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        if (shopId == Guid.Empty)
        {
            throw AppException.BadRequest("Shop ID is invalid.");
        }

        var deals = await _flashDealService.GetByShopIdAsync(shopId, cancellationToken);
        return Ok(ApiResponse<List<FlashDealResponse>>.Ok(deals));
    }
}
