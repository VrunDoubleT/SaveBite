using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopDealNotificationService(AppDbContext db, INotificationService notifications, TimeProvider clock)
{
    public async Task HandleAsync(ShopDealOpenedEvent data, CancellationToken ct)
    {
        if (data.FlashDealId == Guid.Empty || data.ShopId == Guid.Empty || data.OpenedAt == default || data.OpenedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A valid shop deal opening event is required.");
        var now = clock.GetUtcNow().UtcDateTime;
        var deal = await db.FlashDeals.AsNoTracking().Include(d => d.Shop).Include(d => d.Product)
            .SingleOrDefaultAsync(d => d.Id == data.FlashDealId && d.ShopId == data.ShopId, ct);
        // A delayed/replayed event must not advertise an unavailable deal.
        if (deal == null || deal.Status != FlashDealStatus.OnSale || deal.Shop.Status != ShopStatus.Active
            || deal.Product.Status != ProductStatus.Active || now < deal.SaleStartTime || now >= deal.OrderEndTime
            || now >= deal.ShopClosingTime) return;
        await notifications.SendAsync(NotificationTarget.ShopFollowers(data.ShopId, data.OpenedAt),
            new CreateNotificationCommand(NotificationType.FlashDealAvailable, $"New Flash Deal from {deal.Shop.Name}!",
                $"'{deal.Product.Name}' is now on sale. Order now before it runs out!",
                NotificationActionUrls.FlashDeal(deal.Id), $"flash-deal-opened:{deal.Id}"), ct);
    }
}
