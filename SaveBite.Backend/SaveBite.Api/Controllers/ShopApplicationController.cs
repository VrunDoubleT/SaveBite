using SaveBite.Backend.Models.Common;
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
[Route("api/shop-applications")]
public sealed class ShopApplicationController : ControllerBase
{
    private readonly IShopApplicationService _service;
    public ShopApplicationController(IShopApplicationService service) => _service = service;

    [HttpGet("me/latest")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> GetMyShopApplication(CancellationToken cancellationToken)
    {
        var application = await _service.GetMyApplicationAsync(GetAuthenticatedUserId(), cancellationToken);
        if (application is null) return NotFound(ApiResponse<ShopApplicationResponse>.Fail("No shop application was found."));
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(application, "Shop application retrieved successfully."));
    }

    [HttpGet("me")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShopApplicationResponse>>>> GetMyShopApplicationHistory(CancellationToken cancellationToken)
    {
        var applications = await _service.GetMyApplicationHistoryAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShopApplicationResponse>>.Ok(applications, "Shop application history retrieved successfully."));
    }

    [HttpPost]
    [CustomerAccess]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> Create(
        [FromForm] CreateShopApplicationRequest request,
        [FromForm] IFormFile? logo,
        [FromForm] IFormFile? coverImage,
        [FromForm] List<IFormFile>? documents,
        [FromForm] List<string>? documentTypes,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(GetAuthenticatedUserId(), request, logo, coverImage, documents ?? [], documentTypes ?? [], cancellationToken);
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(result, "Shop application submitted successfully."));
    }

    [HttpPost("{applicationId:guid}/revisions")]
    [CustomerAccess]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> Resubmit(
        Guid applicationId,
        [FromForm] ResubmitShopApplicationRequest request,
        [FromForm] IFormFile? logo,
        [FromForm] IFormFile? coverImage,
        [FromForm] List<IFormFile>? documents,
        [FromForm] List<string>? documentTypes,
        CancellationToken cancellationToken)
    {
        request.ApplicationId = applicationId;
        var result = await _service.ResubmitAsync(GetAuthenticatedUserId(), request, logo, coverImage, documents ?? [], documentTypes ?? [], cancellationToken);
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(result, "Shop application resubmitted successfully."));
    }

    [HttpPost("{applicationId:guid}/cancellations")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid applicationId, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(GetAuthenticatedUserId(), applicationId, cancellationToken);
        return Ok(ApiResponse.Ok("Shop application cancelled successfully."));
    }

    [HttpGet]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShopApplicationResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var applications = await _service.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShopApplicationResponse>>.Ok(applications, "Shop applications retrieved successfully."));
    }

    [HttpGet("{applicationId:guid}")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> GetById(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await _service.GetByIdAsync(applicationId, cancellationToken);
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(application, "Shop application retrieved successfully."));
    }

    [HttpPost("{applicationId:guid}/reviews")]
    [AdminAccess]
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

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }
}
