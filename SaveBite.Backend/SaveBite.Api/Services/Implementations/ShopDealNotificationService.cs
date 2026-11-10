using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Caching;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopDealNotificationService(
    AppDbContext db,
    INotificationService notifications,
    IDealRecipientResolver recipientResolver,
    IConnectionMultiplexer redis,
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
                .ThenInclude(p => p.Category)
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
        var notifiedUserIds = new HashSet<Guid>();
        if (pendingRecipients.Count > 0)
        {
            var userIds = pendingRecipients.Select(r => r.UserId).Distinct().ToList();
            notifiedUserIds.UnionWith(userIds);
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

        // 4. Also notify shop followers (exclude users already notified above and verify active category filters)
        var candidateFollowers = await db.ShopFollows.AsNoTracking()
            .Where(f => f.ShopId == data.ShopId && f.NotificationStatus == ShopNotificationStatus.Enabled
                && f.FollowedAt <= data.OpenedAt
                && f.Customer.Role != UserRole.Admin && f.Customer.Status == UserStatus.Active
                && f.Customer.CustomerStatus == CustomerStatus.Active
                && !notifiedUserIds.Contains(f.CustomerId))
            .Select(f => f.CustomerId)
            .Distinct()
            .ToListAsync(ct);

        if (candidateFollowers.Count > 0)
        {
            var redisDb = redis.GetDatabase();
            var dealCategory = deal.Product?.Category?.Name ?? "";
            var cleanDealCat = VietnameseTextHelper.RemoveDiacritics(dealCategory);
            var validFollowerIds = new List<Guid>();

            foreach (var fId in candidateFollowers)
            {
                var filterJson = await redisDb.StringGetAsync(RedisKeys.UserSearchFilter(fId));
                if (!filterJson.IsNullOrEmpty)
                {
                    try
                    {
                        var filter = JsonSerializer.Deserialize<SaveSearchFilterRequest>(filterJson.ToString());
                        if (filter != null && !string.IsNullOrWhiteSpace(filter.Category) && !string.IsNullOrWhiteSpace(dealCategory))
                        {
                            var rawCats = filter.Category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(c => c.ToLowerInvariant()).ToArray();
                            var cleanCats = filter.Category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Select(c => VietnameseTextHelper.RemoveDiacritics(c)).ToArray();

                            if (rawCats.Length > 0 && !rawCats.Contains(dealCategory.ToLowerInvariant()) && !cleanCats.Contains(cleanDealCat))
                            {
                                continue;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                validFollowerIds.Add(fId);
            }

            if (validFollowerIds.Count > 0)
            {
                await notifications.SendAsync(
                    NotificationTarget.Users(validFollowerIds),
                    new CreateNotificationCommand(
                        NotificationType.FlashDealAvailable,
                        notificationTitle,
                        notificationBody,
                        actionUrl,
                        $"flash-deal-opened:{deal.Id}"),
                    ct);
            }
        }
    }
}