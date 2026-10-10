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



        // Áp dụng bộ lọc (Categories, Distance, Price, ShopName)
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

        // Sắp xếp các deal theo CreatedAt giảm dần (mới nhất lên đầu)
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
            // ─────────────────────────────────────────────────────────────────
            // DUAL-CURSOR: Kết hợp con trỏ 1 + con trỏ 2
            //
            // Bước 1 (Cursor 2): Lấy tất cả deal được tạo MỚI HƠN cursor2
            //   (được tạo sau khi page 1 đã load) → prepend lên đầu page mới.
            //
            // Bước 2 (Cursor 1): Lấy tối đa <Limit> deal CŨ HƠN cursor1
            //   → tiếp tục phân trang theo chiều xuống.
            //
            // Kết quả trả về: [deal mới] + [deal cũ]
            // ─────────────────────────────────────────────────────────────────

            // Bước 1: deal mới hơn cursor2 (nếu có cursor2)
            var newerPrepend = new List<FlashDealResponse>();
            if (request.Cursor2.HasValue)
            {
                newerPrepend = sortedDeals
                    .Where(d => d.CreatedAt.ToUniversalTime() > request.Cursor2.Value.ToUniversalTime())
                    .OrderByDescending(d => d.CreatedAt.ToUniversalTime())
                    .ToList();

                if (newerPrepend.Count > 0)
                {
                    // Đẩy cursor2 lên mốc mới nhất vừa được tải
                    cursor2 = newerPrepend.Max(d => d.CreatedAt);
                    prependedCount = newerPrepend.Count;
                }
            }

            // Bước 2: deal cũ hơn cursor1
            var olderPool = sortedDeals
                .Where(d => d.CreatedAt.ToUniversalTime() < request.Cursor1.Value.ToUniversalTime())
                .ToList();

            var olderPage = olderPool.Take(request.Limit).ToList();
            hasOlder = olderPool.Count > request.Limit;

            if (olderPage.Count > 0)
            {
                cursor1 = olderPage.Min(d => d.CreatedAt);
            }

            // Kết hợp: deal mới nhất prepend trước, rồi đến deal cũ
            resultDeals = newerPrepend.Concat(olderPage).ToList();
        }
        else if (request.Mode == CursorMode.Newer && request.Cursor2.HasValue)
        {
            // ─────────────────────────────────────────────────────────────────
            // POLLING ONLY – chỉ kiểm tra số lượng, không cập nhật cursor2.
            // Frontend hiển thị badge thông báo "X deal mới vừa xuất hiện!"
            // Cursor2 chỉ được cập nhật khi người dùng thực sự load page mới.
            // ─────────────────────────────────────────────────────────────────
            var newerDeals = sortedDeals
                .Where(d => d.CreatedAt.ToUniversalTime() > request.Cursor2.Value.ToUniversalTime())
                .ToList();

            resultDeals = new List<FlashDealResponse>(); // Không trả deal khi polling
            hasNewer = newerDeals.Count > 0;
            newDealsCount = newerDeals.Count;
            // cursor2 KHÔNG thay đổi – giữ nguyên mốc cũ để khi load page mới sẽ pick up đúng
        }
        else // CursorMode.Initial
        {
            // ─────────────────────────────────────────────────────────────────
            // INITIAL LOAD: 9 item mới nhất, khởi tạo cả 2 con trỏ.
            // cursor2 = createdAt của deal MỚI NHẤT (dùng detect deal tạo mới sau này)
            // cursor1 = createdAt của deal CŨ NHẤT trong page (dùng phân trang xuống)
            // ─────────────────────────────────────────────────────────────────
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

        // Fallback tự động: nếu deal được INSERT chay bằng SQL mà chưa chèn flash_deal_variants
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