using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/location")]
public sealed class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpPost("gps")]
    [CustomerOrStaffOrStoreOwnerAccess]
    public async Task<ActionResult<ApiResponse<long>>> UpdateGps(
        [FromBody] UpdateGpsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        var version = await _locationService.UpdateGpsAsync(
            userId,
            request.Latitude,
            request.Longitude,
            request.Accuracy,
            cancellationToken);

        return Ok(ApiResponse<long>.Ok(version, "GPS location updated successfully."));
    }

    [HttpDelete("gps")]
    [CustomerOrStaffOrStoreOwnerAccess]
    public async Task<ActionResult<ApiResponse>> RemoveGps(CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId();
        await _locationService.RemoveGpsAsync(userId, cancellationToken);
        return Ok(ApiResponse.Ok("GPS location removed successfully."));
    }

    private Guid GetRequiredUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}
