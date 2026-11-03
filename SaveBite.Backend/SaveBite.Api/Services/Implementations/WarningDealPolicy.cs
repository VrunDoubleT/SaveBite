using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Services.Implementations;
public static class WarningDealPolicy
{
    public static Expression<Func<ProductWarning, bool>> BlockedAt(DateTime now)
        => w => w.ResolvedAt == null && (w.DealBlocked ||
            (w.DueAt <= now && (w.FirstEditedAt == null || w.FirstEditedAt > w.DueAt)));

    public static Task<bool> IsBlockedAsync(AppDbContext db, Guid productId, CancellationToken ct)
        => db.ProductWarnings.Where(w => w.ProductId == productId)
            .AnyAsync(BlockedAt(DateTime.UtcNow), ct);
}
