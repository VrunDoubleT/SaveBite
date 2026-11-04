using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api")]
[CustomerActiveAccess]
public sealed class ShopFollowsController(IShopFollowService follows) : ControllerBase
{
    [HttpPut("shops/{shopId:guid}/follow")]
    public async Task<ActionResult<ApiResponse<ShopFollowResponse>>> Follow(Guid shopId, CancellationToken ct)
        => Ok(ApiResponse<ShopFollowResponse>.Ok(await follows.FollowAsync(CustomerId(), shopId, ct)));

    [HttpDelete("shops/{shopId:guid}/follow")]
    public async Task<IActionResult> Unfollow(Guid shopId, CancellationToken ct)
    {
        await follows.UnfollowAsync(CustomerId(), shopId, ct);
        return Ok(ApiResponse.Ok("Shop unfollowed."));
    }

    [HttpPut("shops/{shopId:guid}/follow/notifications")]
    public async Task<ActionResult<ApiResponse<ShopFollowResponse>>> UpdateSettings(Guid shopId, ShopNotificationSettingsRequest request, CancellationToken ct)
        => Ok(ApiResponse<ShopFollowResponse>.Ok(await follows.UpdateSettingsAsync(CustomerId(), shopId,
            request.NotificationStatus ?? throw AppException.BadRequest("NotificationStatus is required."), ct)));

    [HttpGet("me/followed-shops")]
    public async Task<ActionResult<ApiResponse<PagedResult<ShopFollowResponse>>>> Get([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<ShopFollowResponse>>.Ok(await follows.GetFollowedShopsAsync(CustomerId(), page, pageSize, ct)));

    private Guid CustomerId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
