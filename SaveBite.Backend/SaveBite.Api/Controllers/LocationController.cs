using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Caching;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/location")]
public sealed class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly IConnectionMultiplexer _redis;

    public LocationController(ILocationService locationService, IConnectionMultiplexer redis)
    {
        _locationService = locationService;
        _redis = redis;
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

    [HttpPost("search-filter")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> SaveSearchFilter(
        [FromBody] SaveSearchFilterRequest request,
        CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId();
        var key = userId.HasValue
            ? RedisKeys.UserSearchFilter(userId.Value)
            : RedisKeys.GuestSearchFilter(request.SessionKey ?? "anonymous");

        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(request);
        await db.StringSetAsync(key, json, TimeSpan.FromHours(24));

        return Ok(ApiResponse.Ok("Search filter cached in Redis successfully."));
    }

    [HttpGet("search-filter")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SaveSearchFilterRequest?>>> GetSearchFilter(
        [FromQuery] string? sessionKey,
        CancellationToken cancellationToken)
    {
        var userId = TryGetCurrentUserId();
        var key = userId.HasValue
            ? RedisKeys.UserSearchFilter(userId.Value)
            : RedisKeys.GuestSearchFilter(sessionKey ?? "anonymous");

        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
        {
            return Ok(ApiResponse<SaveSearchFilterRequest?>.Ok(null));
        }

        try
        {
            var result = JsonSerializer.Deserialize<SaveSearchFilterRequest>(json.ToString());
            return Ok(ApiResponse<SaveSearchFilterRequest?>.Ok(result));
        }
        catch
        {
            return Ok(ApiResponse<SaveSearchFilterRequest?>.Ok(null));
        }
    }

    private Guid GetRequiredUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }

    private Guid? TryGetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
