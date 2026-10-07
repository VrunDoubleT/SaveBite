using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/flash-deals")]
[AllowAnonymous] 
public sealed class FlashDealsController : ControllerBase
{
    private readonly IFlashDealService _flashDealService;

    public FlashDealsController(IFlashDealService flashDealService)
    {
        _flashDealService = flashDealService;
    }


    [HttpGet("shop/{shopId:guid}")]
    public async Task<ActionResult<ApiResponse<List<FlashDealResponse>>>> GetByShop(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        if (shopId == Guid.Empty)
        {
            throw AppException.BadRequest("Mã cửa hàng không hợp lệ.");
        }

        var deals = await _flashDealService.GetByShopIdAsync(shopId, cancellationToken);
        return Ok(ApiResponse<List<FlashDealResponse>>.Ok(deals));
    }


    [HttpGet("nearby")]
    public async Task<ActionResult<ApiResponse<List<FlashDealResponse>>>> GetNearby(
        [FromQuery] NearbyFlashDealsRequest request,
        CancellationToken cancellationToken)
    {
        var deals = await _flashDealService.GetNearbyAsync(request, cancellationToken);

        var message = deals.Any()
            ? null
            : $"Không tìm thấy deal nào trong bán kính {request.RadiusInKm} km từ vị trí của bạn.";
        return Ok(ApiResponse<List<FlashDealResponse>>.Ok(deals, message));
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FlashDealResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw AppException.BadRequest("Mã flash deal không hợp lệ.");
        }

        var deal = await _flashDealService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<FlashDealResponse>.Ok(deal));
    }


    [HttpPost("seed-redis")]
    public async Task<ActionResult<ApiResponse>> SeedRedis(CancellationToken cancellationToken)
    {
        await _flashDealService.SeedSampleDealsAsync(cancellationToken);
        return Ok(ApiResponse.Ok("Successfully downloaded Flash Deal data templates to Redis!"));
    }
}