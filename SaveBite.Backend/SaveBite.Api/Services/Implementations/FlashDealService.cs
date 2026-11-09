using SaveBite.Backend.Configurations;
using SaveBite.Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class FlashDealService : IFlashDealService
{
    private readonly AppDbContext _db;
    private readonly IServiceScopeFactory? _scopes;
    private readonly IFlashDealRepository _dealRepository;
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly IShopRepository _shopRepository;
    private readonly IShopStaffRepository _staffRepository;
    private readonly ILogger<FlashDealService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRabbitMqPublisher _publisher;
    public FlashDealService(
        IFlashDealRepository dealRepository,
        IUserAddressRepository userAddressRepository,
        IShopRepository shopRepository,
        IShopStaffRepository staffRepository,
        IBackgroundJobClient backgroundJobClient,
        IRabbitMqPublisher publisher,
        ILogger<FlashDealService> logger, AppDbContext db, IServiceScopeFactory? scopes = null)
    {
        _db = db;
        _scopes = scopes;
        _dealRepository = dealRepository;
        _userAddressRepository = userAddressRepository;
        _shopRepository = shopRepository;
        _staffRepository = staffRepository;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _publisher = publisher;
    }

    private async Task TryPublishLifecycleAsync(string routingKey, FlashDealEvent payload, CancellationToken ct)
    {
        try { await _publisher.PublishAsync(routingKey, payload, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Flash Deal {Id} event deferred to recovery.", payload.FlashDealId); }
    }

    private async Task RefreshScheduleAsync(FlashDeal deal, CancellationToken ct)
    {
        foreach (var oldId in new[] { deal.OpenJobId, deal.EndJobId }.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            try { _backgroundJobClient.Delete(oldId!); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete old Flash Deal job {JobId}; deadline checks invalidate it.", oldId); }
        }
        deal.OpenJobId = null;
        var start = deal.SaleStartTime;
        var end = deal.OrderEndTime;
        if (deal.Status == FlashDealStatus.Pending && start > DateTime.UtcNow)
            deal.OpenJobId = SaveBite.Backend.BackgroundJobs.Scheduling.SafeSchedule.Try(() =>
                _backgroundJobClient.Schedule<IFlashDealJobService>(job => job.PublishDealOpenedAtAsync(deal.Id, start),
                    new DateTimeOffset(DateTime.SpecifyKind(start, DateTimeKind.Utc))), _logger, "flash-open", deal.Id);
        deal.EndJobId = SaveBite.Backend.BackgroundJobs.Scheduling.SafeSchedule.Try(() =>
            _backgroundJobClient.Schedule<IFlashDealJobService>(job => job.PublishDealExpiredAtAsync(deal.Id, end),
                new DateTimeOffset(DateTime.SpecifyKind(end, DateTimeKind.Utc))), _logger, "flash-end", deal.Id);
        await _dealRepository.UpdateAsync(deal, ct);
        if (start <= DateTime.UtcNow && end > DateTime.UtcNow && deal.Status is FlashDealStatus.Pending or FlashDealStatus.OnSale or FlashDealStatus.SoldOut)
        {
            try { await _publisher.PublishAsync(RabbitMqRoutingKeys.FlashDealOpened,
                new FlashDealEvent { FlashDealId = deal.Id, ExpectedDeadline = start }, ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Flash Deal {Id} opening deferred to recovery.", deal.Id); }
        }
    }

    public async Task<FlashDealCursorListResponse> GetNearbyAsync(
        FlashDealCursorRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        double latitude;
        double longitude;

        if (request.Latitude.HasValue && request.Longitude.HasValue && (request.Latitude.Value != 0 || request.Longitude.Value != 0))
        {
            latitude = request.Latitude.Value;
            longitude = request.Longitude.Value;
        }
        else if (userId.HasValue)
        {
            var defaultAddress = await _userAddressRepository.GetDefaultByUserIdAsync(userId.Value, cancellationToken);
            if (defaultAddress != null)
            {
                latitude = defaultAddress.Latitude;
                longitude = defaultAddress.Longitude;
            }
            else
            {
                return new FlashDealCursorListResponse
                {
                    Deals = new List<FlashDealResponse>(),
                    NextCursor = null,
                    HasMore = false,
                    Cursor1 = null,
                    Cursor2 = null,
                    HasOlder = false,
                    HasNewer = false
                };
            }
        }
        else
        {
            return new FlashDealCursorListResponse
            {
                Deals = new List<FlashDealResponse>(),
                NextCursor = null,
                HasMore = false,
                Cursor1 = null,
                Cursor2 = null,
                HasOlder = false,
                HasNewer = false
            };
        }

        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw AppException.BadRequest("Invalid location coordinates.");
        if (!double.IsFinite(request.RadiusInKm) || request.RadiusInKm <= 0)
            throw AppException.BadRequest("Radius must be greater than zero.");
        request.Limit = Math.Clamp(request.Limit, 1, 100);
        var radius = Math.Min(request.RadiusInKm, 50);

        // Section 8.2 & 8.4: Keyset Cursor & Snapshot Handling
        DateTime snapshotAt = DateTime.UtcNow;
        DateTime? lastPublishedAt = null;
        Guid? lastDealId = null;

        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            var cursorData = FeedCursorTokenHandler.Decode(request.Cursor)
                ?? throw AppException.BadRequest("Invalid feed cursor token.");

            // Section 8.4: Verify location context within threshold (500m / 0.5km)
            var distanceMoved = FlashDealReadQuery.Distance(cursorData.Latitude, cursorData.Longitude, latitude, longitude);
            if (distanceMoved > 0.5)
            {
                throw AppException.Conflict("CURSOR_LOCATION_CHANGED: Location context changed beyond threshold. Please refresh your feed.");
            }

            if (!string.IsNullOrWhiteSpace(cursorData.LocationMode) && !string.IsNullOrWhiteSpace(request.LocationMode) &&
                !string.Equals(cursorData.LocationMode, request.LocationMode, StringComparison.OrdinalIgnoreCase))
            {
                throw AppException.Conflict("CURSOR_LOCATION_CHANGED: Location source changed. Please refresh your feed.");
            }

            snapshotAt = cursorData.SnapshotAt;
            lastPublishedAt = cursorData.LastFeedPublishedAt;
            lastDealId = cursorData.LastDealId;
        }

        var query = FlashDealReadQuery.Visible(_db, DateTime.UtcNow);
        // Ensure deal was published before or at snapshot time
        query = query.Where(d => d.FeedPublishedAt <= snapshotAt);

        // Keyset Cursor boundary
        if (lastPublishedAt.HasValue && lastDealId.HasValue)
        {
            query = query.Where(d => d.FeedPublishedAt < lastPublishedAt.Value
                || (d.FeedPublishedAt == lastPublishedAt.Value && d.Id < lastDealId.Value));
        }

        var nearbyShops = _db.Shops.Select(shop => new
        {
            shop.Id,
            Distance = 6371 * 2 * Math.Asin(Math.Min(1, Math.Sqrt(
                Math.Pow(Math.Sin((shop.Latitude - latitude) * Math.PI / 360), 2)
                + Math.Cos(latitude * Math.PI / 180) * Math.Cos(shop.Latitude * Math.PI / 180)
                * Math.Pow(Math.Sin((shop.Longitude - longitude) * Math.PI / 360), 2))))
        }).Where(shop => shop.Distance <= radius);

        var distanceRules = (request.Distance ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (distanceRules.Length > 0)
        {
            var under2 = distanceRules.Contains("Under 2km", StringComparer.OrdinalIgnoreCase);
            var under5 = distanceRules.Contains("Under 5km", StringComparer.OrdinalIgnoreCase);
            var over5 = distanceRules.Contains("Over 5km", StringComparer.OrdinalIgnoreCase)
                || distanceRules.Contains("Over 5km - 10km", StringComparer.OrdinalIgnoreCase);
            nearbyShops = nearbyShops.Where(shop => (under2 && shop.Distance < 2)
                || (under5 && shop.Distance < 5) || (over5 && shop.Distance >= 5 && shop.Distance <= 10));
        }
        query = query.Where(d => nearbyShops.Select(shop => shop.Id).Contains(d.ShopId));
        if (userId.HasValue)
        {
            query = query.Where(d => d.Shop.OwnerUserId != userId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLowerInvariant();
            query = query.Where(d => d.Product.Name.ToLower().Contains(kw) || d.Shop.Name.ToLower().Contains(kw)
                || (d.Product.Description != null && d.Product.Description.ToLower().Contains(kw)));
        }
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var categories = request.Category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.ToLowerInvariant()).ToArray();
            if (categories.Length > 0) query = query.Where(d => categories.Contains(d.Product.Category.Name.ToLower()));
        }
        if (!string.IsNullOrWhiteSpace(request.ShopName))
        {
            var name = request.ShopName.Trim().ToLowerInvariant();
            query = query.Where(d => d.Shop.Name.ToLower() == name);
        }
        var priceRules = (request.Price ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (priceRules.Length > 0)
        {
            var low = priceRules.Contains("Under 30k", StringComparer.OrdinalIgnoreCase);
            var mid = priceRules.Contains("30k - 50k", StringComparer.OrdinalIgnoreCase);
            var high = priceRules.Contains("Over 50k", StringComparer.OrdinalIgnoreCase);
            query = query.Where(d => (low && d.Variants.Min(v => (decimal?)v.DealPrice) < 30000)
                || (mid && d.Variants.Min(v => (decimal?)v.DealPrice) >= 30000 && d.Variants.Min(v => (decimal?)v.DealPrice) <= 50000)
                || (high && d.Variants.Min(v => (decimal?)v.DealPrice) > 50000));
        }

        // Section 8.2: Keyset Order
        var sortedDeals = query.OrderByDescending(d => d.FeedPublishedAt).ThenByDescending(d => d.Id);

        var entities = await FlashDealReadQuery.WithDetails(sortedDeals.Take(request.Limit + 1)).ToListAsync(cancellationToken);
        var hasMore = entities.Count > request.Limit;
        var pagedEntities = entities.Take(request.Limit).ToList();

        var resultDeals = pagedEntities.Select(deal =>
        {
            var response = MapToResponse(deal);
            response.DistanceInKm = Math.Round(FlashDealReadQuery.Distance(latitude, longitude, deal.Shop.Latitude, deal.Shop.Longitude), 2);
            return response;
        }).ToList();

        string? nextCursor = null;
        if (hasMore && pagedEntities.Count > 0)
        {
            var last = pagedEntities[^1];
            nextCursor = FeedCursorTokenHandler.Encode(new FeedCursorData
            {
                LastFeedPublishedAt = last.FeedPublishedAt,
                LastDealId = last.Id,
                SnapshotAt = snapshotAt,
                Latitude = latitude,
                Longitude = longitude,
                LocationMode = request.LocationMode,
                FilterHash = $"{request.RadiusInKm}:{request.Category}:{request.Price}:{request.Distance}:{request.ShopName}"
            });
        }

        DateTime? cursor1 = resultDeals.Count > 0 ? resultDeals.Min(d => d.CreatedAt) : null;
        DateTime? cursor2 = resultDeals.Count > 0 ? resultDeals.Max(d => d.CreatedAt) : null;

        return new FlashDealCursorListResponse
        {
            Deals = resultDeals,
            NextCursor = nextCursor,
            HasMore = hasMore,
            Cursor1 = cursor1,
            Cursor2 = cursor2,
            HasOlder = hasMore,
            HasNewer = false,
            PrependedCount = 0,
            NewDealsCount = 0
        };
    }

    public async Task<List<FlashDealResponse>> GetByShopIdAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var deals = await FlashDealReadQuery.WithDetails(FlashDealReadQuery.Visible(_db, DateTime.UtcNow)
            .Where(d => d.ShopId == shopId).OrderByDescending(d => d.CreatedAt).ThenBy(d => d.Id))
            .ToListAsync(cancellationToken);
        return deals.Select(MapToResponse).ToList();
    }

    public async Task<FlashDealResponse> GetByIdAsync(Guid dealId, CancellationToken cancellationToken = default)
    {
        var deal = await FlashDealReadQuery.WithDetails(FlashDealReadQuery.Visible(_db, DateTime.UtcNow))
            .SingleOrDefaultAsync(d => d.Id == dealId, cancellationToken);
        if (deal == null) throw AppException.NotFound($"Flash deal with ID '{dealId}' is unavailable.");
        return MapToResponse(deal);
    }

    public static FlashDealResponse MapToResponse(FlashDeal deal)
    {
        var attributeGroups = deal.Product?.Attributes?
            .Select(a => new DealAttributeGroupResponse
            {
                Name = a.Name,
                Values = a.Values.Select(v => v.Value).Distinct().ToList()
            })
            .Where(g => g.Values.Count > 0)
            .ToList() ?? new List<DealAttributeGroupResponse>();

        var now = DateTime.UtcNow;
        var isDealOnSale = deal.Status == FlashDealStatus.OnSale
            && deal.Shop?.Status == ShopStatus.Active && deal.Product?.Status == ProductStatus.Active;
        var isDealTimeValid = now >= deal.SaleStartTime.ToUniversalTime() && now < deal.OrderEndTime.ToUniversalTime() && now < deal.ShopClosingTime.ToUniversalTime();

        var variants = deal.Variants.Select(v =>
        {
            var attrs = v.Variant?.VariantValues?
                .Where(vv => vv.AttributeValue != null && vv.AttributeValue.Attribute != null)
                .ToDictionary(
                    vv => vv.AttributeValue.Attribute.Name,
                    vv => vv.AttributeValue.Value
                ) ?? new Dictionary<string, string>();

            var variantImg = v.Variant?.VariantValues?
                .FirstOrDefault(vv => !string.IsNullOrEmpty(vv.AttributeValue?.ImageUrl))?
                .AttributeValue?.ImageUrl;

            var vName = attrs.Count > 0
                ? string.Join(" - ", attrs.Values)
                : (v.Variant?.Sku ?? "Default");

            var availableQty = Math.Max(0L, (long)v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity);
            var isVariantPurchasable = isDealOnSale
                && isDealTimeValid
                && v.Status == FlashDealVariantStatus.Active
                && v.Variant?.IsActive == true
                && availableQty > 0;

            return new FlashDealVariantResponse
            {
                Id = v.Id,
                VariantId = v.VariantId,
                Sku = v.Variant?.Sku,
                VariantName = vName,
                ImageUrl = variantImg,
                Attributes = attrs,
                OriginalPrice = v.OriginalPrice,
                DealPrice = v.DealPrice,
                DiscountPercent = v.DiscountPercent,
                TotalQuantity = v.TotalQuantity,
                SoldQuantity = v.SoldQuantity,
                ReservedQuantity = v.ReservedQuantity,
                Status = v.Status,
                IsPurchasable = isVariantPurchasable
            };
        }).ToList();

        var minPrice = variants.Count > 0 ? variants.Min(v => v.DealPrice) : 0;
        var maxOriginal = variants.Count > 0 ? variants.Max(v => v.OriginalPrice) : 0;
        var maxDiscount = variants.Count > 0 ? variants.Max(v => v.DiscountPercent) : 0;

        var images = deal.Product?.Images?
            .Select(i => i.ImageUrl)
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList() ?? new List<string>();

        foreach (var v in variants)
        {
            if (!string.IsNullOrEmpty(v.ImageUrl) && !images.Contains(v.ImageUrl))
            {
                images.Add(v.ImageUrl);
            }
        }

        var primaryImage = deal.Product?.Images?.FirstOrDefault()?.ImageUrl
            ?? images.FirstOrDefault();

        var isDealPurchasable = isDealOnSale && isDealTimeValid && variants.Any(v => v.IsPurchasable);

        return new FlashDealResponse
        {
            Id = deal.Id,
            ShopId = deal.ShopId,
            ShopName = deal.Shop?.Name ?? string.Empty,
            ShopAddress = deal.Shop?.AddressLine ?? string.Empty,
            ShopLogoUrl = deal.Shop?.LogoUrl,
            ProductId = deal.ProductId,
            ProductName = deal.Product?.Name ?? string.Empty,
            Description = deal.Product?.Description,
            CategoryName = deal.Product?.Category?.Name,
            ProductImageUrl = primaryImage,
            ProductImageUrls = images,
            SaleStartTime = deal.SaleStartTime,
            OrderEndTime = deal.OrderEndTime,
            ShopClosingTime = deal.ShopClosingTime,
            Status = deal.Status,
            MinDealPrice = minPrice,
            MaxOriginalPrice = maxOriginal,
            MaxDiscountPercent = maxDiscount,
            AttributeGroups = attributeGroups,
            Variants = variants,
            CreatedAt = deal.CreatedAt,
            IsPurchasable = isDealPurchasable
        };
    }

    // 1. View Flash Deals for Shop (with pagination and filters)
    public async Task<PagedResult<ShopFlashDealListItemResponse>> GetShopDealsAsync(
        Guid userId,
        ShopFlashDealQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetShopForViewOrThrowAsync(userId, request.ShopId, cancellationToken);
        var pagedDeals = await _dealRepository.GetShopDealsAsync(shop.Id, request, cancellationToken);

        return new PagedResult<ShopFlashDealListItemResponse>
        {
            Items = pagedDeals.Items.Select(MapToShopListItemResponse).ToList(),
            TotalItems = pagedDeals.TotalItems,
            Page = pagedDeals.Page,
            PageSize = pagedDeals.PageSize
        };
    }

    // 2. View Flash Deal Details for Shop
    public async Task<ShopFlashDealDetailResponse> GetShopDealDetailAsync(
        Guid userId,
        Guid dealId,
        Guid? shopId = null,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetShopForViewOrThrowAsync(userId, shopId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
                   ?? throw AppException.NotFound("Flash deal not found.");

        return MapToShopDetailResponse(deal);
    }

    // 3. Create Flash Deal
    public async Task<ShopFlashDealDetailResponse> CreateDealAsync(
        Guid userId,
        CreateFlashDealRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        var effectiveStartTime = request.IsImmediate ? now : request.SaleStartTime!.Value;
        if (request.IsImmediate && request.DurationMinutes.HasValue)
        {
            var window = FlashDealTiming.ImmediateWindow(now, request.DurationMinutes.Value,
                request.ShopClosingTime - request.OrderEndTime);
            request.OrderEndTime = window.End;
            request.ShopClosingTime = window.Closing;
        }

        // Validate time intervals.
        if (effectiveStartTime >= request.OrderEndTime)
        {
            throw AppException.BadRequest("Sale start time must be before order end time.");
        }
        if (shop.OpeningTime.HasValue && shop.ClosingTime.HasValue)
        {
            var startLocalTime = TimeOnly.FromDateTime(effectiveStartTime.AddHours(7));
            var endLocalTime = TimeOnly.FromDateTime(request.OrderEndTime.AddHours(7));
            if (!IsWithinOperatingHours(shop.OpeningTime.Value, shop.ClosingTime.Value, startLocalTime) ||
                !IsWithinOperatingHours(shop.OpeningTime.Value, shop.ClosingTime.Value, endLocalTime))
            {
                throw AppException.BadRequest($"Flash deal timing must fall within shop operating hours ({shop.OpeningTime.Value:HH\\:mm} - {shop.ClosingTime.Value:HH\\:mm}).");
            }
        }
        if (request.OrderEndTime > request.ShopClosingTime)
        {
            throw AppException.BadRequest("Order end time cannot exceed shop closing time.");
        }

        var hasOverlap = await _dealRepository.HasOverlappingDealAsync(
            shop.Id,
            request.ProductId,
            effectiveStartTime,
            request.OrderEndTime,
            null,
            cancellationToken);
        if (hasOverlap)
        {
            throw AppException.BadRequest("An active or pending flash deal already exists for this product in the selected time range.");
        }

        var product = await _dealRepository.GetProductForDealCreationAsync(shop.Id, request.ProductId, cancellationToken)
            ?? throw AppException.NotFound("Product not found or does not belong to your shop.");
        if (await WarningDealPolicy.IsBlockedAsync(_db, product.Id, cancellationToken))
            throw AppException.Conflict("Product is blocked from opening deals due to an overdue warning. Submit corrections for admin approval.");
        if (product.Status != ProductStatus.Active)
        {
            throw AppException.BadRequest("Cannot create flash deal for an inactive product.");
        }

        var isOpenNow = request.IsImmediate || (now >= effectiveStartTime && now <= request.OrderEndTime);
        var initialStatus = isOpenNow ? FlashDealStatus.OnSale : FlashDealStatus.Pending;

        var deal = new FlashDeal
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            ProductId = product.Id,
            SaleStartTime = effectiveStartTime,
            OrderEndTime = request.OrderEndTime,
            ShopClosingTime = request.ShopClosingTime,
            Status = initialStatus,
            FeedPublishedAt = isOpenNow ? effectiveStartTime : now,
            VisibilityVersion = 1,
            PublicationId = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
        {
            FlashDealId = deal.Id,
            FromStatus = null,
            ToStatus = initialStatus,
            ChangedByType = "Shop",
            ChangedByUserId = userId,
            Note = "Deal created.",
            CreatedAt = now
        });

        var existingVariants = product.Variants.ToDictionary(v => v.Id);
        foreach (var reqVariant in request.Variants)
        {
            if (!existingVariants.TryGetValue(reqVariant.VariantId, out var productVariant))
            {
                throw AppException.BadRequest($"Variant with ID {reqVariant.VariantId} does not belong to this product.");
            }
            if (reqVariant.DealPrice >= productVariant.OriginalPrice)
            {
                throw AppException.BadRequest($"Deal price ({reqVariant.DealPrice}) must be less than original price ({productVariant.OriginalPrice}).");
            }
            var discountPercent = Math.Round(((productVariant.OriginalPrice - reqVariant.DealPrice) / productVariant.OriginalPrice) * 100, 2);
            deal.Variants.Add(new FlashDealVariant
            {
                Id = Guid.NewGuid(),
                FlashDealId = deal.Id,
                VariantId = reqVariant.VariantId,
                OriginalPrice = productVariant.OriginalPrice,
                DealPrice = reqVariant.DealPrice,
                DiscountPercent = discountPercent,
                TotalQuantity = reqVariant.TotalQuantity,
                ReservedQuantity = 0,
                SoldQuantity = 0,
                Status = FlashDealVariantStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        Guid? openingMessageId = null;
        await using (var transaction = await _db.Database.BeginTransactionAsync(cancellationToken))
        {
            await _dealRepository.AddAsync(deal, cancellationToken);
            if (isOpenNow && deal.Variants.Any(v => v.TotalQuantity > 0 && existingVariants[v.VariantId].IsActive))
            {
                openingMessageId = await ShopDealOpenedOutbox.StageAsync(_db, deal.Id, shop.Id, now, cancellationToken);
                _db.TrackNotificationOutbox(openingMessageId.Value);
                FlashDealLifecycleOutbox.StageOpened(_db, deal.Id, now: now);
            }
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        // Deliver before touching Hangfire: an immediate opening must not wait for scheduling or the lifecycle queue.
        if (openingMessageId.HasValue)
        {
            var publishTimer = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                await using var scope = (_scopes ?? throw new InvalidOperationException("Delivery scope factory is required."))
                    .CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<SaveBite.Backend.BackgroundJobs.Jobs.OrderEventDispatchJob>()
                    .ExecuteAsync(openingMessageId.Value, timeout.Token);
                _logger.LogInformation("Immediate opening notification published for deal {DealId}, outbox {MessageId}: publish={PublishMs}ms, sinceOpening={SinceOpeningMs}ms.", deal.Id, openingMessageId.Value, publishTimer.ElapsedMilliseconds, (DateTime.UtcNow - now).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Deal {DealId} committed; immediate notification deferred to outbox recovery {MessageId}.", deal.Id, openingMessageId.Value);
            }
        }

        await RefreshScheduleAsync(deal, cancellationToken);
        var createdDeal = await _dealRepository.GetShopDealByIdAsync(shop.Id, deal.Id, cancellationToken);
        return MapToShopDetailResponse(createdDeal!);
    }

    // 4. Update Flash Deal
    public async Task<ShopFlashDealDetailResponse> UpdateDealAsync(
        Guid userId,
        Guid dealId,
        UpdateFlashDealRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {dealId} FOR UPDATE", cancellationToken);
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");
        var product = await _dealRepository.GetProductForDealCreationAsync(shop.Id, deal.ProductId, cancellationToken)
            ?? throw AppException.NotFound("Product not found or does not belong to your shop.");


        var now = DateTime.UtcNow;
        var previousStatus = deal.Status;
        var isReopening = deal.Status is FlashDealStatus.Closed or FlashDealStatus.Cancelled || deal.OrderEndTime <= now;
        if ((isReopening || deal.Status == FlashDealStatus.Pending) &&
            request.DurationMinutes.HasValue)
        {
            var window = FlashDealTiming.ImmediateWindow(now, request.DurationMinutes.Value,
                request.ShopClosingTime - request.OrderEndTime);
            request.SaleStartTime = now;
            request.OrderEndTime = window.End;
            request.ShopClosingTime = window.Closing;
        }

        // Check overlapping deals.
        var hasOverlap = await _dealRepository.HasOverlappingDealAsync(
            shop.Id,
            deal.ProductId,
            request.SaleStartTime,
            request.OrderEndTime,
            deal.Id,
            cancellationToken);
        if (hasOverlap)
        {
            throw AppException.BadRequest("Another flash deal already overlaps with the updated schedule.");
        }
        if (shop.OpeningTime.HasValue && shop.ClosingTime.HasValue)
        {
            var startLocalTime = TimeOnly.FromDateTime(request.SaleStartTime.AddHours(7));
            var endLocalTime = TimeOnly.FromDateTime(request.OrderEndTime.AddHours(7));
            if (!IsWithinOperatingHours(shop.OpeningTime.Value, shop.ClosingTime.Value, startLocalTime) ||
                !IsWithinOperatingHours(shop.OpeningTime.Value, shop.ClosingTime.Value, endLocalTime))
            {
                throw AppException.BadRequest($"Flash deal timing must fall within shop operating hours ({shop.OpeningTime.Value:HH\\:mm} - {shop.ClosingTime.Value:HH\\:mm}).");
            }
        }


        if (isReopening || deal.Status == FlashDealStatus.Pending)
        {
            // As with creation, a valid 30-120 minute window may cross midnight.
            var duration = request.OrderEndTime - request.SaleStartTime;
            if (duration < TimeSpan.FromMinutes(TimingConfiguration.FlashDeals.MinimumDurationMinutes) || duration > TimeSpan.FromMinutes(TimingConfiguration.FlashDeals.MaximumDurationMinutes))
            {
                throw AppException.BadRequest($"The duration of a flash deal must be between {TimingConfiguration.FlashDeals.MinimumDurationMinutes} minutes and a maximum of {TimingConfiguration.FlashDeals.MaximumDurationMinutes} minutes.");
            }

            var isSellImmediately = request.SaleStartTime <= now.AddMinutes(TimingConfiguration.FlashDeals.ImmediateStartToleranceMinutes);
            if (!isSellImmediately)
            {
                if (request.SaleStartTime < now.AddMinutes(TimingConfiguration.FlashDeals.MinimumScheduleAheadMinutes))
                {
                    throw AppException.BadRequest($"The scheduled start time must be at least {TimingConfiguration.FlashDeals.MinimumScheduleAheadMinutes} minutes from the present, or you may choose to start immediately.");
                }
                if (request.SaleStartTime > now.AddMinutes(TimingConfiguration.FlashDeals.MaximumScheduleAheadMinutes))
                {
                    throw AppException.BadRequest($"Sales can be scheduled to open no more than {TimingConfiguration.FlashDeals.MaximumScheduleAheadMinutes} minutes in advance.");
                }
                deal.SaleStartTime = request.SaleStartTime;
                deal.Status = FlashDealStatus.Pending;
            }
            else
            {
                if (request.OrderEndTime <= now)
                {
                    throw AppException.BadRequest("Order end time must be in the future.");
                }
                deal.SaleStartTime = now;
                deal.Status = FlashDealStatus.OnSale;
            }

            if (isReopening)
            {
                deal.FeedPublishedAt = now;
                deal.VisibilityVersion++;
                deal.PublicationId = Guid.NewGuid();
                _db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
                {
                    Id = Guid.NewGuid(),
                    FlashDealId = deal.Id,
                    FromStatus = previousStatus,
                    ToStatus = deal.Status,
                    ChangedByType = "Shop",
                    ChangedByUserId = userId,
                    CreatedAt = now
                });
            }
        }
        else if (deal.Status is FlashDealStatus.OnSale or FlashDealStatus.SoldOut)
        {
            if (request.OrderEndTime - deal.SaleStartTime > TimeSpan.FromMinutes(TimingConfiguration.FlashDeals.MaximumDurationMinutes))
            {
                throw AppException.BadRequest($"Order end time cannot exceed {TimingConfiguration.FlashDeals.MaximumDurationMinutes} minutes from the sale start time.");
            }
            if (request.OrderEndTime <= now)
            {
                throw AppException.BadRequest("Order end time must be in the future.");
            }
        }

        deal.LifecycleSyncedAt = null;
        deal.OrderEndTime = request.OrderEndTime;
        deal.ShopClosingTime = request.ShopClosingTime;
        deal.UpdatedAt = now;

        var existingDealVariants = deal.Variants.ToDictionary(v => v.VariantId);
        var productVariants = product.Variants.ToDictionary(v => v.Id);

        foreach (var reqVariant in request.Variants)
        {
            if (existingDealVariants.TryGetValue(reqVariant.VariantId, out var dealVariant))
            {
                if (reqVariant.TotalQuantity < dealVariant.SoldQuantity + dealVariant.ReservedQuantity)
                {
                    throw AppException.BadRequest($"New total quantity cannot be less than already sold/reserved quantity ({dealVariant.SoldQuantity + dealVariant.ReservedQuantity}).");
                }
                dealVariant.TotalQuantity = reqVariant.TotalQuantity;
                dealVariant.DealPrice = reqVariant.DealPrice;
                dealVariant.DiscountPercent = Math.Round(((dealVariant.OriginalPrice - reqVariant.DealPrice) / dealVariant.OriginalPrice) * 100, 2);
                dealVariant.Status = reqVariant.Status;
                dealVariant.UpdatedAt = now;
            }
            else
            {
                if (!productVariants.TryGetValue(reqVariant.VariantId, out var productVariant))
                {
                    throw AppException.BadRequest($"Variant with ID {reqVariant.VariantId} does not belong to this product.");
                }
                if (reqVariant.DealPrice >= productVariant.OriginalPrice)
                {
                    throw AppException.BadRequest($"Deal price ({reqVariant.DealPrice}) must be less than original price ({productVariant.OriginalPrice}).");
                }
                var discountPercent = Math.Round(((productVariant.OriginalPrice - reqVariant.DealPrice) / productVariant.OriginalPrice) * 100, 2);
                deal.Variants.Add(new FlashDealVariant
                {
                    Id = Guid.NewGuid(),
                    FlashDealId = deal.Id,
                    VariantId = reqVariant.VariantId,
                    OriginalPrice = productVariant.OriginalPrice,
                    DealPrice = reqVariant.DealPrice,
                    DiscountPercent = discountPercent,
                    TotalQuantity = reqVariant.TotalQuantity,
                    ReservedQuantity = 0,
                    SoldQuantity = 0,
                    Status = reqVariant.Status,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }
        await _dealRepository.UpdateAsync(deal, cancellationToken);

        if (isReopening && deal.Status == FlashDealStatus.OnSale)
        {
            var openingMessageId = await ShopDealOpenedOutbox.StageAsync(_db, deal.Id, shop.Id, now, cancellationToken);
            _db.TrackNotificationOutbox(openingMessageId);
            FlashDealLifecycleOutbox.StageOpened(_db, deal.Id, now: now);
        }

        await RefreshScheduleAsync(deal, cancellationToken);
        LifecycleOutbox.InventoryDeals(_db, new[] { deal.Id }, null, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MapToShopDetailResponse(deal);
    }
    // 5. Cancel Flash Deal
    public async Task CancelDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {dealId} FOR UPDATE", cancellationToken);
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");

        if (deal.Status == FlashDealStatus.Cancelled || deal.Status == FlashDealStatus.Closed)
        {
            throw AppException.BadRequest("This flash deal is already finished or cancelled.");
        }

        var previousStatus = deal.Status;
        deal.Status = FlashDealStatus.Cancelled;
        deal.LifecycleSyncedAt = null;
        deal.UpdatedAt = DateTime.UtcNow;
        _db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
        {
            FlashDealId = deal.Id,
            FromStatus = previousStatus,
            ToStatus = FlashDealStatus.Cancelled,
            ChangedByType = "Shop",
            ChangedByUserId = userId,
            CreatedAt = deal.UpdatedAt
        });
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        FlashDealLifecycleOutbox.StageClosed(_db, deal.Id, deal.UpdatedAt);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

    }
    // Cancel live and scheduled deals when the shop is suspended; retries safely skip already closed deals.
    public async Task CancelOpenDealsForSuspendedShopAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        var closedDealIds = new List<Guid>();
        await using (var transaction = await _db.Database.BeginTransactionAsync(cancellationToken))
        {
            var dealIds = await _db.FlashDeals
                .Where(deal => deal.ShopId == shopId
                    && (deal.Status == FlashDealStatus.Pending
                        || deal.Status == FlashDealStatus.OnSale
                        || deal.Status == FlashDealStatus.SoldOut))
                .OrderBy(deal => deal.Id)
                .Select(deal => deal.Id)
                .ToListAsync(cancellationToken);

            foreach (var dealId in dealIds)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT id FROM flash_deals WHERE id = {dealId} FOR UPDATE",
                    cancellationToken);
                var deal = await _dealRepository.GetShopDealByIdAsync(shopId, dealId, cancellationToken);
                if (deal == null || deal.Status is not (FlashDealStatus.Pending or FlashDealStatus.OnSale or FlashDealStatus.SoldOut))
                    continue;

                var previousStatus = deal.Status;
                deal.Status = FlashDealStatus.Cancelled;
                deal.LifecycleSyncedAt = null;
                deal.UpdatedAt = DateTime.UtcNow;
                _db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
                {
                    FlashDealId = deal.Id,
                    FromStatus = previousStatus,
                    ToStatus = FlashDealStatus.Cancelled,
                    ChangedByType = "System",
                    Note = "Automatically cancelled because the shop was suspended.",
                    CreatedAt = deal.UpdatedAt
                });
                await _dealRepository.UpdateAsync(deal, cancellationToken);
                FlashDealLifecycleOutbox.StageClosed(_db, deal.Id, deal.UpdatedAt);
                closedDealIds.Add(deal.Id);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var dealId in closedDealIds)
        {
            await TryPublishLifecycleAsync(
                RabbitMqRoutingKeys.FlashDealClosed,
                new FlashDealEvent { FlashDealId = dealId },
                cancellationToken);
        }
    }
    // 6. Hide Flash Deal
    public async Task HideDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {dealId} FOR UPDATE", cancellationToken);
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");
        if (deal.Status != FlashDealStatus.OnSale && deal.Status != FlashDealStatus.Pending && deal.Status != FlashDealStatus.SoldOut)
        {
            throw AppException.BadRequest("Only active or pending deals can be hidden.");
        }
        var previousStatus = deal.Status;
        deal.Status = FlashDealStatus.Hidden;
        deal.VisibilityVersion++;
        deal.LifecycleSyncedAt = null;
        var now = DateTime.UtcNow;
        deal.UpdatedAt = now;

        // Calculate remaining duration at pause moment (Pause & Resume Duration)
        TimeSpan remainingTime;
        if (now < deal.SaleStartTime)
        {
            remainingTime = deal.OrderEndTime - deal.SaleStartTime;
        }
        else if (now < deal.OrderEndTime)
        {
            remainingTime = deal.OrderEndTime - now;
        }
        else
        {
            remainingTime = TimeSpan.Zero;
        }

        var remainingSeconds = (int)Math.Max(0, remainingTime.TotalSeconds);

        _db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
        {
            FlashDealId = deal.Id,
            FromStatus = previousStatus,
            ToStatus = deal.Status,
            ChangedByType = "Shop",
            ChangedByUserId = userId,
            Note = $"RemainingSeconds:{remainingSeconds}",
            CreatedAt = deal.UpdatedAt
        });
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        FlashDealLifecycleOutbox.StageHidden(_db, deal.Id, deal.UpdatedAt);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    // 7. Unhide / Re-open Flash Deal (Pause & Resume Duration)
    public async Task UnhideDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {dealId} FOR UPDATE", cancellationToken);
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");

        if (deal.Status != FlashDealStatus.Hidden)
        {
            throw AppException.BadRequest("Only hidden deals can be restored to visible status.");
        }
        var now = DateTime.UtcNow;

        if (await WarningDealPolicy.IsBlockedAsync(_db, deal.ProductId, cancellationToken))
            throw AppException.Conflict("Product has an overdue warning and cannot reopen deals.");

        // 1. Retrieve preserved remaining duration when the deal was hidden
        TimeSpan remainingDuration = TimeSpan.Zero;
        var lastHiddenHistory = await _db.FlashDealStatusHistories
            .Where(h => h.FlashDealId == deal.Id && h.ToStatus == FlashDealStatus.Hidden)
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastHiddenHistory?.Note != null && lastHiddenHistory.Note.StartsWith("RemainingSeconds:"))
        {
            if (int.TryParse(lastHiddenHistory.Note["RemainingSeconds:".Length..], out var sec) && sec > 0)
            {
                remainingDuration = TimeSpan.FromSeconds(sec);
            }
        }

        // Fallback for deals hidden before this feature
        if (remainingDuration <= TimeSpan.Zero)
        {
            if (deal.OrderEndTime > deal.UpdatedAt)
            {
                remainingDuration = deal.OrderEndTime - deal.UpdatedAt;
            }
            else if (deal.OrderEndTime > deal.SaleStartTime)
            {
                remainingDuration = deal.OrderEndTime - deal.SaleStartTime;
            }
            else
            {
                remainingDuration = TimeSpan.FromMinutes(60);
            }
        }

        // Minimum duration threshold (at least 5 minutes)
        if (remainingDuration < TimeSpan.FromMinutes(5))
        {
            remainingDuration = TimeSpan.FromMinutes(5);
        }

        // 2. Resume timing: start NOW, end at NOW + remainingDuration (Bảo lưu thời lượng còn lại)
        var pickupBuffer = deal.ShopClosingTime > deal.OrderEndTime
            ? deal.ShopClosingTime - deal.OrderEndTime
            : TimeSpan.FromMinutes(30);

        deal.SaleStartTime = now;
        deal.OrderEndTime = now.Add(remainingDuration);
        deal.ShopClosingTime = deal.OrderEndTime.Add(pickupBuffer);

        // Check operating hours if configured
        if (shop.OpeningTime.HasValue && shop.ClosingTime.HasValue)
        {
            var startLocalTime = TimeOnly.FromDateTime(deal.SaleStartTime.AddHours(7));
            if (!IsWithinOperatingHours(shop.OpeningTime.Value, shop.ClosingTime.Value, startLocalTime))
            {
                throw AppException.BadRequest($"Cannot reopen deal outside shop operating hours ({shop.OpeningTime.Value:HH:mm} - {shop.ClosingTime.Value:HH:mm}).");
            }
        }

        var previousStatus = deal.Status;
        deal.Status = FlashDealStatus.OnSale;

        deal.FeedPublishedAt = now;
        deal.VisibilityVersion++;
        deal.PublicationId = Guid.NewGuid();
        deal.LifecycleSyncedAt = null;
        deal.UpdatedAt = now;
        _db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
        {
            FlashDealId = deal.Id,
            FromStatus = previousStatus,
            ToStatus = deal.Status,
            ChangedByType = "Shop",
            ChangedByUserId = userId,
            Note = $"ResumedWithDurationMinutes:{(int)remainingDuration.TotalMinutes}",
            CreatedAt = now
        });
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        await RefreshScheduleAsync(deal, cancellationToken);

        if (deal.Status == FlashDealStatus.OnSale)
        {
            var openingMessageId = await ShopDealOpenedOutbox.StageAsync(_db, deal.Id, shop.Id, now, cancellationToken);
            _db.TrackNotificationOutbox(openingMessageId);
            FlashDealLifecycleOutbox.StageOpened(_db, deal.Id, expectedDeadline: deal.OrderEndTime, now: now);
        }
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static ShopFlashDealListItemResponse MapToShopListItemResponse(FlashDeal deal)
    {
        var variants = deal.Variants;
        return new ShopFlashDealListItemResponse
        {
            Id = deal.Id,
            ProductId = deal.ProductId,
            ProductName = deal.Product?.Name ?? string.Empty,
            ProductImageUrl = deal.Product?.Images?.FirstOrDefault()?.ImageUrl,
            CategoryName = deal.Product?.Category?.Name,
            SaleStartTime = deal.SaleStartTime,
            OrderEndTime = deal.OrderEndTime,
            ShopClosingTime = deal.ShopClosingTime,
            Status = deal.Status,
            MinDealPrice = variants.Any() ? variants.Min(v => v.DealPrice) : 0,
            MaxOriginalPrice = variants.Any() ? variants.Max(v => v.OriginalPrice) : 0,
            MaxDiscountPercent = variants.Any() ? variants.Max(v => v.DiscountPercent) : 0,
            TotalQuantity = variants.Sum(v => v.TotalQuantity),
            ReservedQuantity = variants.Sum(v => v.ReservedQuantity),
            SoldQuantity = variants.Sum(v => v.SoldQuantity),
            CreatedAt = deal.CreatedAt
        };
    }

    private static ShopFlashDealDetailResponse MapToShopDetailResponse(FlashDeal deal)
    {
        var images = deal.Product?.Images?
            .Select(i => i.ImageUrl)
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList() ?? new List<string>();

        return new ShopFlashDealDetailResponse
        {
            Id = deal.Id,
            ShopId = deal.ShopId,
            ProductId = deal.ProductId,
            ProductName = deal.Product?.Name ?? string.Empty,
            Description = deal.Product?.Description,
            CategoryName = deal.Product?.Category?.Name,
            PrimaryImageUrl = deal.Product?.Images?.FirstOrDefault()?.ImageUrl ?? images.FirstOrDefault(),
            ProductImageUrls = images,
            SaleStartTime = deal.SaleStartTime,
            OrderEndTime = deal.OrderEndTime,
            ShopClosingTime = deal.ShopClosingTime,
            Status = deal.Status,
            CreatedAt = deal.CreatedAt,
            UpdatedAt = deal.UpdatedAt,
            StatusHistory = deal.StatusHistory
                .OrderByDescending(history => history.CreatedAt)
                .Select(history => new ShopFlashDealStatusHistoryResponse
                {
                    FromStatus = history.FromStatus,
                    ToStatus = history.ToStatus,
                    ChangedByType = history.ChangedByType,
                    ChangedByUserId = history.ChangedByUserId,
                    Note = history.Note,
                    CreatedAt = history.CreatedAt
                }).ToList(),
            Variants = deal.Variants.Select(v => new ShopFlashDealVariantDetailResponse
            {
                Id = v.Id,
                VariantId = v.VariantId,
                Sku = v.Variant?.Sku,
                VariantName = v.Variant?.VariantValues?.Any() == true
                    ? string.Join(" - ", v.Variant.VariantValues.Where(vv => vv.AttributeValue != null).Select(vv => vv.AttributeValue!.Value))
                    : (v.Variant?.Sku ?? "Default"),
                ImageUrl = v.Variant?.VariantValues?.FirstOrDefault(vv => !string.IsNullOrEmpty(vv.AttributeValue?.ImageUrl))?.AttributeValue?.ImageUrl,
                OriginalPrice = v.OriginalPrice,
                DealPrice = v.DealPrice,
                DiscountPercent = v.DiscountPercent,
                TotalQuantity = v.TotalQuantity,
                ReservedQuantity = v.ReservedQuantity,
                SoldQuantity = v.SoldQuantity,
                Status = v.Status
            }).ToList()
        };
    }

    public async Task<List<ShopEligibleProductResponse>> GetShopEligibleProductsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var products = await _dealRepository.GetShopEligibleProductsAsync(shop.Id, cancellationToken);
        var activeProductIds = await _db.FlashDeals
            .Where(d => d.ShopId == shop.Id && (d.Status == FlashDealStatus.OnSale || d.Status == FlashDealStatus.Pending))
            .Select(d => d.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return products.Select(p => new ShopEligibleProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            HasActiveDeal = activeProductIds.Contains(p.Id),
            CategoryName = p.Category?.Name,
            ImageUrl = p.Images?.FirstOrDefault()?.ImageUrl,
            Variants = p.Variants.Where(v => v.IsActive).Select(v => new ShopEligibleProductVariantResponse
            {
                Id = v.Id,
                Sku = v.Sku,
                VariantName = v.VariantValues.Any()
                    ? string.Join(" - ", v.VariantValues.Where(vv => vv.AttributeValue != null).Select(vv => vv.AttributeValue!.Value))
                    : (v.Sku ?? "Default"),
                OriginalPrice = v.OriginalPrice,
                ImageUrl = v.VariantValues.FirstOrDefault(vv => !string.IsNullOrEmpty(vv.AttributeValue?.ImageUrl))?.AttributeValue?.ImageUrl
            }).ToList()
        }).ToList();
    }

    private async Task<Shop> GetShopForViewOrThrowAsync(Guid userId, Guid? requestedShopId, CancellationToken cancellationToken)
    {
        if (requestedShopId.HasValue && requestedShopId.Value != Guid.Empty)
        {
            var ownedShop = await _shopRepository.GetShopByOwnerUserIdAsync(userId, cancellationToken);
            if (ownedShop != null && ownedShop.Id == requestedShopId.Value)
            {
                return ownedShop;
            }

            var staff = await _staffRepository.GetStaffByShopAndUserAsync(requestedShopId.Value, userId, cancellationToken);
            if (staff != null && staff.Status == ShopStaffStatus.Active)
            {
                if (staff.Shop != null)
                {
                    return staff.Shop;
                }
                var shop = await _shopRepository.GetActiveShopByIdAsync(requestedShopId.Value, cancellationToken);
                if (shop != null)
                {
                    return shop;
                }
            }

            throw AppException.Forbidden("You do not have access to view this shop's flash deals.");
        }

        var shopByOwner = await _shopRepository.GetShopByOwnerUserIdAsync(userId, cancellationToken);
        if (shopByOwner != null)
        {
            return shopByOwner;
        }

        var memberships = await _staffRepository.GetShopsByStaffUserIdAsync(userId, cancellationToken);
        var activeMembership = memberships.FirstOrDefault(s => s.Status == ShopStaffStatus.Active);
        if (activeMembership != null)
        {
            if (activeMembership.Shop != null)
            {
                return activeMembership.Shop;
            }
            var shop = await _shopRepository.GetActiveShopByIdAsync(activeMembership.ShopId, cancellationToken);
            if (shop != null)
            {
                return shop;
            }
        }

        throw AppException.Forbidden("You do not own or work at any shop.");
    }

    private async Task<Shop> GetShopOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _shopRepository.GetShopByOwnerUserIdAsync(userId, cancellationToken)
            ?? throw AppException.Forbidden("You do not own any shop.");
    }

    private static bool IsWithinOperatingHours(TimeOnly opening, TimeOnly closing, TimeOnly time)
    {
        if (opening == closing) return true;
        return opening <= closing
            ? time >= opening && time <= closing
            : time >= opening || time <= closing;
    }
}
