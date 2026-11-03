using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[AdminAccess]
[Route("api/admin/email-deliveries")]
public sealed class AdminEmailDeliveriesController(AppDbContext db, IAdminAuditWriter audit, TimeProvider clock) : ControllerBase
{
    public sealed record Resolution(bool WasSent, string Reason);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var cutoff = clock.GetUtcNow().UtcDateTime.AddMinutes(-10);
        return Ok(await db.Set<EmailDelivery>().AsNoTracking()
            .Where(e => e.Status == "Uncertain" || (e.Status == "Sending" && e.UpdatedAt < cutoff))
            .OrderBy(e => e.UpdatedAt).Take(100).ToListAsync(ct));
    }

    // SMTP logs must be checked before choosing WasSent=false; the decision is audited atomically.
    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, Resolution request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            throw AppException.BadRequest("Provide the result of SMTP delivery verification (1–1000 characters).");
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(actor, out var actorId)) throw AppException.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var row = await db.Set<EmailDelivery>().FromSqlInterpolated($"SELECT * FROM email_deliveries WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound();
        var now = clock.GetUtcNow().UtcDateTime;
        if (row.Status != "Uncertain" && !(row.Status == "Sending" && row.UpdatedAt < now.AddMinutes(-10)))
            throw AppException.Conflict("This email is not awaiting delivery verification.");
        var before = row.Status;
        row.Status = request.WasSent ? "Sent" : "Pending";
        row.SentAt = request.WasSent ? now : null;
        row.UpdatedAt = now;
        if (!request.WasSent)
            await db.Set<OrderEventOutbox>().Where(e => e.EventKey == row.DeliveryKey)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.PublishedAt, (DateTime?)null), ct);
        audit.Record(new AuditLog { ActorUserId = actorId, Action = "ResolveEmailDelivery", TargetType = "EmailDelivery", TargetId = row.Id,
            OldValuesJson = JsonSerializer.Serialize(new { Status = before }),
            NewValuesJson = JsonSerializer.Serialize(new { row.Status }), Reason = request.Reason.Trim() });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { row.Id, row.Status });
    }
}
