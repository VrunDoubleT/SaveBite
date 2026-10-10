using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/admin/shop-applications")]
[AdminAccess]
public sealed class AdminShopApplicationsController : ControllerBase
{
    private readonly IShopApplicationService _service;

    public AdminShopApplicationsController(IShopApplicationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShopApplicationResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var applications = await _service.GetAllForAdminAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShopApplicationResponse>>.Ok(applications, "Shop applications retrieved successfully."));
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> GetById(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await _service.GetByIdForAdminAsync(applicationId, cancellationToken);
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(application, "Shop application retrieved successfully."));
    }

    [HttpPost("{applicationId:guid}/review")]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> Review(
        Guid applicationId,
        [FromBody] ReviewShopApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdValue, out var adminId)) throw AppException.Unauthorized();

        var application = await _service.ReviewAsync(adminId, applicationId, request, cancellationToken);
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(application, $"Shop application {application.Status.ToLowerInvariant()} successfully."));
    }
}
