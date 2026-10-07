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

    public async Task<List<FlashDealResponse>> GetNearbyAsync(
        NearbyFlashDealsRequest request,
        CancellationToken cancellationToken = default)
    {
        var latitude = request.Latitude ?? 0;
        var longitude = request.Longitude ?? 0;

        return await _redisService.GetNearbyDealsAsync(
            latitude,
            longitude,
            request.RadiusInKm);
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
        var responses = dbDeals.Select(MapToResponse).ToList();

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

    public async Task SeedSampleDealsAsync(CancellationToken cancellationToken = default)
    {
        var shops = await _dealRepository.GetSampleShopsForSeedAsync(10, cancellationToken);
        if (shops.Count == 0)
        {
            _logger.LogWarning("No shops found in the database to seed flash deal data.");
            return;
        }

        foreach (var shop in shops)
        {
            var dbDeals = await _dealRepository.GetActiveDealsByShopIdAsync(shop.Id, cancellationToken);

            if (dbDeals.Count > 0)
            {
                foreach (var deal in dbDeals)
                {
                    var response = MapToResponse(deal);
                    await _redisService.SaveDealAsync(response, shop.Latitude, shop.Longitude);
                }
            }
            else
            {
                // If shop has no active deals in DB, create a sample deal and save to Redis
                var sampleDeal = new FlashDealResponse
                {
                    Id = Guid.NewGuid(),
                    ShopId = shop.Id,
                    ShopName = shop.Name,
                    ShopAddress = shop.AddressLine,
                    ShopLogoUrl = shop.LogoUrl,
                    ProductId = Guid.NewGuid(),
                    ProductName = $"Promotional Item at {shop.Name}",
                    ProductImageUrl = shop.CoverImageUrl,
                    SaleStartTime = DateTime.UtcNow,
                    OrderEndTime = DateTime.UtcNow.AddHours(3),
                    ShopClosingTime = DateTime.UtcNow.AddHours(5),
                    Status = FlashDealStatus.OnSale,
                    MinDealPrice = 25000,
                    MaxOriginalPrice = 50000,
                    MaxDiscountPercent = 50,
                    Variants = new List<FlashDealVariantResponse>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            VariantId = Guid.NewGuid(),
                            Sku = "SAMPLE-DEAL-01",
                            OriginalPrice = 50000,
                            DealPrice = 25000,
                            DiscountPercent = 50,
                            TotalQuantity = 20,
                            SoldQuantity = 0,
                            Status = FlashDealVariantStatus.Active
                        }
                    }
                };

                await _redisService.SaveDealAsync(sampleDeal, shop.Latitude, shop.Longitude);
            }
        }

        _logger.LogInformation("Successfully synced sample flash deals to Redis.");
    }

    private static FlashDealResponse MapToResponse(FlashDeal deal)
    {
        var variants = deal.Variants.Select(v => new FlashDealVariantResponse
        {
            Id = v.Id,
            VariantId = v.VariantId,
            Sku = v.Variant?.Sku,
            OriginalPrice = v.OriginalPrice,
            DealPrice = v.DealPrice,
            DiscountPercent = v.DiscountPercent,
            TotalQuantity = v.TotalQuantity,
            SoldQuantity = v.SoldQuantity,
            Status = v.Status
        }).ToList();

        var minPrice = variants.Count > 0 ? variants.Min(v => v.DealPrice) : 0;
        var maxOriginal = variants.Count > 0 ? variants.Max(v => v.OriginalPrice) : 0;
        var maxDiscount = variants.Count > 0 ? variants.Max(v => v.DiscountPercent) : 0;

        return new FlashDealResponse
        {
            Id = deal.Id,
            ShopId = deal.ShopId,
            ShopName = deal.Shop?.Name ?? string.Empty,
            ShopAddress = deal.Shop?.AddressLine ?? string.Empty,
            ShopLogoUrl = deal.Shop?.LogoUrl,
            ProductId = deal.ProductId,
            ProductName = deal.Product?.Name ?? string.Empty,
            ProductImageUrl = deal.Product?.Images?.FirstOrDefault()?.ImageUrl,
            SaleStartTime = deal.SaleStartTime,
            OrderEndTime = deal.OrderEndTime,
            ShopClosingTime = deal.ShopClosingTime,
            Status = deal.Status,
            MinDealPrice = minPrice,
            MaxOriginalPrice = maxOriginal,
            MaxDiscountPercent = maxDiscount,
            Variants = variants
        };
    }
}