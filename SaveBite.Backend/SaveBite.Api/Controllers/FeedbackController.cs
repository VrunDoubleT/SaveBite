
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/feedback")]
public sealed class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedback;

    public FeedbackController(IFeedbackService feedback)
    {
        _feedback = feedback;
    }

    // Manage Feedbacks.
    [HttpGet]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<FeedbackResponse>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] FeedbackStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _feedback.ListAsync(page, pageSize, status, search, cancellationToken);
        return Ok(ApiResponse<PagedResult<FeedbackResponse>>.Ok(result, "Feedback retrieved."));
    }

    // Manage Feedbacks.
    [HttpDelete("{feedbackId:guid}")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<ModerationActionResponse>>> Remove(Guid feedbackId, [FromBody] HideViolatingFeedbackRequest request, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var result = await _feedback.RemoveAsync(adminId, feedbackId, request, cancellationToken);
        return Ok(ApiResponse<ModerationActionResponse>.Ok(result, "Feedback removed."));
    }

    // Manage Feedbacks.
    [HttpPost("{feedbackId:guid}/restore")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<ModerationActionResponse>>> Restore(Guid feedbackId, [FromBody] RestoreFeedbackRequest request, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var result = await _feedback.RestoreAsync(adminId, feedbackId, request, cancellationToken);
        return Ok(ApiResponse<ModerationActionResponse>.Ok(result, "Feedback restored."));
    }

    private Guid GetAdminId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(raw, out var adminId))
            throw AppException.Unauthorized("Unable to identify user.");

        return adminId;
    }
}
