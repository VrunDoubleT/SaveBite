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
            throw AppException.BadRequest("Shop ID is invalid.");
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
            : $"No flash deals found within a {request.RadiusInKm} km radius of your location.";
        return Ok(ApiResponse<List<FlashDealResponse>>.Ok(deals, message));
    }


    [HttpGet("{id:guid}")]
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


    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetCategories(
        [FromServices] SaveBite.Backend.Data.AppDbContext context,
        CancellationToken cancellationToken)
    {
        var categories = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                System.Linq.Queryable.Select(
                    System.Linq.Queryable.OrderBy(
                        System.Linq.Queryable.Where(
                            Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(context.Categories),
                            c => c.IsActive
                        ),
                        c => c.Name
                    ),
                    c => new CategoryResponse(
                        c.Id,
                        c.Name,
                        c.Description,
                        c.ImageUrl,
                        c.IsActive
                    )
                ),
                cancellationToken
            );

        return Ok(ApiResponse<List<CategoryResponse>>.Ok(categories));
    }
}