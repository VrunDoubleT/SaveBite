using System.Text.Json;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopService : IShopService
{
    private readonly IShopRepository _shopRepository;
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IRabbitMqPublisher _publisher;

    public ShopService(
        IShopRepository shopRepository,
        IUserAddressRepository userAddressRepository,
        ICloudinaryService cloudinaryService,
        IRabbitMqPublisher publisher)
    {
        _shopRepository = shopRepository;
        _userAddressRepository = userAddressRepository;
        _cloudinaryService = cloudinaryService;
        _publisher = publisher;
    }

    public async Task<ShopSummaryResponse> GetOwnerShopAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _shopRepository.GetShopByOwnerUserIdAsync(ownerUserId, cancellationToken)
            ?? throw AppException.NotFound("Shop not found.");

        return new ShopSummaryResponse(shop.Id, shop.Name);
    }

    public async Task<PagedResult<NearbyShopResponse>> GetNearbyShopsAsync(
        NearbyShopsRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 6 : request.PageSize;

        double userLat;
        double userLon;

        if (request.Latitude.HasValue && request.Longitude.HasValue && (request.Latitude.Value != 0 || request.Longitude.Value != 0))
        {
            userLat = request.Latitude.Value;
            userLon = request.Longitude.Value;
        }
        else if (userId.HasValue)
        {
            var defaultAddress = await _userAddressRepository.GetDefaultByUserIdAsync(userId.Value, cancellationToken);
            if (defaultAddress != null)
            {
                userLat = defaultAddress.Latitude;
                userLon = defaultAddress.Longitude;
            }
            else
            {
                return PagedResult<NearbyShopResponse>.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0);
            }
        }
        else
        {
            return PagedResult<NearbyShopResponse>.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0);
        }

        var shops = await _shopRepository.GetActiveShopsAsync(request.Keyword, cancellationToken);
        if (!shops.Any())
            return PagedResult<NearbyShopResponse>.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0);

        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var radiusLimit = Math.Min(request.RadiusInKm <= 0 ? 10.0 : request.RadiusInKm, 10.0);
        var matchingShops = new List<(Shop Shop, double Distance, bool IsOpen)>();
        foreach (var shop in shops)
        {
            var distance = CalculateDistanceInKm(userLat, userLon, shop.Latitude, shop.Longitude);
            if (distance > radiusLimit) continue;

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
        var dealCounts = await _shopRepository.GetActiveDealCountsForShopsAsync(pagedShopIds, cancellationToken);

        var results = new List<NearbyShopResponse>();
        foreach (var item in pagedShops)
        {
            var shop = item.Shop;
            reviewsStats.TryGetValue(shop.Id, out var stat);
            dealCounts.TryGetValue(shop.Id, out var dealCount);

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
                TotalReviews = stat.TotalCount,
                FlashDealCount = dealCount
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
    // ===== iss-12: payment config =====

    public async Task<ShopPaymentConfigResponse> GetOwnerPaymentConfigAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var paymentConfig = await _shopRepository.GetPaymentConfigByOwnerUserIdAsync(ownerUserId, cancellationToken);
        if (paymentConfig == null)
            throw AppException.NotFound("Shop payment configuration not found.");

        return MapPaymentConfigResponse(paymentConfig);
    }

    public async Task<ShopPaymentConfigResponse> UpdateOwnerPaymentConfigAsync(Guid ownerUserId, UpdateShopPaymentConfigRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var paymentConfig = await _shopRepository.GetPaymentConfigByOwnerUserIdAsync(ownerUserId, cancellationToken);
        if (paymentConfig == null)
            throw AppException.NotFound("Shop payment configuration not found.");

        paymentConfig.BankName = string.IsNullOrWhiteSpace(request.BankName) ? null : request.BankName.Trim();
        paymentConfig.BankAccountNumber = string.IsNullOrWhiteSpace(request.BankAccountNumber) ? null : request.BankAccountNumber.Trim();
        paymentConfig.BankAccountHolder = string.IsNullOrWhiteSpace(request.BankAccountHolder) ? null : request.BankAccountHolder.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayosClientId))
            paymentConfig.PayosClientId = request.PayosClientId.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayosApiKey))
            paymentConfig.PayosApiKey = request.PayosApiKey.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayosChecksumKey))
            paymentConfig.PayosChecksumKey = request.PayosChecksumKey.Trim();

        paymentConfig.UpdatedAt = DateTime.UtcNow;
        await _shopRepository.SaveChangesAsync(cancellationToken);
        return MapPaymentConfigResponse(paymentConfig);
    }

    private static ShopPaymentConfigResponse MapPaymentConfigResponse(ShopPaymentConfig config)
    {
        return new ShopPaymentConfigResponse
        {
            BankName = config.BankName,
            BankAccountNumber = config.BankAccountNumber,
            BankAccountHolder = config.BankAccountHolder,
            PayosClientId = config.PayosClientId,
            HasPayosApiKey = !string.IsNullOrWhiteSpace(config.PayosApiKey),
            HasPayosChecksumKey = !string.IsNullOrWhiteSpace(config.PayosChecksumKey),
            UpdatedAt = config.UpdatedAt
        };
    }

    // ===== iss-13: store profile & operating management =====

    public Task<ShopProfileResponse> GetOwnerShopProfileAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return ToShopProfileResponseAsync(ownerUserId, cancellationToken);
    }

    public async Task<ShopProfileResponse> UpdateBusinessInfoAsync(Guid ownerUserId, UpdateBusinessInfoRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await _shopRepository.GetShopByOwnerUserIdAsync(ownerUserId, cancellationToken)
                   ?? throw AppException.NotFound("Shop not found.");

        shop.Name = request.Name.Trim();
        shop.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        shop.AddressLine = request.AddressLine.Trim();
        shop.Ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim();
        shop.District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        shop.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        shop.Latitude = request.Latitude;
        shop.Longitude = request.Longitude;
        shop.OpeningTime = request.OpeningTime;
        shop.ClosingTime = request.ClosingTime;
        shop.UpdatedAt = DateTime.UtcNow;

        await _shopRepository.UpdateShopAsync(shop, cancellationToken);

        return await ToShopProfileResponseAsync(ownerUserId, cancellationToken);
    }

    public async Task<ShopProfileResponse> UpdateShopImagesAsync(Guid ownerUserId, UpdateShopImagesRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await _shopRepository.GetShopByOwnerUserIdAsync(ownerUserId, cancellationToken)
                   ?? throw AppException.NotFound("Shop not found.");

        if (request.Logo is null && string.IsNullOrWhiteSpace(shop.LogoUrl))
            throw AppException.BadRequest("Shop logo is required.");

        if (request.Logo is not null)
        {
            var upload = await _cloudinaryService.UploadImageAsync(request.Logo, cancellationToken);
            shop.LogoUrl = upload.Url;
        }

        if (request.CoverImage is not null)
        {
            var upload = await _cloudinaryService.UploadImageAsync(request.CoverImage, cancellationToken);
            shop.CoverImageUrl = upload.Url;
        }

        shop.UpdatedAt = DateTime.UtcNow;
        await _shopRepository.UpdateShopAsync(shop, cancellationToken);
        return await ToShopProfileResponseAsync(ownerUserId, cancellationToken);
    }

    private async Task<ShopProfileResponse> ToShopProfileResponseAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var shop = await _shopRepository.GetShopProfileByOwnerUserIdAsync(ownerUserId, cancellationToken)
                    ?? throw AppException.NotFound("Shop not found.");

        var (avgRating, totalReviews, breakdown) = await _shopRepository.GetShopReviewStatsAsync(shop.Id, cancellationToken);
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
            Status = shop.Status.ToString(),
            BusinessLicenseNo = shop.Application?.BusinessLicenseNo,
            Documents = shop.Application?.Documents
                .Where(document => document.IsCurrent)
                .OrderBy(document => document.DocumentType)
                .Select(document => new ShopProfileDocumentResponse
                {
                    Id = document.Id,
                    DocumentType = document.DocumentType.ToString(),
                    FileUrl = document.FileUrl,
                    OriginalFileName = document.OriginalFileName,
                    ContentType = document.ContentType,
                    UploadedAt = document.UploadedAt
                }).ToList() ?? new List<ShopProfileDocumentResponse>(),
            AverageRating = Math.Round(avgRating, 1),
            TotalReviews = totalReviews,
            RatingBreakdown = breakdown
        };
    }
    
    // Manage Shops.
    public async Task<PagedResult<ShopListResponse>> GetShopListAsync(string? keyword, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _shopRepository.GetShopListAsync(keyword, page, pageSize, ct);

        var responses = items.Select(s => new ShopListResponse
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Status = s.Status.ToString(),
            City = s.City,
            OwnerUserId = s.OwnerUserId,
            OwnerName = s.Owner.FullName,
            CreatedAt = s.CreatedAt
        }).ToArray();

        return PagedResult<ShopListResponse>.Create(responses, page, pageSize, total);
    }

    // Manage Shops.
    public async Task<ShopContentResponse> GetShopContentAsync(Guid shopId, CancellationToken ct = default)
    {
        var shop = await _shopRepository.GetShopContentAsync(shopId, ct)
                   ?? throw AppException.NotFound("Shop not found.");

        var auditLogs = await _shopRepository.GetShopStatusHistoryAsync(shopId, ct);
        var statusHistory = auditLogs.Select(log => new ShopStatusHistoryResponse
        {
            Id = log.Id,
            FromStatus = ReadStatus(log.OldValuesJson),
            ToStatus = ReadStatus(log.NewValuesJson) ?? string.Empty,
            Reason = log.Reason,
            CreatedAt = log.CreatedAt
        }).ToArray();

        var currentStatusReason = shop.Status.ToString() is "Paused" or "Suspended"
            ? statusHistory.FirstOrDefault(item => item.ToStatus == shop.Status.ToString())?.Reason
            : null;

        return new ShopContentResponse
        {
            Id = shop.Id,
            OwnerUserId = shop.OwnerUserId,
            Owner = shop.Owner == null ? null : new ShopOwnerResponse
            {
                Id = shop.Owner.Id,
                FullName = shop.Owner.FullName,
                Email = shop.Owner.Email,
                PhoneNumber = shop.Owner.Phone
            },
            Name = shop.Name,
            Description = shop.Description,
            LogoUrl = shop.LogoUrl,
            CoverImageUrl = shop.CoverImageUrl,
            AddressLine = shop.AddressLine,
            Ward = shop.Ward,
            District = shop.District,
            City = shop.City,
            Latitude = shop.Latitude,
            Longitude = shop.Longitude,
            OpeningTime = shop.OpeningTime,
            ClosingTime = shop.ClosingTime,
            BusinessLicenseNo = shop.Application?.BusinessLicenseNo,
            Documents = shop.Application?.Documents
                .Where(document => document.IsCurrent)
                .OrderBy(document => document.DocumentType)
                .Select(document => new ShopProfileDocumentResponse
                {
                    Id = document.Id,
                    DocumentType = document.DocumentType.ToString(),
                    FileUrl = document.FileUrl,
                    OriginalFileName = document.OriginalFileName,
                    ContentType = document.ContentType,
                    UploadedAt = document.UploadedAt
                }).ToList() ?? new List<ShopProfileDocumentResponse>(),
            Status = shop.Status.ToString(),
            CurrentStatusReason = currentStatusReason,
            StatusHistory = statusHistory,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.UpdatedAt
        };
    }
    
    // Manage Shops.
    public async Task<ModerationActionResponse> UpdateShopStatusAsync(Guid adminId, Guid shopId, UpdateShopModerationStatusRequest request, CancellationToken ct)
    {
        var targetStatus = Enum.Parse<ShopStatus>(request.Status.Trim(), ignoreCase: true);
        
        var shop = await _shopRepository.GetShopForStatusUpdateAsync(shopId, ct);

        if (shop is null)
            throw AppException.NotFound("Shop not found.");

        var currentStatus = shop.Status;
                  
        if (currentStatus == targetStatus)
            throw AppException.Conflict("Shop already has this status.");
        
        var isAllowed = currentStatus switch
        {
            ShopStatus.Active => targetStatus is ShopStatus.Paused or ShopStatus.Suspended,
            ShopStatus.Paused => targetStatus is ShopStatus.Active or ShopStatus.Suspended,
            ShopStatus.Suspended => targetStatus == ShopStatus.Active,
            _ => false
        };

        if (!isAllowed)
            throw AppException.BadRequest($"Cannot change shop status from {currentStatus} to {targetStatus}.");

        var requiresReason = targetStatus is ShopStatus.Paused or ShopStatus.Suspended;
        var reason = request.Reason?.Trim();

        if (!requiresReason) reason = null;
        
        var oldValues = new
        {
            Status = currentStatus.ToString()
        };

        shop.Status = targetStatus;
        shop.UpdatedAt = DateTime.UtcNow;
        await _shopRepository.SaveChangesAsync(ct);

        var moderationEvent = new ModerationEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = adminId,
            Action = "SHOP_STATUS_UPDATED",
            TargetType = "Shop",
            TargetId = shopId,
            Reason = reason,
            OldValuesJson = JsonSerializer.Serialize(oldValues),
            NewValuesJson = JsonSerializer.Serialize(new { Status = targetStatus.ToString() }),
            CreatedAt = DateTime.UtcNow
        };
        await _publisher.PublishAsync(RabbitMqRoutingKeys.ModerationAudit, moderationEvent, ct);

        return new ModerationActionResponse
        {
            TargetId = shopId,
            Action = "SHOP_STATUS_UPDATED",
            Message = $"Shop status changed from {currentStatus} to {targetStatus}."
        };
    }
    
    // Manage Shops.
    private static string? ReadStatus(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name.Equals("Status", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }
        return null;
    }
}