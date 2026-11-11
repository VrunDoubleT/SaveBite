using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using Microsoft.Extensions.Primitives;
namespace SaveBite.Backend.Uploads;
public sealed record UploadReference(string Field, Guid AssetId);
public sealed class UploadReferenceFilter(AppDbContext db) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!request.HasFormContentType) { await next(); return; }
        var ct = context.HttpContext.RequestAborted;
        var form = await request.ReadFormAsync(ct);
        if (!form.ContainsKey("__uploadAssets")) { await next(); return; }
        var userId = Guid.Parse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.HttpContext.User.FindFirstValue("sub") ?? throw AppException.Unauthorized());
        var refs = JsonSerializer.Deserialize<List<UploadReference>>(form["__uploadAssets"].ToString(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        if (refs.Count > 70 || refs.Select(x => x.AssetId).Distinct().Count() != refs.Count)
            throw AppException.BadRequest("Invalid upload references.");
        var path = request.Path.Value ?? "";
        var purpose = path.StartsWith("/api/products", StringComparison.OrdinalIgnoreCase) ? "product"
            : path.StartsWith("/api/shop-applications", StringComparison.OrdinalIgnoreCase) ? "shop-application"
            : throw AppException.BadRequest("Upload references are not supported on this endpoint.");
        var assets = await db.Set<UploadAsset>().Include(x => x.Session)
            .Where(x => refs.Select(r => r.AssetId).Contains(x.Id)).ToListAsync(ct);
        var files = new FormFileCollection(); foreach (var file in form.Files) files.Add(file);
        foreach (var reference in refs)
        {
            var asset = assets.SingleOrDefault(x => x.Id == reference.AssetId);
            if (asset == null || asset.Session.UserId != userId || asset.Session.Purpose != purpose ||
                asset.Status != "Uploaded" || asset.Session.Status != "Active" || asset.ExpiresAt <= DateTime.UtcNow)
                throw AppException.BadRequest("Upload does not belong to this account, or is no longer available.");
            files.Add(new StoredUploadFile(asset, reference.Field));
        }
        request.Form = new FormCollection(form.ToDictionary(x => x.Key, x => x.Value), files);
        await next();
    }
}
