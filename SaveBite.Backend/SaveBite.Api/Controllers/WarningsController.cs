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
[Route("api/warnings")]
public sealed class WarningsController : ControllerBase
{
    private readonly IWarningService _warningService;

    public WarningsController(IWarningService warningService)
    {
        _warningService = warningService;
    }

    // View Warning History.
    [HttpGet]
    [StoreOwnerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<WarningResponse>>>> History([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        var ownerUserId = GetRequiredCurrentUserId();
        var result = await _warningService.GetHistoryAsync(ownerUserId, page, pageSize, cancellationToken, status);
        return Ok(ApiResponse<PagedResult<WarningResponse>>.Ok(result, "Warning history retrieved successfully."));
    }

    // View Warning Details.
    [HttpGet("{warningId:guid}")]
    [StoreOwnerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<WarningResponse>>> Details(Guid warningId, CancellationToken cancellationToken)
    {
        if (warningId == Guid.Empty)
            throw AppException.BadRequest("Warning ID is invalid.");

        var ownerUserId = GetRequiredCurrentUserId();
        var result = await _warningService.GetDetailsAsync(ownerUserId, warningId, cancellationToken);
        return Ok(ApiResponse<WarningResponse>.Ok(result, "Warning details retrieved successfully."));
    }

    // Admin Method: Issue a violation warning for a product's shop.
    [HttpPost("admin/products/{productId:guid}")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<ModerationActionResponse>>> Issue(Guid productId, [FromBody] IssueViolationWarningRequest request, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
            throw AppException.BadRequest("Product ID is invalid.");

        var result = await _warningService.IssueWarningAsync(GetRequiredCurrentUserId(), productId, request, cancellationToken);
        return Ok(ApiResponse<ModerationActionResponse>.Ok(result, "Violation warning issued successfully."));
    }

    private Guid? TryGetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private Guid GetRequiredCurrentUserId()
    {
        var id = TryGetCurrentUserId();
        if (!id.HasValue || id.Value == Guid.Empty)
            throw AppException.Unauthorized("Unable to identify the authenticated user.");

        return id.Value;
    }
}
