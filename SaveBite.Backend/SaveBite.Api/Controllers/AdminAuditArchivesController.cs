using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Audit;
using SaveBite.Backend.Audit.Archives;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Common;
namespace SaveBite.Backend.Controllers;

[ApiController, Route("api/admin/audit-archives"), AdminAccess]
public sealed class AdminAuditArchivesController(AuditArchiveService archives, AppDbContext db, IAuditWriter audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(ApiResponse<object>.Ok(await archives.ListAsync(ct)));
    [HttpPost("{year:int}/{month:int}")]
    public async Task<IActionResult> Create(int year, int month, CancellationToken ct) =>
        ArchiveResponse(await archives.CreateAsync(year, month, false, ct));

    private IActionResult ArchiveResponse(AuditLogArchive? archive) =>
        Ok(ApiResponse<AuditLogArchive?>.Ok(archive, archive == null
            ? "No audit logs remain for this month. No archive was created."
            : "Audit logs archived successfully."));
    // Deliberate GET commands requested for demo testing; never delete source audit rows.
    [HttpGet("test/current-month/backup"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TestBackup(CancellationToken ct)
    {
        var current = archives.CurrentMonth();
        return ArchiveResponse(await archives.CreateAsync(current.Year, current.Month, true, ct));
    }
    [HttpGet("test/current-month/delete"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TestDelete(CancellationToken ct)
    {
        await archives.DeleteCurrentTestAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { Message = "Current-month test files deleted. Source audit rows were not deleted." }));
    }
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var archive = await archives.FindAsync(id, ct);
        var path = await archives.DownloadAsync(archive, ct);
        string? zipPath = null;
        try
        {
            var name = $"audit-logs-{archive.Year:D4}-{archive.Month:D2}-{id}";
            zipPath = await AuditArchiveFile.CreateDownloadZipAsync(path, name + ".jsonl", ct);
            audit.Record(nameof(AuditLogArchive), id, "DOWNLOADED");
            await db.SaveChangesAsync(ct);
            var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.DeleteOnClose);
            return File(stream, "application/zip", name + ".zip");
        }
        catch { if (zipPath != null) System.IO.File.Delete(zipPath); throw; }
        finally { System.IO.File.Delete(path); }
    }
    [HttpGet("{id:guid}/logs")]
    public async Task<IActionResult> Logs(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var archive = await archives.FindAsync(id, ct);
        var path = await archives.DownloadAsync(archive, ct);
        try
        {
            var items = new List<object>();
            var skip = ((long)page - 1) * pageSize;
            var index = 0L;
            await foreach (var row in AuditArchiveFile.RowsAsync(path, ct))
            {
                if (index++ < skip) continue;
                items.Add(new
                {
                    row.Id,
                    row.ActorUserId,
                    ActorType = row.ActorType.ToString(),
                    row.Action,
                    row.TargetType,
                    row.TargetId,
                    OldValues = SaveBite.Backend.Helpers.AuditPayloadSanitizer.ToSafeDictionary(row.OldValuesJson),
                    NewValues = SaveBite.Backend.Helpers.AuditPayloadSanitizer.ToSafeDictionary(row.NewValuesJson),
                    row.Reason,
                    row.CreatedAt
                });
                if (items.Count == pageSize) break;
            }
            return Ok(ApiResponse<object>.Ok(new { Items = items, Page = page, PageSize = pageSize, TotalItems = archive.RecordCount }));
        }
        finally { System.IO.File.Delete(path); }
    }
}
