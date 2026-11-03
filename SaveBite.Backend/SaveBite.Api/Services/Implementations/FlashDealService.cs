using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Caching.Interfaces;
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
    private readonly IFlashDealRedisService _redisService;
    private readonly IFlashDealRepository _dealRepository;
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly IShopRepository _shopRepository;
    private readonly IShopStaffRepository _staffRepository;
    private readonly ILogger<FlashDealService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRabbitMqPublisher _publisher;
    public FlashDealService(
        IFlashDealRedisService redisService,
        IFlashDealRepository dealRepository,
        IUserAddressRepository userAddressRepository,
        IShopRepository shopRepository,
        IShopStaffRepository staffRepository,
        IBackgroundJobClient backgroundJobClient,
        IRabbitMqPublisher publisher,
        ILogger<FlashDealService> logger, AppDbContext db)
    {
        _db = db;
        _redisService = redisService;
        _dealRepository = dealRepository;
        _userAddressRepository = userAddressRepository;
        _shopRepository = shopRepository;
        _staffRepository = staffRepository;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _publisher = publisher;
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
                Cursor1 = null,
                Cursor2 = null,
                HasOlder = false,
                HasNewer = false
            };
        }

        var allDeals = await _redisService.GetNearbyDealsAsync(
            latitude,
            longitude,
            request.RadiusInKm);



        // Apply keyword, category, distance, price, and shop-name filters.
        var filteredDeals = allDeals.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            filteredDeals = filteredDeals.Where(d =>
                (!string.IsNullOrEmpty(d.ProductName) && d.ProductName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(d.ShopName) && d.ShopName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(d.Description) && d.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var categories = request.Category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (categories.Length > 0)
            {
                filteredDeals = filteredDeals.Where(d => !string.IsNullOrEmpty(d.CategoryName) && categories.Contains(d.CategoryName, StringComparer.OrdinalIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ShopName))
        {
            filteredDeals = filteredDeals.Where(d => string.Equals(d.ShopName, request.ShopName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Distance))
        {
            var distList = request.Distance.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            filteredDeals = filteredDeals.Where(d =>
            {
                var dist = d.DistanceInKm ?? 0;
                foreach (var rule in distList)
                {
                    if (rule.Equals("Under 2km", StringComparison.OrdinalIgnoreCase) && dist < 2) return true;
                    if (rule.Equals("Under 5km", StringComparison.OrdinalIgnoreCase) && dist < 5) return true;
                    if (rule.Equals("Over 5km", StringComparison.OrdinalIgnoreCase) && dist >= 5) return true;
                }
                return false;
            });
        }

        if (!string.IsNullOrWhiteSpace(request.Price))
        {
            var priceList = request.Price.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            filteredDeals = filteredDeals.Where(d =>
            {
                var price = d.MinDealPrice;
                foreach (var rule in priceList)
                {
                    if (rule.Equals("Under 30k", StringComparison.OrdinalIgnoreCase) && price < 30000) return true;
                    if (rule.Equals("30k - 50k", StringComparison.OrdinalIgnoreCase) && price >= 30000 && price <= 50000) return true;
                    if (rule.Equals("Over 50k", StringComparison.OrdinalIgnoreCase) && price > 50000) return true;
                }
                return false;
            });
        }

        // Sort deals by CreatedAt in descending order, with the newest deals first.
        var sortedDeals = filteredDeals.OrderByDescending(d => d.CreatedAt.ToUniversalTime()).ToList();

        List<FlashDealResponse> resultDeals;
        bool hasOlder = false;
        bool hasNewer = false;
        int newDealsCount = 0;
        int prependedCount = 0;
        DateTime? cursor1 = request.Cursor1;
        DateTime? cursor2 = request.Cursor2;

        if (request.Mode == CursorMode.Older && request.Cursor1.HasValue)
        {

            // Step 1: Retrieve deals newer than Cursor2 when Cursor2 is provided.
            var newerPrepend = new List<FlashDealResponse>();
            if (request.Cursor2.HasValue)
            {
                newerPrepend = sortedDeals
                    .Where(d => d.CreatedAt.ToUniversalTime() > request.Cursor2.Value.ToUniversalTime())
                    .OrderByDescending(d => d.CreatedAt.ToUniversalTime())
                    .ToList();

                if (newerPrepend.Count > 0)
                {
                    // Advance Cursor2 to the newest timestamp loaded.
                    cursor2 = newerPrepend.Max(d => d.CreatedAt);
                    prependedCount = newerPrepend.Count;
                }
            }

            // Step 2: Retrieve deals older than Cursor1.
            var olderPool = sortedDeals
                .Where(d => d.CreatedAt.ToUniversalTime() < request.Cursor1.Value.ToUniversalTime())
                .ToList();

            var olderPage = olderPool.Take(request.Limit).ToList();
            hasOlder = olderPool.Count > request.Limit;

            if (olderPage.Count > 0)
            {
                cursor1 = olderPage.Min(d => d.CreatedAt);
            }

            // Prepend the newest deals, then append older deals.
            resultDeals = newerPrepend.Concat(olderPage).ToList();
        }
        else if (request.Mode == CursorMode.Newer && request.Cursor2.HasValue)
        {
            var newerDeals = sortedDeals
                .Where(d => d.CreatedAt.ToUniversalTime() > request.Cursor2.Value.ToUniversalTime())
                .ToList();

            resultDeals = new List<FlashDealResponse>();
            hasNewer = newerDeals.Count > 0;
            newDealsCount = newerDeals.Count;
        }
        else
        {

            resultDeals = sortedDeals.Take(request.Limit).ToList();
            hasOlder = sortedDeals.Count > request.Limit;

            if (resultDeals.Count > 0)
            {
                cursor1 = resultDeals.Min(d => d.CreatedAt);
                cursor2 = resultDeals.Max(d => d.CreatedAt);
            }
        }

        return new FlashDealCursorListResponse
        {
            Deals = resultDeals,
            Cursor1 = cursor1,
            Cursor2 = cursor2,
            HasOlder = hasOlder,
            HasNewer = hasNewer,
            PrependedCount = prependedCount,
            NewDealsCount = newDealsCount
        };
    }

    public async Task<List<FlashDealResponse>> GetByShopIdAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var cached = await _redisService.GetDealsByShopAsync(shopId);
        if (cached.Count > 0)
        {
            return cached;
        }

        var dbDeals = await _dealRepository.GetActiveDealsByShopIdAsync(shopId, cancellationToken);
        var responses = new List<FlashDealResponse>();

        foreach (var deal in dbDeals)
        {
            var response = MapToResponse(deal);
            responses.Add(response);
            if (deal.Shop != null)
            {
                await _redisService.SaveDealAsync(
                    response,
                    deal.Shop.Latitude,
                    deal.Shop.Longitude);
            }
        }

        return responses;
    }

    public async Task<FlashDealResponse> GetByIdAsync(
        Guid dealId,
        CancellationToken cancellationToken = default)
    {
        var cached = await _redisService.GetDealAsync(dealId);
        if (cached != null)
        {
            return cached;
        }

        var deal = await _dealRepository.GetActiveDealByIdAsync(dealId, cancellationToken);
        if (deal == null)
        {
            throw AppException.NotFound($"Flash deal with ID '{dealId}' was not found.");
        }

        var response = MapToResponse(deal);

        if (deal.Shop != null)
        {
            await _redisService.SaveDealAsync(
                response,
                deal.Shop.Latitude,
                deal.Shop.Longitude);
        }

        return response;
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

        // Validate time intervals.
        if (effectiveStartTime >= request.OrderEndTime)
        {
            throw AppException.BadRequest("Sale start time must be before order end time.");
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
            CreatedAt = now,
            UpdatedAt = now
        };

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
        await _dealRepository.AddAsync(deal, cancellationToken);
        // --- SCHEDULING AND EVENT DISPATCHING ---
        if (isOpenNow)
        {
            // 1. Open now: Publish flash sale.opened
            await _publisher.PublishAsync(
                RabbitMqRoutingKeys.FlashDealOpened,
                new FlashDealEvent { FlashDealId = deal.Id },
                cancellationToken);
        }
        else if (!request.IsImmediate && effectiveStartTime > now)
        {
            // 2. Future opening: Schedule Hangfire to run at SaleStartTime
            var delayToStart = effectiveStartTime - now;
            _backgroundJobClient.Schedule<IFlashDealJobService>(
                job => job.PublishDealOpenedAsync(deal.Id),
                delayToStart);
        }
        // 3. Always schedule the end time at OrderEndTime
        var delayToEnd = request.OrderEndTime - now;
        if (delayToEnd > TimeSpan.Zero)
        {
            _backgroundJobClient.Schedule<IFlashDealJobService>(
                job => job.PublishDealExpiredAsync(deal.Id),
                delayToEnd);
        }
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
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");
        var product = await _dealRepository.GetProductForDealCreationAsync(shop.Id, deal.ProductId, cancellationToken)
            ?? throw AppException.NotFound("Product not found or does not belong to your shop.");

        if (deal.Status == FlashDealStatus.Cancelled || deal.Status == FlashDealStatus.Closed)
        {
            throw AppException.BadRequest("Cannot update a closed or cancelled flash deal.");
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

        var now = DateTime.UtcNow;

        if (deal.Status == FlashDealStatus.Pending)
        {
            if (request.SaleStartTime.Date != request.OrderEndTime.Date)
            {
                throw AppException.BadRequest("Flash deals only take place within a single day.");
            }

            var duration = request.OrderEndTime - request.SaleStartTime;
            if (duration < TimeSpan.FromMinutes(30) || duration > TimeSpan.FromMinutes(120))
            {
                throw AppException.BadRequest("The duration of a flash deal must be between 30 minutes and a maximum of 120 minutes.");
            }

            var isSellImmediately = request.SaleStartTime <= now.AddMinutes(5);
            if (!isSellImmediately)
            {
                if (request.SaleStartTime < now.AddMinutes(15))
                {
                    throw AppException.BadRequest("The scheduled start time must be at least 15 minutes from the present, or you may choose to start immediately.");
                }
                if (request.SaleStartTime > now.AddMinutes(90))
                {
                    throw AppException.BadRequest("Sales can be scheduled to open no more than 90 minutes in advance.");
                }
            }

            deal.SaleStartTime = request.SaleStartTime;
        }
        else if (deal.Status == FlashDealStatus.OnSale)
        {
            if (request.OrderEndTime - deal.SaleStartTime > TimeSpan.FromMinutes(120))
            {
                throw AppException.BadRequest("Order end time cannot exceed 120 minutes from the sale start time.");
            }
            if (request.OrderEndTime <= now)
            {
                throw AppException.BadRequest("Order end time must be in the future.");
            }
        }

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

        // New completion timer with Hangfire
        var delayToEnd = request.OrderEndTime - DateTime.UtcNow;
        if (delayToEnd > TimeSpan.Zero)
        {
            _backgroundJobClient.Schedule<IFlashDealJobService>(
                job => job.PublishDealExpiredAsync(deal.Id),
                delayToEnd);
        }
        // If the deal is OnSale, update the Redis cache by publishing an event
        if (deal.Status == FlashDealStatus.OnSale)
        {
            await _publisher.PublishAsync(
                RabbitMqRoutingKeys.FlashDealOpened,
                new FlashDealEvent { FlashDealId = deal.Id },
                cancellationToken);
        }
        return MapToShopDetailResponse(deal);
    }
    // 5. Cancel Flash Deal
    public async Task CancelDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");

        if (deal.Status == FlashDealStatus.Cancelled || deal.Status == FlashDealStatus.Closed)
        {
            throw AppException.BadRequest("This flash deal is already finished or cancelled.");
        }

        deal.Status = FlashDealStatus.Cancelled;
        deal.UpdatedAt = DateTime.UtcNow;
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        // Fire an event to have the Consumer remove the deal from Redis
        await _publisher.PublishAsync(
            RabbitMqRoutingKeys.FlashDealClosed,
            new FlashDealEvent { FlashDealId = deal.Id },
            cancellationToken);

    }
    // 6. Hide Flash Deal
    public async Task HideDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");
        if (deal.Status != FlashDealStatus.OnSale && deal.Status != FlashDealStatus.Pending)
        {
            throw AppException.BadRequest("Only active or pending deals can be hidden.");
        }
        deal.Status = FlashDealStatus.Hidden;
        deal.UpdatedAt = DateTime.UtcNow;
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        // Emit an event for the Consumer to remove the temporary deal from Redis
        await _publisher.PublishAsync(
            RabbitMqRoutingKeys.FlashDealHidden,
            new FlashDealEvent { FlashDealId = deal.Id },
            cancellationToken);
    }
    // 7. Unhide Flash Deal
    public async Task UnhideDealAsync(Guid userId, Guid dealId, CancellationToken cancellationToken = default)
    {
        var shop = await GetShopOrThrowAsync(userId, cancellationToken);
        var deal = await _dealRepository.GetShopDealByIdAsync(shop.Id, dealId, cancellationToken)
            ?? throw AppException.NotFound("Flash deal not found.");

        if (deal.Status != FlashDealStatus.Hidden)
        {
            throw AppException.BadRequest("Only hidden deals can be restored to visible status.");
        }
        var now = DateTime.UtcNow;
        if (deal.OrderEndTime <= now)
        {
            throw AppException.BadRequest("This deal has already expired and cannot be restored.");
        }
        if (await WarningDealPolicy.IsBlockedAsync(_db, deal.ProductId, cancellationToken))
            throw AppException.Conflict("Product has an overdue warning and cannot reopen deals.");
        deal.Status = now >= deal.SaleStartTime ? FlashDealStatus.OnSale : FlashDealStatus.Pending;
        deal.UpdatedAt = now;
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        // If the sale start time has been reached, publish the event so that the Consumer can reload data into Redis
        if (deal.Status == FlashDealStatus.OnSale)
        {
            await _publisher.PublishAsync(
                RabbitMqRoutingKeys.FlashDealOpened,
                new FlashDealEvent { FlashDealId = deal.Id },
                cancellationToken);
        }
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

        return products.Select(p => new ShopEligibleProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
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
}
