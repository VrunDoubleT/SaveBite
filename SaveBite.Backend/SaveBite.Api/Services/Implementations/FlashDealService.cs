using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class FlashDealService : IFlashDealService
{
    private readonly IFlashDealRedisService _redisService;
    private readonly IFlashDealRepository _dealRepository;
    private readonly ILogger<FlashDealService> _logger;

    public FlashDealService(
        IFlashDealRedisService redisService,
        IFlashDealRepository dealRepository,
        ILogger<FlashDealService> logger)
    {
        _redisService = redisService;
        _dealRepository = dealRepository;
        _logger = logger;
    }

    public async Task<FlashDealCursorListResponse> GetNearbyAsync(
        FlashDealCursorRequest request,
        CancellationToken cancellationToken = default)
    {
        var latitude = request.Latitude.GetValueOrDefault() != 0 ? request.Latitude!.Value : 10.0240868;
        var longitude = request.Longitude.GetValueOrDefault() != 0 ? request.Longitude!.Value : 105.7660278;

        var allDeals = await _redisService.GetNearbyDealsAsync(
            latitude,
            longitude,
            request.RadiusInKm);



        // Apply category, distance, price, and shop-name filters.
        var filteredDeals = allDeals.AsEnumerable();

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

            // Combine Cursor1 and Cursor2 for pagination.

            // Step 1: Retrieve all deals created after Cursor2.
            // Prepend deals created after the initial page was loaded to the new page.

            // Step 2: Retrieve up to Limit deals created before Cursor1.
            // Continue pagination toward older deals.

            // Return new deals followed by older deals.


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

            // Polling only checks the count and does not update Cursor2.
            // The frontend displays a badge showing the number of newly available deals.
            // Update Cursor2 only when the user loads a new page.

            var newerDeals = sortedDeals
                .Where(d => d.CreatedAt.ToUniversalTime() > request.Cursor2.Value.ToUniversalTime())
                .ToList();

            resultDeals = new List<FlashDealResponse>(); // Do not return deals while polling.
            hasNewer = newerDeals.Count > 0;
            newDealsCount = newerDeals.Count;
            // Keep Cursor2 unchanged so the next page load retrieves all new deals.
        }
        else // CursorMode.Initial.
        {

            // Load the newest deals up to the requested limit and initialize both cursors.
            // Set Cursor2 to the newest deal timestamp to detect subsequently created deals.
            // Set Cursor1 to the oldest deal timestamp on the page to paginate toward older deals.

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
                Status = v.Status
            };
        }).ToList();

        // Use a fallback for deals inserted directly through SQL without flash_deal_variants.
        if (variants.Count == 0 && deal.Product?.Variants != null && deal.Product.Variants.Count > 0)
        {
            variants = deal.Product.Variants.Where(pv => pv.IsActive).Select(pv =>
            {
                var attrs = pv.VariantValues?
                    .Where(vv => vv.AttributeValue != null && vv.AttributeValue.Attribute != null)
                    .ToDictionary(
                        vv => vv.AttributeValue.Attribute.Name,
                        vv => vv.AttributeValue.Value
                    ) ?? new Dictionary<string, string>();

                var variantImg = pv.VariantValues?
                    .FirstOrDefault(vv => !string.IsNullOrEmpty(vv.AttributeValue?.ImageUrl))?
                    .AttributeValue?.ImageUrl;

                var vName = attrs.Count > 0
                    ? string.Join(" - ", attrs.Values)
                    : (pv.Sku ?? "Default");

                return new FlashDealVariantResponse
                {
                    Id = Guid.NewGuid(),
                    VariantId = pv.Id,
                    Sku = pv.Sku,
                    VariantName = vName,
                    ImageUrl = variantImg,
                    Attributes = attrs,
                    OriginalPrice = pv.OriginalPrice,
                    DealPrice = pv.DealPrice > 0 ? pv.DealPrice : pv.OriginalPrice,
                    DiscountPercent = pv.OriginalPrice > 0 ? Math.Round((pv.OriginalPrice - pv.DealPrice) / pv.OriginalPrice * 100, 2) : 0,
                    TotalQuantity = 50,
                    SoldQuantity = 0,
                    Status = FlashDealVariantStatus.Active
                };
            }).ToList();
        }

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
            CreatedAt = deal.CreatedAt
        };
    }
}
