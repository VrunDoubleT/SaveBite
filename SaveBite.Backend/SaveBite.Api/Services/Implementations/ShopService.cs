using SaveBite.Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.DTOs;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Hubs;
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
    private readonly AppDbContext _db;
    private readonly INotificationQueue _notifications;
    private readonly IShopRepository _shopRepository;
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IAdminAuditWriter _audit;
    private readonly IShopSuspensionCleanupService _suspensionCleanup;
    private readonly IShopStatusEventService _shopStatusEvents;
    private readonly IRealtimePublisher _realtime;
    private readonly IPayOsCredentialsValidator _payOsValidator;
    private readonly IPayOsCredentialProtector _credentialProtector;

    public ShopService(
        IShopRepository shopRepository,
        IUserAddressRepository userAddressRepository,
        ICloudinaryService cloudinaryService,
        IAdminAuditWriter audit,
        INotificationQueue notifications, AppDbContext db,
        IShopSuspensionCleanupService suspensionCleanup,
        IShopStatusEventService shopStatusEvents,
        IRealtimePublisher realtime, IPayOsCredentialsValidator payOsValidator,
        IPayOsCredentialProtector credentialProtector)
    {
        _realtime = realtime;
        _payOsValidator = payOsValidator;
        _credentialProtector = credentialProtector;
        _db = db;
        _notifications = notifications;
        _shopRepository = shopRepository;
        _userAddressRepository = userAddressRepository;
        _cloudinaryService = cloudinaryService;
        _audit = audit;
        _suspensionCleanup = suspensionCleanup;
        _shopStatusEvents = shopStatusEvents;
    }

    public async Task<ShopSummaryResponse> GetOwnerShopAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var shop = await _shopRepository.GetShopByOwnerUserIdAsync(ownerUserId, cancellationToken)
            ?? throw AppException.NotFound("Shop not found.");

        return new ShopSummaryResponse(shop.Id, shop.Name);
    }

    public async Task<NearbyShopsPagedResult> GetNearbyShopsAsync(
        NearbyShopsRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Clamp(request.Page, 1, 100_000);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 6 : request.PageSize, 1, 100);

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
                return NearbyShopsPagedResult.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0, 0, 0);
            }
        }
        else
        {
            return NearbyShopsPagedResult.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0, 0, 0);
        }

        var shops = await _shopRepository.GetActiveShopsAsync(request.Keyword, request.Category, request.ShopName, cancellationToken, request.Price);
        if (!shops.Any())
            return NearbyShopsPagedResult.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0, 0, 0);

        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        double maxDistance = Math.Clamp(request.RadiusInKm, 5.0, 20.0);
        if (!string.IsNullOrWhiteSpace(request.Distance))
        {
            var distanceRules = request.Distance.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (distanceRules.Length > 0)
            {
                if (distanceRules.Any(r => r.Contains("2km", StringComparison.OrdinalIgnoreCase))) maxDistance = Math.Max(maxDistance, 2.0);
                if (distanceRules.Any(r => r.Contains("5km", StringComparison.OrdinalIgnoreCase))) maxDistance = Math.Max(maxDistance, 5.0);
                if (distanceRules.Any(r => r.Contains("10km", StringComparison.OrdinalIgnoreCase))) maxDistance = Math.Max(maxDistance, 10.0);
                if (distanceRules.Any(r => r.Contains("20km", StringComparison.OrdinalIgnoreCase))) maxDistance = Math.Max(maxDistance, 20.0);
                if (distanceRules.Any(r => r.Contains("15km", StringComparison.OrdinalIgnoreCase))) maxDistance = Math.Max(maxDistance, 15.0);
            }
        }

        var matchingShops = new List<(Shop Shop, double Distance, bool IsOpen)>();
        foreach (var shop in shops)
        {
            var distance = CalculateDistanceInKm(userLat, userLon, shop.Latitude, shop.Longitude);
            if (distance > maxDistance) continue;

            var isOpen = CheckIsOpen(shop.OpeningTime, shop.ClosingTime, nowTime);
            if (request.OnlyOpen == true && !isOpen) continue;

            matchingShops.Add((shop, distance, isOpen));
        }

        if (!matchingShops.Any())
            return NearbyShopsPagedResult.Create(Array.Empty<NearbyShopResponse>(), page, pageSize, 0, 0, 0);

        var allMatchingShopIds = matchingShops.Select(x => x.Shop.Id).ToList();
        var allDealCounts = await _shopRepository.GetActiveDealCountsForShopsAsync(allMatchingShopIds, cancellationToken);

        var allStoresCount = matchingShops.Count;
        var flashDealsStoresCount = matchingShops.Count(x => allDealCounts.TryGetValue(x.Shop.Id, out var count) && count > 0);

        if (request.IsFlashDealsOnly)
        {
            matchingShops = matchingShops.Where(x => allDealCounts.TryGetValue(x.Shop.Id, out var count) && count > 0).ToList();
        }

        var totalItems = matchingShops.Count;
        var pagedShops = matchingShops
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Shop.Id)
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
            allDealCounts.TryGetValue(shop.Id, out var dealCount);

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

        return NearbyShopsPagedResult.Create(results, page, pageSize, totalItems, allStoresCount, flashDealsStoresCount);
    }

    public async Task<ShopProfileResponse> GetShopProfileAsync(
        Guid shopId,
        CancellationToken cancellationToken = default, Guid? viewerId = null)
    {
        var shop = await _shopRepository.GetActiveShopByIdAsync(shopId, cancellationToken);
        if (shop == null)
            throw AppException.NotFound("Shop not found or is currently inactive.");

        var (avgRating, totalReviews, breakdown) = await _shopRepository.GetShopReviewStatsAsync(shopId, cancellationToken);
        var nowTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        return new ShopProfileResponse
        {
            OwnerUserId = shop.OwnerUserId,
            FollowerCount = await _db.Set<ShopFollow>().CountAsync(f => f.ShopId == shop.Id && f.CustomerId != shop.OwnerUserId, cancellationToken),
            CurrentUserFollow = viewerId.HasValue && viewerId != shop.OwnerUserId
                ? await _db.Set<ShopFollow>().AsNoTracking().Where(f => f.ShopId == shop.Id && f.CustomerId == viewerId.Value)
                    .Select(f => new ShopFollowResponse(shop.Id, shop.Name, shop.LogoUrl, shop.AddressLine,
                        shop.Status, f.NotificationStatus, f.FollowedAt)).SingleOrDefaultAsync(cancellationToken) : null,
            SuspendedForPlatformFees = shop.SuspendedForPlatformFees,
            SuspendedByAdmin = shop.SuspendedByAdmin,
            SuspendedForOverdueRefunds = shop.SuspendedForOverdueRefunds,
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
            Images = f.Images?.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).ToList() ?? new List<string>(),
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
        if (opening.Value == closing.Value) return true; // 00:00 - 00:00 or same opening/closing means open 24/7 (full day)
        return opening.Value <= closing.Value
            ? now >= opening.Value && now <= closing.Value
            : now >= opening.Value || now <= closing.Value;
    }

    //  payment config =====
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
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var paymentConfig = await _shopRepository.GetPaymentConfigByOwnerUserIdAsync(ownerUserId, cancellationToken);
        if (paymentConfig == null)
            throw AppException.NotFound("Shop payment configuration not found.");

        // Match checkout's user -> order -> shop locking order when an owner is also a customer.
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {ownerUserId} FOR UPDATE", cancellationToken);
        var lockName = $"payos-shop:{paymentConfig.ShopId}";
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockName}, 0))", cancellationToken);
        await _db.Entry(paymentConfig).ReloadAsync(cancellationToken);
        var clientChanged = !string.IsNullOrWhiteSpace(request.PayosClientId) && paymentConfig.PayosClientId != request.PayosClientId.Trim();
        var apiKeyChanged = !string.IsNullOrWhiteSpace(request.PayosApiKey) && paymentConfig.PayosApiKey != request.PayosApiKey.Trim();
        var checksumChanged = !string.IsNullOrWhiteSpace(request.PayosChecksumKey) && paymentConfig.PayosChecksumKey != request.PayosChecksumKey.Trim();
        if (!clientChanged && !apiKeyChanged && !checksumChanged)
        {
            await tx.CommitAsync(cancellationToken);
            return MapPaymentConfigResponse(paymentConfig);
        }
        await _payOsValidator.ValidateAsync(new(
            string.IsNullOrWhiteSpace(request.PayosClientId) ? paymentConfig.PayosClientId : request.PayosClientId.Trim(),
            string.IsNullOrWhiteSpace(request.PayosApiKey) ? paymentConfig.PayosApiKey : request.PayosApiKey.Trim(),
            string.IsNullOrWhiteSpace(request.PayosChecksumKey) ? paymentConfig.PayosChecksumKey : request.PayosChecksumKey.Trim()), cancellationToken);
        // Preserve existing links before replacing the shop's active credential set.
        var legacy = await _db.Payments.Where(p => p.Order.ShopId == paymentConfig.ShopId
            && p.GatewayCreationAttempted && p.ProtectedPayOsCredentials == null)
            .ToListAsync(cancellationToken);
        foreach (var payment in legacy) payment.ProtectedPayOsCredentials = _credentialProtector.Protect(new(
            paymentConfig.PayosClientId, paymentConfig.PayosApiKey, paymentConfig.PayosChecksumKey));

        if (!string.IsNullOrWhiteSpace(request.PayosClientId))
            paymentConfig.PayosClientId = request.PayosClientId.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayosApiKey))
            paymentConfig.PayosApiKey = request.PayosApiKey.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayosChecksumKey))
            paymentConfig.PayosChecksumKey = request.PayosChecksumKey.Trim();

        _audit.Record(new AuditLog
        {
            ActorUserId = ownerUserId,
            TargetType = "ShopPaymentConfig",
            TargetId = paymentConfig.ShopId,
            Action = "SHOP_PAYMENT_CONFIG_UPDATED",
            NewValuesJson = JsonSerializer.Serialize(new
            {
                MerchantSettingChanged = clientChanged,
                AccessKeyChanged = apiKeyChanged,
                SigningKeyChanged = checksumChanged
            })
        });
        paymentConfig.UpdatedAt = DateTime.UtcNow;
        await _shopRepository.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return MapPaymentConfigResponse(paymentConfig);
    }

    private static ShopPaymentConfigResponse MapPaymentConfigResponse(ShopPaymentConfig config)
    {
        return new ShopPaymentConfigResponse
        {
            PayosClientId = config.PayosClientId,
            HasPayosApiKey = !string.IsNullOrWhiteSpace(config.PayosApiKey),
            HasPayosChecksumKey = !string.IsNullOrWhiteSpace(config.PayosChecksumKey),
            UpdatedAt = config.UpdatedAt
        };
    }

    //  store profile & operating management =====
    public Task<ShopProfileResponse> GetOwnerShopProfileAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return ToShopProfileResponseAsync(ownerUserId, cancellationToken);
    }

    public async Task<ShopProfileResponse> UpdateBusinessInfoAsync(Guid ownerUserId, UpdateBusinessInfoRequest request, CancellationToken cancellationToken = default)
    {
        var shop = await _shopRepository.GetShopByOwnerUserIdAsync(ownerUserId, cancellationToken)
                   ?? throw AppException.NotFound("Shop not found.");

        if (shop.OpeningTime != request.OpeningTime || shop.ClosingTime != request.ClosingTime)
        {
            await ValidateOperatingHoursChangeAsync(shop.Id, request.OpeningTime, request.ClosingTime, cancellationToken);
        }

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
            OwnerUserId = shop.OwnerUserId,
            FollowerCount = await _db.Set<ShopFollow>().CountAsync(f => f.ShopId == shop.Id && f.CustomerId != shop.OwnerUserId, cancellationToken),
            SuspendedForPlatformFees = shop.SuspendedForPlatformFees,
            SuspendedByAdmin = shop.SuspendedByAdmin,
            SuspendedForOverdueRefunds = shop.SuspendedForOverdueRefunds,
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
            LogoUrl = s.LogoUrl,
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

        var histories = await _shopRepository.GetShopStatusHistoryAsync(shopId, ct);
        var statusHistory = histories.Select(log => new ShopStatusHistoryResponse
        {
            Id = log.Id,
            FromStatus = log.FromStatus?.ToString(),
            ToStatus = log.ToStatus.ToString(),
            Reason = log.Reason,
            ChangedByType = log.ChangedByType,
            ChangedByUserId = log.ChangedByUserId,
            SuspendedByAdmin = log.SuspendedByAdmin,
            SuspendedForPlatformFees = log.SuspendedForPlatformFees,
            SuspendedForOverdueRefunds = log.SuspendedForOverdueRefunds,
            CreatedAt = log.CreatedAt
        }).ToArray();

        var currentStatusReason = shop.Status.ToString() is "Paused" or "Suspended"
            ? statusHistory.FirstOrDefault(item => item.ToStatus == shop.Status.ToString())?.Reason
            : null;
        if (shop.Status == ShopStatus.Suspended)
            currentStatusReason = string.Join(" ", new[] { currentStatusReason, ShopSuspensionRules.Reason(shop) }.Where(value => !string.IsNullOrWhiteSpace(value)));

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
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        var targetStatus = Enum.Parse<ShopStatus>(request.Status.Trim(), ignoreCase: true);

        var shop = await _shopRepository.GetShopForStatusUpdateAsync(shopId, ct);

        if (shop is null)
            throw AppException.NotFound("Shop not found.");

        var currentStatus = shop.Status;

        var statusChanged = currentStatus != targetStatus;
        if (!statusChanged && targetStatus != ShopStatus.Suspended)
            throw AppException.Conflict("Shop already has this status.");

        var isAllowed = !statusChanged || currentStatus switch
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

        if (targetStatus == ShopStatus.Active && (shop.SuspendedForPlatformFees || shop.SuspendedForOverdueRefunds))
            throw AppException.Conflict("Settle all overdue platform fees and mandatory refunds before reactivating the shop.");
        if (statusChanged || targetStatus == ShopStatus.Suspended)
        {
            _db.RecordAdminShopStatusChange(shopId, adminId, reason);
            shop.SuspendedByAdmin = targetStatus == ShopStatus.Suspended;
            shop.Status = targetStatus;
            shop.UpdatedAt = DateTime.UtcNow;
            var auditEntry = _audit.Record(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = adminId,
                Action = AuditActionTypes.ShopStatusUpdated,
                TargetType = "Shop",
                TargetId = shopId,
                Reason = reason,
                OldValuesJson = JsonSerializer.Serialize(oldValues),
                NewValuesJson = JsonSerializer.Serialize(new { Status = targetStatus.ToString() }),
                CreatedAt = DateTime.UtcNow
            });
            await _shopRepository.SaveChangesAsync(ct);

            await _notifications.EnqueueAsync(NotificationTarget.User(shop.OwnerUserId), new CreateNotificationCommand(
                NotificationType.ShopStatusChanged, NotificationMessages.Shop.ShopStatusUpdatedTitle,
                NotificationMessages.Shop.ShopStatusUpdatedBody(shop.Name, currentStatus, targetStatus, reason),
                "/store-owner/profile", $"shop-status:{auditEntry.Id}"), ct);
        }
        await transaction.CommitAsync(ct);

        if (statusChanged || targetStatus == ShopStatus.Suspended)
            await _shopStatusEvents.NotifyChangedAsync(shopId, targetStatus.ToString(), ct);
        if (targetStatus == ShopStatus.Suspended)
            await _suspensionCleanup.CleanupAsync(shopId, ct);
        // Realtime: tell the shop owner's open tabs to reload the shop (status / reason) without a page refresh.
        // Sent only after the commit so the client always reads the new status; failures are swallowed by the publisher.
        await _realtime.SendToUserAsync(shop.OwnerUserId, "shop.status.changed", new
        {
            shopId,
            previousStatus = currentStatus.ToString(),
            status = targetStatus.ToString(),
            reason
        });

        return new ModerationActionResponse
        {
            TargetId = shopId,
            Action = AuditActionTypes.ShopStatusUpdated,
            Message = statusChanged
                ? $"Shop status changed from {currentStatus} to {targetStatus}."
                : "Shop suspension cleanup was retried."
        };
    }

    private async Task ValidateOperatingHoursChangeAsync(
        Guid shopId,
        TimeOnly? newOpening,
        TimeOnly? newClosing,
        CancellationToken cancellationToken)
    {
        if (newOpening.HasValue && newClosing.HasValue && newOpening.Value == newClosing.Value)
        {
            // Equal times mean open 24 hours, so existing deals and pickups remain within hours.
            return;
        }

        if (!newClosing.HasValue && !newOpening.HasValue)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;

        // 1. Check active (OnSale) or scheduled (Pending) Flash Deals
        var activeDeals = await _db.FlashDeals
            .AsNoTracking()
            .Include(d => d.Product)
            .Where(d => d.ShopId == shopId
                && (d.Status == FlashDealStatus.Pending || d.Status == FlashDealStatus.OnSale)
                && d.OrderEndTime > nowUtc)
            .OrderBy(d => d.OrderEndTime)
            .ToListAsync(cancellationToken);

        if (activeDeals.Count > 0)
        {
            var conflictingDeals = activeDeals.Where(deal =>
            {
                var dealStartLocal = TimeOnly.FromDateTime(deal.SaleStartTime.AddHours(7));
                var dealEndLocal = TimeOnly.FromDateTime(deal.OrderEndTime.AddHours(7));
                var startMinute = new TimeOnly(dealStartLocal.Hour, dealStartLocal.Minute);
                var endMinute = new TimeOnly(dealEndLocal.Hour, dealEndLocal.Minute);
                return !IsTimeWithinWindow(newOpening, newClosing, startMinute) ||
                       !IsTimeWithinWindow(newOpening, newClosing, endMinute);
            }).ToList();

            if (conflictingDeals.Count > 0)
            {
                var firstDeal = conflictingDeals[0];
                var firstDealEndLocal = TimeOnly.FromDateTime(firstDeal.OrderEndTime.AddHours(7));
                var statusText = firstDeal.Status == FlashDealStatus.OnSale ? "active" : "scheduled";
                var productName = firstDeal.Product?.Name ?? "product";

                var hoursText = newOpening.HasValue && newClosing.HasValue
                    ? $"{newOpening.Value:HH\\:mm} - {newClosing.Value:HH\\:mm}"
                    : (newClosing.HasValue ? $"closing at {newClosing.Value:HH\\:mm}" : $"opening at {newOpening!.Value:HH\\:mm}");

                throw AppException.BadRequest(
                    $"Cannot change operating hours to ({hoursText}). " +
                    $"The shop currently has {conflictingDeals.Count} Flash Deal(s) ({statusText}: '{productName}') " +
                    $"ending at {firstDealEndLocal:HH\\:mm}, which exceeds the new operating hours. " +
                    $"Please end or cancel these Flash Deals before changing operating hours.");
            }
        }

        // 2. Check pending or pickup orders with pickup deadline (HardDeadlineAt)
        var activeOrderStatuses = new[]
        {
            OrderStatus.AwaitingPayment,
            OrderStatus.Pending,
            OrderStatus.Confirmed,
            OrderStatus.Preparing,
            OrderStatus.ReadyForPickup
        };

        var activeOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.ShopId == shopId
                && activeOrderStatuses.Contains(o.Status)
                && o.HardDeadlineAt.HasValue
                && o.HardDeadlineAt.Value > nowUtc)
            .OrderBy(o => o.HardDeadlineAt)
            .ToListAsync(cancellationToken);

        if (activeOrders.Count > 0)
        {
            var conflictingOrders = activeOrders.Where(o =>
            {
                var pickupLocal = TimeOnly.FromDateTime(o.HardDeadlineAt!.Value.AddHours(7));
                var pickupMinute = new TimeOnly(pickupLocal.Hour, pickupLocal.Minute);
                return !IsTimeWithinWindow(newOpening, newClosing, pickupMinute);
            }).ToList();

            if (conflictingOrders.Count > 0)
            {
                var firstOrder = conflictingOrders[0];
                var pickupDeadlineLocal = TimeOnly.FromDateTime(firstOrder.HardDeadlineAt!.Value.AddHours(7));

                var hoursText = newClosing.HasValue ? $"closing at {newClosing.Value:HH\\:mm}" : "new";

                throw AppException.BadRequest(
                    $"Cannot change operating hours to {hoursText}. " +
                    $"The shop currently has {conflictingOrders.Count} order(s) awaiting pickup " +
                    $"(Order #{firstOrder.OrderCode} has pickup deadline at {pickupDeadlineLocal:HH\\:mm}), " +
                    $"which falls outside the new operating hours. Please fulfill or resolve these orders before changing hours.");
            }
        }
    }

    private static bool IsTimeWithinWindow(TimeOnly? opening, TimeOnly? closing, TimeOnly time)
    {
        if (opening.HasValue && closing.HasValue)
        {
            if (opening.Value == closing.Value) return true;

            return opening.Value <= closing.Value
                ? time >= opening.Value && time <= closing.Value
                : time >= opening.Value || time <= closing.Value;
        }

        if (closing.HasValue)
        {
            return time <= closing.Value;
        }

        if (opening.HasValue)
        {
            return time >= opening.Value;
        }

        return true;
    }
}
