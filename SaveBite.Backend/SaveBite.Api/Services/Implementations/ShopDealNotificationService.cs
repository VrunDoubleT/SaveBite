using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopDealNotificationService(
    AppDbContext db,
    INotificationService notifications,
    IDealRecipientResolver recipientResolver,
    TimeProvider clock,
    ILogger<ShopDealNotificationService> logger)
{
    public async Task HandleAsync(ShopDealOpenedEvent data, CancellationToken ct)
    {
        if (data.FlashDealId == Guid.Empty || data.ShopId == Guid.Empty || data.OpenedAt == default || data.OpenedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A valid shop deal opening event is required.");

        var now = clock.GetUtcNow().UtcDateTime;
        var deal = await db.FlashDeals.AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product)
            .SingleOrDefaultAsync(d => d.Id == data.FlashDealId && d.ShopId == data.ShopId, ct);

        // A delayed/replayed event must not advertise an unavailable deal.
        if (deal == null || deal.Status != FlashDealStatus.OnSale || deal.Shop.Status != ShopStatus.Active
            || deal.Product.DeletedAt != null || now < deal.SaleStartTime || now >= deal.OrderEndTime
            || now >= deal.ShopClosingTime)
        {
            logger.LogInformation("Deal {DealId} is unavailable; skipping notifications.", data.FlashDealId);
            return;
        }

        // 1. Resolve and persist nearby recipients (Redis GEO + GPS TTL priority rules)
        await recipientResolver.ResolveAndMaterializeRecipientsAsync(
            deal.Id,
            deal.PublicationId,
            deal.ShopId,
            deal.Shop.Latitude,
            deal.Shop.Longitude,
            radiusInKm: 15.0,
            ct);

        // 2. Fetch pending recipients for this publication
        var pendingRecipients = await db.DealNotificationRecipients
            .Where(r => r.PublicationId == deal.PublicationId && r.Status == DealNotificationRecipientStatus.Pending)
            .ToListAsync(ct);

        // Re-check deal status to ensure it was not Hidden while resolution ran
        var currentDealStatus = await db.FlashDeals
            .Where(d => d.Id == deal.Id)
            .Select(d => d.Status)
            .FirstOrDefaultAsync(ct);

        if (currentDealStatus != FlashDealStatus.OnSale)
        {
            logger.LogWarning("Deal {DealId} status changed to {Status} during resolution; cancelling delivery.", deal.Id, currentDealStatus);
            foreach (var r in pendingRecipients)
            {
                r.Status = DealNotificationRecipientStatus.Skipped;
                r.ErrorMessage = $"Deal status became {currentDealStatus}";
            }
            await db.SaveChangesAsync(ct);
            return;
        }

        var notificationTitle = NotificationMessages.DealsAndProducts.FlashDealAvailableTitle(deal.Shop.Name);
        var notificationBody = NotificationMessages.DealsAndProducts.FlashDealAvailableBody(deal.Product.Name);
        var actionUrl = NotificationActionUrls.FlashDeal(deal.Id);

        // 3. Dispatch to nearby recipients
        if (pendingRecipients.Count > 0)
        {
            var userIds = pendingRecipients.Select(r => r.UserId).Distinct().ToList();
            await notifications.SendAsync(
                NotificationTarget.Users(userIds),
                new CreateNotificationCommand(
                    NotificationType.FlashDealAvailable,
                    notificationTitle,
                    notificationBody,
                    actionUrl,
                    $"flash-deal-pub:{deal.PublicationId}"),
                ct);

            var sentAt = clock.GetUtcNow().UtcDateTime;
            foreach (var r in pendingRecipients)
            {
                r.Status = DealNotificationRecipientStatus.Sent;
                r.SentAt = sentAt;
            }
            await db.SaveChangesAsync(ct);
        }

        // 4. Also notify shop followers
        await notifications.SendAsync(
            NotificationTarget.ShopFollowers(data.ShopId, data.OpenedAt),
            new CreateNotificationCommand(
                NotificationType.FlashDealAvailable,
                notificationTitle,
                notificationBody,
                actionUrl,
                $"flash-deal-opened:{deal.Id}"),
            ct);
    }
}
