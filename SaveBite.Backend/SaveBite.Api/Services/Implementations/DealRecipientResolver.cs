using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Caching;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Services.Implementations;

public sealed class DealRecipientResolver : IDealRecipientResolver
{
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILocationService _locationService;
    private readonly ILogger<DealRecipientResolver> _logger;

    public DealRecipientResolver(
        AppDbContext db,
        IConnectionMultiplexer redis,
        ILocationService locationService,
        ILogger<DealRecipientResolver> logger)
    {
        _db = db;
        _redis = redis;
        _locationService = locationService;
        _logger = logger;
    }

    public async Task<int> ResolveAndMaterializeRecipientsAsync(
        Guid flashDealId,
        Guid publicationId,
        Guid shopId,
        double shopLatitude,
        double shopLongitude,
        double radiusInKm,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var gpsTempKey = RedisKeys.PublicationGpsTemp(publicationId);
        var defaultTempKey = RedisKeys.PublicationDefaultTemp(publicationId);

        var shopLonStr = shopLongitude.ToString(CultureInfo.InvariantCulture);
        var shopLatStr = shopLatitude.ToString(CultureInfo.InvariantCulture);
        var radiusStr = radiusInKm.ToString(CultureInfo.InvariantCulture);

        var dealCategory = await _db.FlashDeals.AsNoTracking()
            .Where(d => d.Id == flashDealId)
            .Select(d => d.Product.Category.Name)
            .FirstOrDefaultAsync(ct);

        // 1. Materialize GPS candidates via GEOSEARCHSTORE
        try
        {
            await db.ExecuteAsync("GEOSEARCHSTORE", gpsTempKey, RedisKeys.UsersGpsGeo,
                "FROMLONLAT", shopLonStr, shopLatStr,
                "BYRADIUS", radiusStr, "KM");
            await db.KeyExpireAsync(gpsTempKey, TimeSpan.FromHours(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to materialize GPS candidates via GEOSEARCHSTORE for publication {PubId}", publicationId);
        }

        // 2. Materialize Default Address candidates via GEOSEARCHSTORE
        try
        {
            await db.ExecuteAsync("GEOSEARCHSTORE", defaultTempKey, RedisKeys.UsersDefaultGeo,
                "FROMLONLAT", shopLonStr, shopLatStr,
                "BYRADIUS", radiusStr, "KM");
            await db.KeyExpireAsync(defaultTempKey, TimeSpan.FromHours(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to materialize Default candidates via GEOSEARCHSTORE for publication {PubId}", publicationId);
        }

        var processedUsers = new HashSet<Guid>();
        var recipients = new List<DealNotificationRecipient>();
        const int BatchSize = 500;

        // 3. Process GPS candidates (Batch-by-batch)
        var gpsCount = await db.SortedSetLengthAsync(gpsTempKey);
        for (long start = 0; start < gpsCount; start += BatchSize)
        {
            ct.ThrowIfCancellationRequested();
            var members = await db.SortedSetRangeByRankAsync(gpsTempKey, start, start + BatchSize - 1);
            foreach (var member in members)
            {
                if (!Guid.TryParse(member.ToString(), out var userId)) continue;
                if (processedUsers.Contains(userId)) continue;

                var metadata = await _locationService.GetGpsMetadataAsync(userId, ct);
                if (metadata == null) continue;

                if (metadata.Accuracy.HasValue && metadata.Accuracy.Value > 3000) continue;

                // Check active search filter from Redis
                var filterJson = await db.StringGetAsync(RedisKeys.UserSearchFilter(userId));
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
                            var cleanDealCat = VietnameseTextHelper.RemoveDiacritics(dealCategory);

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

                processedUsers.Add(userId);
                recipients.Add(new DealNotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    PublicationId = publicationId,
                    UserId = userId,
                    LocationSource = RecipientLocationSource.GPS,
                    LocationVersion = metadata.GpsVersion.ToString(CultureInfo.InvariantCulture),
                    Status = DealNotificationRecipientStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // 4. Process Default Address candidates (Batch-by-batch)
        var defaultCount = await db.SortedSetLengthAsync(defaultTempKey);
        for (long start = 0; start < defaultCount; start += BatchSize)
        {
            ct.ThrowIfCancellationRequested();
            var members = await db.SortedSetRangeByRankAsync(defaultTempKey, start, start + BatchSize - 1);
            foreach (var member in members)
            {
                if (!Guid.TryParse(member.ToString(), out var userId)) continue;
                if (processedUsers.Contains(userId)) continue;

                var metadata = await _locationService.GetGpsMetadataAsync(userId, ct);
                if (metadata != null) continue;

                // Check active search filter from Redis
                var filterJson = await db.StringGetAsync(RedisKeys.UserSearchFilter(userId));
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
                            var cleanDealCat = VietnameseTextHelper.RemoveDiacritics(dealCategory);

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

                var addressVersion = await _locationService.GetSyncedDefaultAddressVersionAsync(userId, ct) ?? 1;
                processedUsers.Add(userId);

                recipients.Add(new DealNotificationRecipient
                {
                    Id = Guid.NewGuid(),
                    PublicationId = publicationId,
                    UserId = userId,
                    LocationSource = RecipientLocationSource.DefaultAddress,
                    LocationVersion = addressVersion.ToString(CultureInfo.InvariantCulture),
                    Status = DealNotificationRecipientStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // 5. Persist recipients into PostgreSQL with ON CONFLICT protection
        var insertedCount = 0;
        if (_db.Database.IsRelational())
        {
            for (int i = 0; i < recipients.Count; i += BatchSize)
            {
                ct.ThrowIfCancellationRequested();
                var chunk = recipients.Skip(i).Take(BatchSize).ToList();

                foreach (var r in chunk)
                {
                    try
                    {
                        await _db.Database.ExecuteSqlInterpolatedAsync($"""
                            INSERT INTO deal_notification_recipients (
                                id, publication_id, user_id, location_source, location_version, status, created_at
                            )
                            VALUES (
                                {r.Id}, {r.PublicationId}, {r.UserId}, {r.LocationSource.ToString()},
                                {r.LocationVersion}, {r.Status.ToString()}, {r.CreatedAt}
                            )
                            ON CONFLICT (publication_id, user_id) DO NOTHING;
                            """, ct);
                        insertedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to persist recipient {UserId} for publication {PubId}", r.UserId, publicationId);
                    }
                }
            }
        }
        else
        {
            foreach (var r in recipients)
            {
                if (!await _db.DealNotificationRecipients.AnyAsync(existing => existing.PublicationId == r.PublicationId && existing.UserId == r.UserId, ct))
                {
                    _db.DealNotificationRecipients.Add(r);
                    insertedCount++;
                }
            }
            await _db.SaveChangesAsync(ct);
        }

        // 6. Cleanup temporary Redis keys
        try
        {
            await Task.WhenAll(
                db.KeyDeleteAsync(gpsTempKey),
                db.KeyDeleteAsync(defaultTempKey)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temporary Redis GEO keys for publication {PubId}", publicationId);
        }

        _logger.LogInformation("Resolved {Count} recipients for Deal {DealId}, Publication {PubId}", insertedCount, flashDealId, publicationId);
        return insertedCount;
    }
}