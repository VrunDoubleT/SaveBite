using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Services.Implementations;

public static class FeedbackRealtimeOutbox
{
    public const string Route = "internal.feedback.changed";

    public static async Task StageAsync(AppDbContext db, CancellationToken ct)
    {
        var changed = db.ChangeTracker.Entries<ProductFeedback>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => e.Entity).ToArray();
        foreach (var feedback in changed)
        {
            var shopId = feedback.Product?.ShopId ?? await db.Products.AsNoTracking()
                .Where(p => p.Id == feedback.ProductId).Select(p => (Guid?)p.ShopId).SingleOrDefaultAsync(ct);
            db.Add(new OrderEventOutbox
            {
                Id = Guid.NewGuid(), EventKey = $"feedback-refresh:{feedback.Id}:{Guid.NewGuid():N}",
                RoutingKey = Route, CreatedAt = DateTime.UtcNow,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    feedbackId = feedback.Id, productId = feedback.ProductId, shopId,
                    rating = feedback.Rating, action = feedback.Status == FeedbackStatus.Visible ? "refresh" : "hide"
                })
            });
        }
    }
}
