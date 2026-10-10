using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopViewService : IShopViewService
{
    private readonly IShopRepository _shopRepository;

    public ShopViewService(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<PagedResult<NearbyShopResponse>> GetNearbyShopsAsync(
        NearbyShopsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 6 : request.PageSize;

        var shops = await _shopRepository.GetActiveShopsAsync(request.Keyword, cancellationToken);
        if (!shops.Any())
            return PagedResult<NearbyShopResponse>.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0);

        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var userLat = request.Latitude ?? 0;
        var userLon = request.Longitude ?? 0;

        var matchingShops = new List<(Shop Shop, double Distance, bool IsOpen)>();
        foreach (var shop in shops)
        {
            var distance = CalculateDistanceInKm(userLat, userLon, shop.Latitude, shop.Longitude);
            if (distance > request.RadiusInKm) continue;

            var isOpen = CheckIsOpen(shop.OpeningTime, shop.ClosingTime, nowTime);
            if (request.OnlyOpen == true && !isOpen) continue;

            matchingShops.Add((shop, distance, isOpen));
        }

        var totalItems = matchingShops.Count;
        var pagedShops = matchingShops
            .OrderBy(x => x.Distance)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagedShopIds = pagedShops.Select(x => x.Shop.Id).ToList();
        var reviewsStats = await _shopRepository.GetReviewsStatsForShopsAsync(pagedShopIds, cancellationToken);

        var results = new List<NearbyShopResponse>();
        foreach (var item in pagedShops)
        {
            var shop = item.Shop;
            reviewsStats.TryGetValue(shop.Id, out var stat);

            results.Add(new NearbyShopResponse
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                Address = $"{shop.AddressLine}, {shop.District}, {shop.City}",
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                DistanceInKm = item.Distance,
                LogoUrl = shop.LogoUrl,
                CoverImageUrl = shop.CoverImageUrl,
                OpeningTime = shop.OpeningTime,
                ClosingTime = shop.ClosingTime,
                IsOpen = item.IsOpen,
                AverageRating = stat.TotalCount > 0 ? Math.Round(stat.AvgRating, 1) : 5.0,
                TotalReviews = stat.TotalCount
            });
        }

        return PagedResult<NearbyShopResponse>.Create(results, page, pageSize, totalItems);
    }

    public async Task<ShopProfileResponse> GetShopProfileAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _shopRepository.GetActiveShopByIdAsync(shopId, cancellationToken);
        if (shop == null)
            throw AppException.NotFound("Shop not found or is currently inactive.");

        var (avgRating, totalReviews, breakdown) = await _shopRepository.GetShopReviewStatsAsync(shopId, cancellationToken);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        return new ShopProfileResponse
        {
            Id = shop.Id,
            Name = shop.Name,
            Description = shop.Description,
            AddressLine = shop.AddressLine,
            Ward = shop.Ward,
            District = shop.District,
            City = shop.City,
            Latitude = shop.Latitude,
            Longitude = shop.Longitude,
            LogoUrl = shop.LogoUrl,
            CoverImageUrl = shop.CoverImageUrl,
            OpeningTime = shop.OpeningTime,
            ClosingTime = shop.ClosingTime,
            IsOpen = CheckIsOpen(shop.OpeningTime, shop.ClosingTime, nowTime),
            AverageRating = Math.Round(avgRating, 1),
            TotalReviews = totalReviews,
            RatingBreakdown = breakdown
        };
    }

    public async Task<StoreReviewsSummaryResponse> GetStoreReviewsAsync(
        Guid shopId,
        StoreReviewsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var shopExists = await _shopRepository.ShopExistsAsync(shopId, cancellationToken);
        if (!shopExists)
            throw AppException.NotFound("Shop not found.");

        var (avgRating, totalReviews, breakdown) = await _shopRepository.GetShopReviewStatsAsync(shopId, cancellationToken);
        var (items, totalItems) = await _shopRepository.GetPagedStoreReviewsAsync(
            shopId, 
            request.Rating, 
            request.Page, 
            request.PageSize, 
            cancellationToken);

        var reviewResponses = items.Select(f => new StoreReviewResponse
        {
            Id = f.Id,
            UserId = f.UserId,
            UserFullName = f.User?.FullName ?? string.Empty,
            UserAvatarUrl = f.User?.AvatarUrl,
            Rating = f.Rating,
            Comment = f.Comment,
            ProductId = f.ProductId,
            ProductName = f.Product?.Name ?? string.Empty,
            ProductImageUrl = f.Product?.Images?.Select(i => i.ImageUrl).FirstOrDefault(),
            CreatedAt = f.CreatedAt,
            Reply = f.Reply != null
                ? new StoreReviewReplyResponse
                {
                    Id = f.Reply.Id,
                    Content = f.Reply.Content,
                    RepliedByName = f.Reply.RepliedByUser?.FullName ?? string.Empty,
                    CreatedAt = f.Reply.CreatedAt
                }
                : null
        }).ToList();

        return new StoreReviewsSummaryResponse
        {
            AverageRating = Math.Round(avgRating, 1),
            TotalReviews = totalReviews,
            RatingBreakdown = breakdown,
            Reviews = PagedResult<StoreReviewResponse>.Create(reviewResponses, request.Page, request.PageSize, totalItems)
        };
    }

    private static double CalculateDistanceInKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371.0;
        var dLat = (Math.PI / 180.0) * (lat2 - lat1);
        var dLon = (Math.PI / 180.0) * (lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos((Math.PI / 180.0) * lat1) * Math.Cos((Math.PI / 180.0) * lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(r * c, 2);
    }

    private static bool CheckIsOpen(TimeOnly? opening, TimeOnly? closing, TimeOnly now)
    {
        if (!opening.HasValue || !closing.HasValue) return true;
        return opening.Value <= closing.Value
            ? now >= opening.Value && now <= closing.Value
            : now >= opening.Value || now <= closing.Value;
    }
}
