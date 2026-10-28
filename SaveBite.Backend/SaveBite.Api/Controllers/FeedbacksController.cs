using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    private readonly ICloudinaryService _cloudinaryService;

    public FeedbacksController(IFeedbackService feedbackService, ICloudinaryService cloudinaryService)
    {
        _feedbackService = feedbackService;
        _cloudinaryService = cloudinaryService;
    }

    // Add feedback (Customer)
    [HttpPost]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<FeedbackResponses.FeedbackItemResponse>>> AddFeedback(
        [FromBody] FeedbackRequests.CreateFeedbackRequest request,
        CancellationToken ct)
    {
        var result = await _feedbackService.AddFeedbackAsync(GetAuthenticatedUserId(), request, ct);
        return Ok(ApiResponse<FeedbackResponses.FeedbackItemResponse>.Ok(result, "Feedback submitted successfully."));
    }

    // Upload feedback image (Cloudinary)
    [HttpPost("images")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw AppException.BadRequest("Image file is required.");

        var result = await _cloudinaryService.UploadImageAsync(file, ct);
        return Ok(ApiResponse<string>.Ok(result.Url, "Image uploaded successfully."));
    }

    // View feedback history (Customer) - supports deep-linking via feedbackId
    [HttpGet("history")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? feedbackId = null,
        CancellationToken ct = default)
    {
        var result = await _feedbackService.GetMyFeedbackHistoryAsync(GetAuthenticatedUserId(), page, pageSize, feedbackId, ct);
        return Ok(ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>.Ok(result, "Retrieved feedback history successfully."));
    }

    // Edit feedback (Customer)
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

    // Delete feedback (Customer soft delete)
    [HttpDelete("{feedbackId:guid}")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse>> DeleteFeedback(
        Guid feedbackId,
        CancellationToken ct)
    {
        await _feedbackService.DeleteFeedbackAsync(GetAuthenticatedUserId(), feedbackId, ct);
        return Ok(ApiResponse.Ok("Feedback deleted successfully."));
    }

    // View feedback for shop (Store Owner & Staff)
    [HttpGet("shop")]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>> GetShopFeedbacks(
        [FromQuery] FeedbackRequests.ShopFeedbacksQueryRequest request,
        CancellationToken ct)
    {
        var result = await _feedbackService.GetShopFeedbacksAsync(GetAuthenticatedUserId(), request, ct);
        return Ok(ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>.Ok(result, "Retrieved shop feedbacks successfully."));
    }

    // View feedback for admin (Admin)
    [HttpGet("admin")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>>> GetAdminFeedbacks(
        [FromQuery] FeedbackRequests.ShopFeedbacksQueryRequest request,
        CancellationToken ct)
    {
        var result = await _feedbackService.GetAdminFeedbacksAsync(GetAuthenticatedUserId(), request, ct);
        return Ok(ApiResponse<PagedResult<FeedbackResponses.FeedbackItemResponse>>.Ok(result, "Retrieved feedbacks successfully."));
    }

    // Reply feedback (Store Owner, Staff, or Admin)
    [HttpPost("{feedbackId:guid}/reply")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<FeedbackResponses.FeedbackReplyResponse>>> ReplyFeedback(
        Guid feedbackId,
        [FromBody] FeedbackRequests.CreateFeedbackReplyRequest request,
        CancellationToken ct)
    {
        var result = await _feedbackService.ReplyFeedbackAsync(GetAuthenticatedUserId(), feedbackId, request, ct);
        return Ok(ApiResponse<FeedbackResponses.FeedbackReplyResponse>.Ok(result, "Reply submitted successfully."));
    }

    // Edit reply (Store Owner, Staff, or Admin)
    [HttpPut("{feedbackId:guid}/reply")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<FeedbackResponses.FeedbackReplyResponse>>> EditReply(
        Guid feedbackId,
        [FromBody] FeedbackRequests.UpdateFeedbackReplyRequest request,
        CancellationToken ct)
    {
        var result = await _feedbackService.EditFeedbackReplyAsync(GetAuthenticatedUserId(), feedbackId, request, ct);
        return Ok(ApiResponse<FeedbackResponses.FeedbackReplyResponse>.Ok(result, "Reply updated successfully."));
    }

    // Delete reply (Store Owner, Staff, or Admin)
    [HttpDelete("{feedbackId:guid}/reply")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteReply(
        Guid feedbackId,
        CancellationToken ct)
    {
        await _feedbackService.DeleteFeedbackReplyAsync(GetAuthenticatedUserId(), feedbackId, ct);
        return Ok(ApiResponse.Ok("Reply deleted successfully."));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }
}
