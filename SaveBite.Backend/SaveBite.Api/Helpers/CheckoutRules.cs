using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Helpers;

// Centralize checkout eligibility, deposit, pickup and cancellation rules.
public static class CheckoutRules
{
    public const int MinimumSoftDeadlineMinutes = 5;
    public const int MinimumPickupBufferMinutes = 10;
    public const int MinimumPickupWindowMinutes = MinimumSoftDeadlineMinutes + MinimumPickupBufferMinutes;
    public const string CustomerCancellationPolicy = "Customer cancellations are non-refundable. Any online payment or deposit already paid will not be refunded.";
    // Explain why customer cancellation is unavailable for the current order and deadline.
    public static string? CancellationUnavailableReason(Order order, DateTime now)
    {
        if (order.NoShow || order.Status == OrderStatus.NoShow) return "An order marked NoShow cannot be cancelled.";
        if (order.Status is not (OrderStatus.AwaitingPayment or OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup))
            return "Only AwaitingPayment, Pending, Confirmed, Preparing, or ReadyForPickup orders can be cancelled.";
        if (!order.HardDeadlineAt.HasValue || now >= order.HardDeadlineAt.Value)
            return "This order cannot be cancelled at or after its pickup deadline.";
        return null;
    }
    // Reject cancellation outside the allowed order states or pickup deadline.
    public static void ValidateCustomerCancellation(Order order, DateTime now)
    {
        var reason = CancellationUnavailableReason(order, now);
        if (reason != null) throw AppException.Conflict(reason);
    }

    // Require valid, accurate and recent customer GPS coordinates before checkout.
    public static void ValidateLocation(CheckoutLocation? location, DateTime now)
    {
        if (location == null || !double.IsFinite(location.Latitude) || !double.IsFinite(location.Longitude)
            || location.Latitude is < -90 or > 90 || location.Longitude is < -180 or > 180
            || !double.IsFinite(location.AccuracyMeters) || location.AccuracyMeters is < 0 or > 1000
            || location.CapturedAt < now.AddMinutes(-2) || location.CapturedAt > now.AddSeconds(30))
            throw AppException.BadRequest("A fresh GPS location with accuracy within 1 km is required. Please enable location access and try again.");
    }

    // Check one-shop membership, deal availability, sale deadlines and available stock.
    public static void ValidateItems(IReadOnlyList<CartItem> items, Guid shopId, DateTime now)
    {
        if (items.Count == 0) throw AppException.Conflict("This shop has no items in your cart.");
        foreach (var item in items)
        {
            var v = item.FlashDealVariant;
            var d = v.FlashDeal;
            if (d.ShopId != shopId || v.Variant.ProductId != d.ProductId || d.Product.ShopId != shopId)
                throw AppException.BadRequest("An order may contain items from exactly one shop.");
            if (d.Status != FlashDealStatus.OnSale || v.Status != FlashDealVariantStatus.Active
                || !v.Variant.IsActive || d.Product.Status != ProductStatus.Active || d.Shop.Status != ShopStatus.Active)
                throw AppException.Conflict("One or more items are no longer available for purchase.");
            if (now < d.SaleStartTime || now >= d.OrderEndTime || now >= d.ShopClosingTime)
                throw AppException.OrderClosed("The deal is not currently accepting orders.");
            var available = (long)v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity;
            if (item.Quantity <= 0 || item.Quantity > available)
                throw AppException.OutOfStock("A cart quantity exceeds currently available stock. Please update your cart.");
            if (v.DealPrice < 0) throw AppException.Conflict("An item has an invalid price.");
        }
    }

    // Calculate minimum soft and buffered hard deadlines without exceeding the deal cutoff.
    public static PickupEstimate Pickup(DateTime now, decimal distanceKm, int etaMinutes, DateTime endsAt)
    {
        if (etaMinutes < 1 || distanceKm < 0) throw AppException.ServiceUnavailable("No valid travel estimate is available.");
        var soft = now.AddMinutes(Math.Max(MinimumSoftDeadlineMinutes, etaMinutes));
        if (now >= endsAt || soft >= endsAt)
            throw AppException.OrderClosed("There is not enough time to reach this shop before the earliest deal ends.");
        if (endsAt < now.AddMinutes(MinimumPickupWindowMinutes))
            throw AppException.OrderClosed("At least 15 minutes must remain before the deal ends to place an order.");
        var buffer = Math.Max(MinimumPickupBufferMinutes, (int)Math.Ceiling(etaMinutes * 0.30m));
        var uncapped = soft.AddMinutes(buffer);
        var hard = uncapped > endsAt ? endsAt : uncapped;
        return new PickupEstimate
        {
            DistanceKm = distanceKm,
            EtaMinutes = etaMinutes,
            BufferMinutes = buffer,
            SoftDeadlineAt = soft,
            HardDeadlineAt = hard,
            DealEndsAt = endsAt,
            BufferWasCapped = hard < uncapped
        };
    }

    // Resolve PayAtShop eligibility and the fixed deposit for the customer's level and order value.
    public static CheckoutPolicySnapshot Policy(TrustLevel? level, bool isDefault, bool permanentlyLocked,
        bool recovering, decimal total)
    {
        var p = level?.DepositPolicy;
        var ranges = p?.Ranges.OrderBy(x => x.FromOrderAmount)
            .Select(x => new DepositRangeSnapshot
            {
                Id = x.Id,
                FromOrderAmount = x.FromOrderAmount,
                ToOrderAmount = x.ToOrderAmount,
                DepositAmount = x.DepositAmount
            }).ToList() ?? [];
        string? code = null, reason = null;
        decimal deposit = 0;
        DepositRangeSnapshot? applied = null;
        if (permanentlyLocked) { code = "PermanentOfflineLock"; reason = "Your account is permanently restricted to full online payment."; }
        else if (recovering) { code = "RecoveryRequired"; reason = "Complete your required online orders before paying at the shop again."; }
        else if (level == null || !level.IsActive || p == null) { code = "PolicyUnavailable"; reason = "A valid deposit policy is unavailable for your level. Full online payment is required."; }
        else if (p.RequireFullOnlinePayment) { code = "LevelOnlineOnly"; reason = $"{level.Name} (level {level.Rank}) requires full online payment for every order. Deposits and payment at the shop are not available."; }
        else if (!p.DepositFreeOrderAmount.HasValue || !p.MaxDepositOrderAmount.HasValue)
        { code = "PolicyUnavailable"; reason = "This level's deposit policy is incomplete. Full online payment is required."; }
        else if (total > p.MaxDepositOrderAmount.Value)
        { code = "AboveDepositMaximum"; reason = "Your order exceeds this level's maximum value eligible for a deposit. Full online payment is required."; }
        else if (total > p.DepositFreeOrderAmount.Value)
        {
            var matches = ranges.Where(x => total > x.FromOrderAmount && total <= x.ToOrderAmount).ToList();
            if (matches.Count != 1 || matches[0].DepositAmount <= 0 || matches[0].DepositAmount > total)
            { code = "PolicyUnavailable"; reason = "No valid deposit range covers this order. Full online payment is required."; }
            else { applied = matches[0]; deposit = applied.DepositAmount; }
        }
        return new CheckoutPolicySnapshot
        {
            LevelId = level?.Id,
            LevelName = level?.Name ?? "Unassigned",
            LevelRank = level?.Rank,
            IsDefaultLevel = isDefault,
            PolicyId = p?.Id,
            PolicyUpdatedAt = p?.UpdatedAt,
            FreeOrderAmount = p?.DepositFreeOrderAmount,
            MaxDepositOrderAmount = p?.MaxDepositOrderAmount,
            PayAtShopAllowed = code == null,
            RestrictionCode = code,
            RestrictionReason = reason,
            RequiredDeposit = deposit,
            AppliedRange = applied,
            Ranges = ranges
        };
    }
}
