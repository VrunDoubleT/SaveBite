using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
namespace SaveBite.Backend.Uploads;
public sealed record CreateUploadSession(string Purpose, Guid? TargetId);
[ApiController, Route("api/uploads"), Authorize(Policy = AuthorizationPolicies.AccountOnly)]
public sealed class UploadAssetsController(UploadService uploads, AppDbContext db) : ControllerBase
{
    Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? throw AppException.Unauthorized());
    [HttpPost("sessions")]
    public async Task<IActionResult> Create(CreateUploadSession request, CancellationToken ct)
    {
        var session = await uploads.CreateAsync(UserId, request.Purpose, request.TargetId, ct);
        return Ok(ApiResponse<object>.Ok(new { session.Id, session.ExpiresAt }));
    }
    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var session = await db.Set<UploadSession>().AsNoTracking().Include(x => x.Assets).SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId, ct) ?? throw AppException.NotFound();
        return Ok(ApiResponse<object>.Ok(new { session.Id, session.Status, session.ResultId, session.ExpiresAt,
            assets = session.Assets.Select(x => new {
                x.Id,
                x.Status,
                x.Url,
                x.FileName,
                metadata = x.MetadataJson == null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(x.MetadataJson)
            }) }));
    }
    [HttpPost("sessions/{id:guid}/assets/{assetId:guid}"), RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid id, Guid assetId, IFormFile file, CancellationToken ct)
    {
        var asset = await uploads.UploadAsync(UserId, id, assetId, file, ct);
        return Ok(ApiResponse<object>.Ok(new { assetId = asset.Id, asset.Url,
            metadata = asset.MetadataJson == null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(asset.MetadataJson) }));
    }
    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    { await uploads.CancelAsync(UserId, id, ct); return Ok(ApiResponse.Ok("Upload cancelled.")); }
}
