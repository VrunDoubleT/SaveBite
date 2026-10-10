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

// Manage shop staff members and their activity logs.
[ApiController]
[Route("api/shop-staff")]
public sealed class ShopStaffController(IShopStaffService shopStaffService) : ControllerBase
{
    private readonly IShopStaffService _shopStaffService = shopStaffService;

    [HttpGet("shop")]
    [StoreOwnerAccess]
    public async Task<IActionResult> GetStaffs(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _shopStaffService.GetShopStaffsAsync(
            GetCurrentUserId(),
            keyword,
            page,
            pageSize,
            cancellationToken
        );

        return Ok(
            ApiResponse<PagedResult<ShopStaffResponse>>.Ok(
                result,
                "Staffs retrieved successfully"
            )
        );
    }

    [HttpPut("{staffId:guid}")]
    [AccountAccess]
    public async Task<IActionResult> UpdateStaff(
        Guid staffId,
        [FromBody] UpdateStaffInfoRequest request,
        CancellationToken cancellationToken)
    {
        await _shopStaffService.UpdateStaffInfoAsync(
            GetCurrentUserId(),
            staffId,
            request,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok("Staff updated successfully")
        );
    }

    [HttpDelete("{staffId:guid}")]
    [StoreOwnerAccess]
    public async Task<IActionResult> RemoveStaff(
        Guid staffId,
        CancellationToken cancellationToken)
    {
        await _shopStaffService.RemoveStaffAsync(
            GetCurrentUserId(),
            staffId,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok("Staff removed successfully")
        );
    }

    [HttpGet("{staffId:guid}/activity-logs")]
    [StoreOwnerAnyStatusAccess]
    public async Task<IActionResult> GetStaffActivityLogs(
        Guid staffId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _shopStaffService.GetStaffActivityLogsAsync(
            GetCurrentUserId(),
            staffId,
            page,
            pageSize,
            cancellationToken
        );

        return Ok(
            ApiResponse<PagedResult<StaffActivityLogResponse>>.Ok(
                result,
                "Activity logs retrieved successfully"
            )
        );
    }

    [HttpGet("me/shops")]
    [StaffAnyStatusAccess]
    public async Task<IActionResult> GetAssociatedShops(
        CancellationToken cancellationToken)
    {
        var result = await _shopStaffService
            .GetAssociatedShopsAsync(
                GetCurrentUserId(),
                cancellationToken);

        var message = result.Any()
            ? "Shops retrieved successfully"
            : "No associated shops found";

        return Ok(
            ApiResponse<IEnumerable<AssociatedShopResponse>>
                .Ok(result, message)
        );
    }

    [HttpGet("me/shops/{shopId:guid}")]
    [StaffAnyStatusAccess]
    public async Task<IActionResult> GetShopAndStaffInfo(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        var result = await _shopStaffService.GetShopAndStaffInfoAsync(
            GetCurrentUserId(),
            shopId,
            cancellationToken);

        return Ok(
            ApiResponse<StaffShopInfoResponse>
                .Ok(
                    result,
                    "Shop and staff information retrieved successfully"
                )
        );
    }

    [HttpDelete("me/shops/{shopId:guid}")]
    [StaffAnyStatusAccess]
    public async Task<IActionResult> LeaveShop(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        await _shopStaffService.LeaveShopAsync(
            GetCurrentUserId(),
            shopId,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok(
                "You have left the shop successfully"
            )
        );
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id)
            ? id
            : throw AppException.Unauthorized();
    }
}
