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

[ApiController]
[Route("api/feedbacks")]
public sealed class FeedbacksController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbacksController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    // Add feedback
    [HttpPost]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<FeedbackResponses.FeedbackItemResponse>>> AddFeedback(
        [FromBody] FeedbackRequests.CreateFeedbackRequest request,
        CancellationToken ct)
    {
        var result = await _feedbackService.AddFeedbackAsync(GetAuthenticatedUserId(), request, ct);
        return Ok(ApiResponse<FeedbackResponses.FeedbackItemResponse>.Ok(result, "Feedback submitted successfully."));
    }

    // View feedback history
    [HttpGet("history")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _feedbackService.GetMyFeedbackHistoryAsync(GetAuthenticatedUserId(), page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>.Ok(result, "Retrieved feedback history successfully."));
    }

    // Edit feedback
    [HttpPut("{feedbackId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<FeedbackResponses.FeedbackItemResponse>>> EditFeedback(
        Guid feedbackId,
        [FromBody] FeedbackRequests.UpdateFeedbackRequest request,
        CancellationToken ct)
    {
        var result = await _feedbackService.EditFeedbackAsync(GetAuthenticatedUserId(), feedbackId, request, ct);
        return Ok(ApiResponse<FeedbackResponses.FeedbackItemResponse>.Ok(result, "Feedback updated successfully."));
    }

    // Delete feedback
    [HttpDelete("{feedbackId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse>> DeleteFeedback(
        Guid feedbackId,
        CancellationToken ct)
    {
        await _feedbackService.DeleteFeedbackAsync(GetAuthenticatedUserId(), feedbackId, ct);
        return Ok(ApiResponse.Ok("Feedback deleted successfully."));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }
}
