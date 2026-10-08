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
        var latitude = request.Latitude.GetValueOrDefault() != 0 ? request.Latitude!.Value : 10.7626;
        var longitude = request.Longitude.GetValueOrDefault() != 0 ? request.Longitude!.Value : 106.6601;

        var deals = await _redisService.GetNearbyDealsAsync(
            latitude,
            longitude,
            request.RadiusInKm);

        if (deals.Count == 0)
        {
            var dbDeals = await _dealRepository.GetAllActiveDealsAsync(cancellationToken);
            if (dbDeals.Count > 0)
            {
                foreach (var deal in dbDeals)
                {
                    if (deal.Shop != null)
                    {
                        var response = MapToResponse(deal);
                        await _redisService.SaveDealAsync(
                            response,
                            deal.Shop.Latitude,
                            deal.Shop.Longitude);
                    }
                }

                deals = await _redisService.GetNearbyDealsAsync(
                    latitude,
                    longitude,
                    request.RadiusInKm);
            }
        }

        return deals;
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

    private static FlashDealResponse MapToResponse(FlashDeal deal)
    {
        var variants = deal.Variants.Select(v => new FlashDealVariantResponse
        {
            Id = v.Id,
            VariantId = v.VariantId,
            Sku = v.Variant?.Sku,
            VariantName = v.Variant?.Sku,
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

        var images = deal.Product?.Images?
            .Select(i => i.ImageUrl)
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList() ?? new List<string>();

        var primaryImage = deal.Product?.Images?.FirstOrDefault()?.ImageUrl;
        if (images.Count == 0 && !string.IsNullOrEmpty(primaryImage))
        {
            images.Add(primaryImage);
        }

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
            Variants = variants
        };
    }
}