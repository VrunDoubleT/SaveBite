using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
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
        var userId = TryGetCurrentUserId();
        var result = await _flashDealService.GetNearbyAsync(request, userId, cancellationToken);

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
    private Guid? TryGetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }
    
    // 1. View Flash Deals for Shop
    [HttpGet("flash-deals/shop")]
    [StoreOwnerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<ShopFlashDealListItemResponse>>>> GetShopDeals(
        [FromQuery] ShopFlashDealQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _flashDealService.GetShopDealsAsync(GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiResponse<PagedResult<ShopFlashDealListItemResponse>>.Ok(result));
    }

    // 2. View Flash Deal Details for Shop
    [HttpGet("flash-deals/shop/{id:guid}")]
    [StoreOwnerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<ShopFlashDealDetailResponse>>> GetShopDealDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw AppException.BadRequest("Flash deal ID is invalid.");
        var result = await _flashDealService.GetShopDealDetailAsync(GetRequiredUserId(), id, cancellationToken);
        return Ok(ApiResponse<ShopFlashDealDetailResponse>.Ok(result));
    }

    // Retrieve active shop products for flash deal creation dropdown.
    [HttpGet("flash-deals/eligible-products")]
    [StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<List<ShopEligibleProductResponse>>>> GetEligibleProducts(
        CancellationToken cancellationToken)
    {
        var result = await _flashDealService.GetShopEligibleProductsAsync(GetRequiredUserId(), cancellationToken);
        return Ok(ApiResponse<List<ShopEligibleProductResponse>>.Ok(result));
    }

    // 3. Create Flash Deal
    [HttpPost("flash-deals")]
    [StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopFlashDealDetailResponse>>> Create(
        [FromBody] CreateFlashDealRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _flashDealService.CreateDealAsync(GetRequiredUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetShopDealDetail), new { id = result.Id }, 
            ApiResponse<ShopFlashDealDetailResponse>.Ok(result, "Flash deal created successfully."));
    }

    // 4. Update Flash Deal
    [HttpPut("flash-deals/{id:guid}")]
    [StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse<ShopFlashDealDetailResponse>>> Update(
        Guid id,
        [FromBody] UpdateFlashDealRequest request,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw AppException.BadRequest("Flash deal ID is invalid.");
        var result = await _flashDealService.UpdateDealAsync(GetRequiredUserId(), id, request, cancellationToken);
        return Ok(ApiResponse<ShopFlashDealDetailResponse>.Ok(result, "Flash deal updated successfully."));
    }
    // 5. Cancel Flash Deal
    [HttpPost("flash-deals/{id:guid}/cancellations")]
    [StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse>> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw AppException.BadRequest("Flash deal ID is invalid.");
        await _flashDealService.CancelDealAsync(GetRequiredUserId(), id, cancellationToken);
        return Ok(ApiResponse.Ok("Flash deal cancelled successfully."));
    }
    // 6. Hide Flash Deal
    [HttpPost("flash-deals/{id:guid}/hides")]
    [StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse>> Hide(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw AppException.BadRequest("Flash deal ID is invalid.");
        await _flashDealService.HideDealAsync(GetRequiredUserId(), id, cancellationToken);
        return Ok(ApiResponse.Ok("Flash deal hidden successfully."));
    }
    // 7. Unhide Flash Deal
    [HttpDelete("flash-deals/{id:guid}/hides")]
    [StoreOwnerActiveAccess]
    public async Task<ActionResult<ApiResponse>> Unhide(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw AppException.BadRequest("Flash deal ID is invalid.");
        await _flashDealService.UnhideDealAsync(GetRequiredUserId(), id, cancellationToken);
        return Ok(ApiResponse.Ok("Flash deal unhidden successfully."));
    }
    private Guid GetRequiredUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
