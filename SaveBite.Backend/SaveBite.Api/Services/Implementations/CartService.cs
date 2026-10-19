using System.Buffers.Binary;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.DTOs;
using Microsoft.AspNetCore.WebUtilities;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Manage customer cart quantities, availability checks, shop grouping, and cursor pages.
public sealed class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IRealtimePublisher? _realtimePublisher;

    public CartService(ICartRepository cartRepository, IRealtimePublisher? realtimePublisher = null)
    {
        _cartRepository = cartRepository;
        _realtimePublisher = realtimePublisher;
    }

    // Load a cursor page of complete shop groups and include counts for the whole cart.
    public async Task<CartPageResponse> GetPageAsync(Guid userId, string? cursor, int pageSize, CancellationToken ct = default)
    {
        if (pageSize is < 1 or > 20) throw AppException.BadRequest("Page size must be between 1 and 20 shops.");
        // Decode the last addition timestamp and shop ID for newest-first pagination.
        Guid? after = null;
        DateTime? afterAddedAt = null;
        if (cursor != null)
        {
            try
            {
                if (cursor.Length > 64) throw new FormatException();
                var bytes = WebEncoders.Base64UrlDecode(cursor);
                if (bytes.Length != 24) throw new FormatException();
                afterAddedAt = new DateTime(BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(0, 8)), DateTimeKind.Utc);
                after = new Guid(bytes.AsSpan(8, 16));
                if (after == Guid.Empty) throw new FormatException();
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException) { throw AppException.BadRequest("Invalid cart cursor."); }
        }
        // Keep every shop's items together so pagination does not split one checkout group.
        var result = await _cartRepository.GetPageAsync(userId, after, afterAddedAt, pageSize, ct);
        var shops = ToResponse(result.Items).Shops;
        string? next = null;
        if (result.HasMore && result.LastShopId.HasValue && result.LastAddedAt.HasValue)
        {
            var bytes = new byte[24];
            BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(0, 8), result.LastAddedAt.Value.Ticks);
            result.LastShopId.Value.TryWriteBytes(bytes.AsSpan(8, 16));
            next = WebEncoders.Base64UrlEncode(bytes);
        }
        // Use whole-cart counts rather than counting only the shops loaded on this page.
        var summary = await _cartRepository.GetSummaryAsync(userId, ct);
        return new CartPageResponse
        {
            Page = CursorPagedResult<CartShopResponse>.Create(shops, next, result.HasMore, pageSize),
            TotalQuantity = summary.TotalQuantity,
            TotalFlashDeals = summary.TotalFlashDeals
        };
    }

    // Return total quantity and distinct Flash Deal count without loading all cart item details.
    public Task<CartSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct = default) => _cartRepository.GetSummaryAsync(userId, ct);

    // Add a variant or increase its existing cart quantity without reserving inventory.
    public Task<CartResponse> AddItemAsync(Guid userId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FlashDealVariantId == Guid.Empty || request.Quantity <= 0)
            throw AppException.BadRequest("Select a variant and a positive quantity.");

        // Serialize this customer's cart changes so concurrent additions cannot overwrite quantities.
        return ExecuteMutationAsync(userId, async () =>
        {
            var variant = await _cartRepository.GetVariantAsync(request.FlashDealVariantId, cancellationToken)
                ?? throw AppException.NotFound("Flash deal variant not found.");
            await EnsureNotShopOperatorAsync(userId, variant.FlashDeal.ShopId, cancellationToken);
            var item = await _cartRepository.FindVariantItemAsync(userId, request.FlashDealVariantId, cancellationToken);
            // Validate the combined quantity using a wider integer before storing it as an int.
            var quantity = (long)(item?.Quantity ?? 0) + request.Quantity;
            EnsureAvailable(variant, quantity);
            if (item == null)
                _cartRepository.Add(new CartItem
                {
                    Id = Guid.NewGuid(), UserId = userId, FlashDealVariantId = variant.Id,
                    Quantity = (int)quantity, AddedAt = DateTime.UtcNow
                });
            else
            {
                item.Quantity = (int)quantity;
                // Adding an existing variant also moves it and its shop to the top.
                item.AddedAt = DateTime.UtcNow;
            }
            await _cartRepository.SaveChangesAsync(cancellationToken);
            // Reload current prices, availability, and totals instead of returning a client-side estimate.
            return ToResponse(await _cartRepository.GetItemsAsync(userId, cancellationToken: cancellationToken));
        }, cancellationToken);
    }

    // Replace the quantity of an item owned by this customer after checking current availability.
    public Task<CartMutationResponse> UpdateQuantityAsync(Guid userId, Guid itemId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw AppException.BadRequest("Quantity must be positive. Remove the item to delete it.");

        return ExecuteMutationAsync(userId, async () =>
        {
            // Scope the lookup to this customer so another customer's cart item cannot be changed.
            var item = await _cartRepository.FindItemAsync(userId, itemId, cancellationToken)
                ?? throw AppException.NotFound("Cart item not found.");
            var variant = await _cartRepository.GetVariantAsync(item.FlashDealVariantId, cancellationToken)
                ?? throw AppException.NotFound("Flash deal variant not found.");
            EnsureAvailable(variant, request.Quantity);
            await EnsureNotShopOperatorAsync(userId, variant.FlashDeal.ShopId, cancellationToken);
            item.Quantity = request.Quantity;
            await _cartRepository.SaveChangesAsync(cancellationToken);
            // Return only the affected shop and whole-cart badge counts for an immediate UI patch.
            return await MutationResponseAsync(userId, variant.FlashDeal.ShopId, cancellationToken);
        }, cancellationToken);
    }

    // Remove the customer's selected cart item and return its shop snapshot inside the user lock.
    public Task<CartMutationResponse> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
        => ExecuteMutationAsync(userId, async () =>
        {
            // Scope the lookup to this customer so another customer's cart item cannot be changed.
            var item = await _cartRepository.FindItemAsync(userId, itemId, cancellationToken)
                ?? throw AppException.NotFound("Cart item not found.");
            var variant = await _cartRepository.GetVariantAsync(item.FlashDealVariantId, cancellationToken)
                ?? throw AppException.NotFound("Flash deal variant not found.");
            _cartRepository.Remove(item);
            await _cartRepository.SaveChangesAsync(cancellationToken);
            // Return only the affected shop and whole-cart badge counts for an immediate UI patch.
            return await MutationResponseAsync(userId, variant.FlashDeal.ShopId, cancellationToken);
        }, cancellationToken);

    // Repository returns only after committing and disposing its transaction; orchestration stays in the service.
    private async Task<T> ExecuteMutationAsync<T>(Guid userId, Func<Task<T>> action, CancellationToken ct)
    {
        var response = await _cartRepository.WithUserLockAsync(userId, action, ct);
        if (_realtimePublisher != null)
            await _realtimePublisher.SendToUserAsync(userId, RealtimeEvents.CartChanged, new CartChangedEvent(RealtimeEvents.CartReasons.Updated));
        return response;
    }

    // Return the changed shop snapshot and whole-cart badge counts for an immediate UI update.
    private async Task<CartMutationResponse> MutationResponseAsync(Guid userId, Guid shopId, CancellationToken ct)
    {
        var shop = ToResponse(await _cartRepository.GetItemsAsync(userId, shopId, ct)).Shops.SingleOrDefault();
        return new CartMutationResponse
        {
            ShopId = shopId,
            Shop = shop,
            Summary = await _cartRepository.GetSummaryAsync(userId, ct)
        };
    }

    // Existing cart items must not let newly assigned shop operators bypass the purchase restriction.
    private async Task EnsureNotShopOperatorAsync(Guid userId, Guid shopId, CancellationToken ct)
    {
        if (await _cartRepository.IsShopOperatorAsync(userId, shopId, ct))
            throw AppException.Forbidden("You cannot add or purchase items from a shop you own or actively work for.");
    }

    // Calculate remaining stock after reservations and sales, clamped to the response integer range.
    private static int AvailableQuantity(FlashDealVariant variant)
        => (int)Math.Clamp((long)variant.TotalQuantity - variant.ReservedQuantity - variant.SoldQuantity, 0, int.MaxValue);

    // Return the first shop, product, deal, sale-window, or stock condition preventing purchase.
    private static string? UnavailableReason(FlashDealVariant variant, DateTime now)
    {
        var deal = variant.FlashDeal;
        if (deal.Shop.Status != ShopStatus.Active) return "This shop is not accepting orders.";
        if (deal.Product.Status != ProductStatus.Active || !variant.Variant.IsActive)
            return "This product variant is no longer available.";
        if (deal.Status != FlashDealStatus.OnSale || variant.Status != FlashDealVariantStatus.Active)
            return "This deal is not available for purchase.";
        if (now < deal.SaleStartTime) return "This deal has not started yet.";
        if (now >= deal.OrderEndTime || now >= deal.ShopClosingTime) return "The order window has closed.";
        if (AvailableQuantity(variant) == 0) return "This item is sold out.";
        return null;
    }

    // Reject unavailable variants or quantities exceeding current stock; this check does not reserve stock.
    private static void EnsureAvailable(FlashDealVariant variant, long quantity)
    {
        var reason = UnavailableReason(variant, DateTime.UtcNow);
        if (reason != null) throw AppException.Conflict(reason);
        if (quantity > AvailableQuantity(variant))
            throw AppException.OutOfStock($"Only {AvailableQuantity(variant)} item(s) are currently available.");
    }

    // Build server-priced item details, availability flags, subtotals, and totals grouped by shop.
    internal static CartResponse ToResponse(IReadOnlyList<CartItem> items)
    {
        var now = DateTime.UtcNow;
        // Each shop becomes a separate checkout group because one order may contain only one shop.
        var shops = items.GroupBy(x => x.FlashDealVariant.FlashDeal.ShopId)
            .OrderByDescending(group => group.Max(x => x.AddedAt)).ThenBy(group => group.Key).Select(group =>
        {
            var shop = group.First().FlashDealVariant.FlashDeal.Shop;
            var responses = group.OrderByDescending(x => x.AddedAt).ThenBy(x => x.Id).Select(item =>
            {
                var variant = item.FlashDealVariant;
                var product = variant.FlashDeal.Product;
                // Build the variant label from its attributes and prefer a variant image over a product image.
                var values = variant.Variant.VariantValues.OrderBy(x => x.AttributeValue.Attribute.Name).ToList();
                var name = values.Count > 0 ? string.Join(" / ", values.Select(x => x.AttributeValue.Value)) : variant.Variant.Sku ?? "Default";
                var image = values.Select(x => x.AttributeValue.ImageUrl).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                    ?? product.Images.OrderBy(x => x.Id).Select(x => x.ImageUrl).FirstOrDefault();
                var available = AvailableQuantity(variant);
                var reason = UnavailableReason(variant, now);
                // Allow reducing an excessive quantity while the variant itself remains purchasable.
                var canUpdateQuantity = reason == null;
                if (reason == null && item.Quantity > available) reason = $"Only {available} item(s) are currently available. Reduce the quantity.";
                // Show the earlier deal/shop cutoff and calculate the subtotal from the database deal price.
                return new CartItemResponse
                {
                    Id = item.Id,
                    FlashDealId = variant.FlashDealId,
                    FlashDealVariantId = variant.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    VariantName = name,
                    ImageUrl = image,
                    OriginalPrice = variant.OriginalPrice,
                    UnitPrice = variant.DealPrice,
                    Quantity = item.Quantity,
                    AvailableQuantity = available,
                    Subtotal = variant.DealPrice * item.Quantity,
                    OrderEndTime = variant.FlashDeal.OrderEndTime < variant.FlashDeal.ShopClosingTime
                        ? variant.FlashDeal.OrderEndTime : variant.FlashDeal.ShopClosingTime,
                    CanUpdateQuantity = canUpdateQuantity,
                    IsAvailable = reason == null,
                    UnavailableReason = reason
                };
            }).ToList();
            var address = string.Join(", ", new[] { shop.AddressLine, shop.Ward, shop.District, shop.City }.Where(x => !string.IsNullOrWhiteSpace(x)));
            // Enable shop checkout only when every item in that shop is available at its requested quantity.
            return new CartShopResponse
            {
                ShopId = shop.Id,
                ShopName = shop.Name,
                ShopLogoUrl = shop.LogoUrl,
                ShopAddress = address,
                Items = responses,
                TotalQuantity = responses.Sum(x => x.Quantity),
                Subtotal = responses.Sum(x => x.Subtotal),
                CanCheckout = responses.All(x => x.IsAvailable),
                Latitude = shop.Latitude,
                Longitude = shop.Longitude
            };
        }).ToList();
        return new CartResponse { Shops = shops, TotalQuantity = shops.Sum(x => x.TotalQuantity) };
    }
}
