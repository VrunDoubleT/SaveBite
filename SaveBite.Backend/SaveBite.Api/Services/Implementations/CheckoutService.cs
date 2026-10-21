using SaveBite.Backend.Constants;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using System.Security.Cryptography;
using System.Text;
using SaveBite.Backend.Caching.Interfaces;
using System.Text.Json;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Calculate quotes and create one-shop orders with current policy and stock validation.
public sealed class CheckoutService : ICheckoutService
{
    private readonly IOrderCodeCache _orderCodes;
    private readonly ICheckoutRepository _checkoutRepository;
    private readonly ITravelTimeService _travelTimeService;
    private readonly ICheckoutPaymentGateway _paymentGateway;
    private readonly ICheckoutPaymentService _paymentService;
    private readonly ICheckoutStockCache _stockCache;
    private readonly TimeProvider _clock;
    private readonly ICheckoutQuoteStore _quoteStore;
    private readonly IOrderEventPublisher _orderEvents;
    private readonly ICheckoutPaymentExpirationScheduler _paymentExpirationScheduler;
    private readonly IShopOrderEventService _shopOrderEvents;

    public CheckoutService(
        ICheckoutRepository repository,
        ITravelTimeService travel,
        ICheckoutPaymentGateway gateway,
        ICheckoutPaymentService payments,
        ICheckoutStockCache stockCache,
        TimeProvider clock,
        ICheckoutQuoteStore quotes,
        ICheckoutPaymentExpirationScheduler paymentExpirationScheduler,
        IOrderEventPublisher orderEvents,
        IShopOrderEventService shopOrderEvents,
        IOrderCodeCache orderCodes)
    {
        _orderCodes = orderCodes;
        _checkoutRepository = repository;
        _travelTimeService = travel;
        _paymentGateway = gateway;
        _paymentService = payments;
        _stockCache = stockCache;
        _clock = clock;
        _quoteStore = quotes;
        _orderEvents = orderEvents;
        _paymentExpirationScheduler = paymentExpirationScheduler;
        _shopOrderEvents = shopOrderEvents;
    }

    // Use the injected UTC clock for consistent checkout validation and testable deadlines.
    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    // Load the selected shop cart and payment policy before GPS is provided; no quote or reservation is created.
    public async Task<CheckoutContextResponse> ContextAsync(Guid userId, Guid shopId, CancellationToken ct = default)
    {
        if (shopId == Guid.Empty) throw AppException.BadRequest("Select one shop to checkout.");
        var state = await _checkoutRepository.GetStateAsync(userId, shopId, ct);
        CheckoutRules.ValidateItems(state.Items, shopId, Now);
        var shop = CartService.ToResponse(state.Items).Shops.Single();
        // Resolve the deposit and PayAtShop eligibility from the current level, account restrictions and order value.
        var policy = CheckoutRules.Policy(state.Level, state.IsDefaultLevel, state.PermanentlyLocked, state.Recovering, shop.Subtotal);
        var configured = _paymentGateway.IsConfigured(state.PaymentConfig);
        return new CheckoutContextResponse
        {
            Shop = shop,
            Policy = policy,
            OnlinePaymentAvailable = configured,
            PaymentUnavailableReason = configured ? null : "This shop has not configured online payments yet.",
            IsSimulatedPayment = _paymentGateway is DevelopmentCheckoutGateway
        };
    }
    
    // Calculate checkout amounts and pickup deadlines from fresh GPS, then cache the agreed values without reserving stock.
    public async Task<CheckoutQuoteResponse> QuoteAsync(Guid userId, CheckoutQuoteRequest request, CancellationToken ct = default)
    {
        if (request.ShopId == Guid.Empty) throw AppException.BadRequest("Select one shop to checkout.");
        // Reject stale or inaccurate GPS and validate the selected shop items before calling the routing provider.
        CheckoutRules.ValidateLocation(request.Location, Now);
        var state = await _checkoutRepository.GetStateAsync(userId, request.ShopId, ct);
        CheckoutRules.ValidateItems(state.Items, request.ShopId, Now);
        var shop = state.Items[0].FlashDealVariant.FlashDeal.Shop;
        // Obtain distance, travel time and optional map geometry from the customer location to the shop.
        var estimate = await _travelTimeService.EstimateAsync(request.Location, shop.Latitude, shop.Longitude, ct);
        // Route calls can take time; validate fresh DB state after obtaining the estimate.
        state = await _checkoutRepository.GetStateAsync(userId, request.ShopId, ct);
        var now = Now;
        CheckoutRules.ValidateLocation(request.Location, now);
        CheckoutRules.ValidateItems(state.Items, request.ShopId, now);
        var currentShop = state.Items[0].FlashDealVariant.FlashDeal.Shop;
        if (shop.Latitude != currentShop.Latitude || shop.Longitude != currentShop.Longitude)
            throw AppException.Conflict("The shop location changed. Please refresh your GPS estimate.");
        var summary = CartService.ToResponse(state.Items).Shops.Single();
        var policy = CheckoutRules.Policy(state.Level, state.IsDefaultLevel, state.PermanentlyLocked, state.Recovering, summary.Subtotal);
        // Calculate the minimum soft deadline and buffered hard deadline, capped by the earliest pickup cutoff.
        var pickup = CheckoutRules.Pickup(now, estimate.DistanceKm, estimate.EtaMinutes, EndTime(state));
        // Expire the quote within 60 seconds, GPS freshness and the latest feasible checkout time.
        var expiresAt = new[] { now.AddSeconds(60), request.Location.CapturedAt.AddMinutes(2), pickup.DealEndsAt.AddMinutes(-Math.Max(CheckoutRules.MinimumPickupWindowMinutes, estimate.EtaMinutes)) }.Min();
        if (expiresAt <= now) throw AppException.OrderClosed("There is not enough time left to confirm this quote before the pickup cutoff.");
        var configured = _paymentGateway.IsConfigured(state.PaymentConfig);
        // Return full checkout information to the UI, including the optional route for map display.
        var response = new CheckoutQuoteResponse
        {
            Id = Guid.NewGuid(),
            Shop = summary,
            Policy = policy,
            Pickup = pickup,
            OnlinePaymentAvailable = configured,
            PaymentUnavailableReason = configured ? null : "This shop has not configured online _paymentService. Orders requiring online payment cannot be placed yet.",
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsSimulatedPayment = _paymentGateway is DevelopmentCheckoutGateway,
            Route = string.IsNullOrWhiteSpace(estimate.EncodedPolyline) ? null : new CheckoutRouteResponse
            {
                OriginLatitude = request.Location.Latitude,
                OriginLongitude = request.Location.Longitude,
                ShopLatitude = currentShop.Latitude,
                ShopLongitude = currentShop.Longitude,
                EncodedPolyline = estimate.EncodedPolyline
            }
        };
        // Cache GPS, agreed calculations and a validation hash in Redis; omit full shop and item response snapshots.
        await _quoteStore.SaveAsync(new CheckoutQuote
        {
            Id = response.Id, UserId = userId, ShopId = request.ShopId,
            Location = request.Location, Pickup = pickup, RequiredDeposit = policy.RequiredDeposit, TotalAmount = summary.Subtotal,
            ShopLatitude = currentShop.Latitude, ShopLongitude = currentShop.Longitude,
            Fingerprint = Fingerprint(state, policy), CreatedAt = now, ExpiresAt = expiresAt
        }, ct);
        return response;
    }

    // Validate an accepted quote and atomically reserve stock and create one shop order before starting payment.
    public async Task<CheckoutOrderResponse> PlaceAsync(Guid userId, PlaceCheckoutOrderRequest request, CancellationToken ct = default)
    {
        if (request.QuoteId == Guid.Empty || request.RequestId == Guid.Empty || request.PaymentMethod is not ("OnlinePayment" or "PayAtShop"))
            throw AppException.BadRequest("A valid checkout quote, request ID and payment method are required.");
        // A committed order remains retryable even after the temporary Redis quote expires.
        var previous = await _checkoutRepository.FindRequestAsync(userId, request.RequestId, request.QuoteId, ct);
        var quote = previous == null ? await _quoteStore.GetAsync(userId, request.QuoteId, ct) : null;
        if (previous == null && quote == null)
            throw AppException.Conflict("Your checkout quote expired or is unavailable. Refresh GPS and review the latest amounts.");
        // The repository opens a transaction and locks the customer, stock and related checkout data.
        var shopId = previous?.ShopId ?? quote!.ShopId;
        var notifyShopPending = false;
        var orderId = await RetryOrderCodeAsync(() => _checkoutRepository.WithUserLockAsync(userId, shopId, async () =>
        {
            // Recheck after acquiring locks so concurrent retries reuse the same committed order.
            var existing = await _checkoutRepository.FindRequestAsync(userId, request.RequestId, request.QuoteId, ct);
            if (existing != null)
            {
                if (existing.CheckoutQuoteId != request.QuoteId || existing.PaymentMethod.ToString() != request.PaymentMethod)
                    throw AppException.Conflict("This request ID was already used for a different checkout.");
                return existing.Id;
            }
            // Revalidate quote expiry and saved GPS after any time spent waiting for locks.
            var now = Now;
            if (quote == null || quote.ExpiresAt <= now) throw AppException.Conflict("Your checkout quote expired. Refresh GPS and review the latest amounts.");
            CheckoutRules.ValidateLocation(quote.Location, now);
            // Read authoritative DB state and reject expired, inactive or insufficient-stock items.
            var state = await _checkoutRepository.GetStateAsync(userId, quote.ShopId, ct);
            CheckoutRules.ValidateItems(state.Items, quote.ShopId, now);
            var summary = CartService.ToResponse(state.Items).Shops.Single();
            var policy = CheckoutRules.Policy(state.Level, state.IsDefaultLevel, state.PermanentlyLocked, state.Recovering, summary.Subtotal);
            var currentShop = state.Items[0].FlashDealVariant.FlashDeal.Shop;
            if (quote.ShopLatitude != currentShop.Latitude || quote.ShopLongitude != currentShop.Longitude)
                throw AppException.Conflict("The shop location changed. Please refresh your GPS estimate.");
            // Require a new quote if cart contents, prices or deposit policy no longer match the agreed calculations.
            if (quote.Pickup == null || Fingerprint(state, policy) != quote.Fingerprint
                || summary.Subtotal != quote.TotalAmount || policy.RequiredDeposit != quote.RequiredDeposit)
                throw AppException.Conflict("Cart prices, quantities or your deposit policy changed. Refresh checkout and review them before placing your order.");
            // Recheck feasibility with current time, but keep the pickup times already shown in the quote.
            CheckoutRules.Pickup(now, quote.Pickup.DistanceKm, quote.Pickup.EtaMinutes, EndTime(state));
            var pickup = quote.Pickup;
            if (pickup.SoftDeadlineAt <= now || pickup.HardDeadlineAt <= pickup.SoftDeadlineAt || pickup.HardDeadlineAt > EndTime(state))
                throw AppException.OrderClosed("The quoted pickup deadlines are no longer valid. Refresh checkout.");
            // Enforce PayAtShop eligibility; OnlinePayment collects the full total and PayAtShop collects the fixed deposit.
            var method = request.PaymentMethod == "PayAtShop" ? PaymentMethod.PayAtShop : PaymentMethod.OnlinePayment;
            if (method == PaymentMethod.PayAtShop && !policy.PayAtShopAllowed)
                throw AppException.Forbidden(policy.RestrictionReason ?? "Full online payment is required.");
            var total = quote.TotalAmount;
            var deposit = method == PaymentMethod.PayAtShop ? quote.RequiredDeposit : 0;
            var online = method == PaymentMethod.OnlinePayment ? total : deposit;
            notifyShopPending = online == 0;
            if (total < 0 || total > 9999999999.99m || online < 0 || decimal.Truncate(online) != online)
                throw AppException.Conflict("This order does not have valid VND payment amounts.");
            if (online > 0 && !_paymentGateway.IsConfigured(state.PaymentConfig))
                throw AppException.ServiceUnavailable("This shop has not configured online payments yet. No order was created.");
            // Persist the agreed amounts and absolute pickup deadlines; payment expires within 120 seconds or the hard deadline.
            var order = new Order
            {
                Id = Guid.NewGuid(), OrderCode = await _orderCodes.NextAsync(shopId, OrderBusinessDate.FromUtc(now),
                    await _checkoutRepository.OrderCodeMaximumAsync(shopId, OrderBusinessDate.FromUtc(now), ct), ct),
                CustomerId = userId, ShopId = quote.ShopId, PaymentMethod = method, Status = online > 0 ? OrderStatus.AwaitingPayment : OrderStatus.Pending,
                TotalAmount = total, DepositAmount = deposit, RemainingAmount = method == PaymentMethod.PayAtShop ? total - deposit : 0,
                RequiredOnlineAmount = online, DistanceKm = pickup.DistanceKm, EtaMinutes = pickup.EtaMinutes,
                SoftDeadlineAt = pickup.SoftDeadlineAt, HardDeadlineAt = pickup.HardDeadlineAt,
                CheckoutQuoteId = quote.Id, CheckoutRequestId = request.RequestId, CheckoutPolicySnapshotJson = JsonSerializer.Serialize(policy),
                PaymentExpiresAt = online > 0 ? new[] { now.AddSeconds(120), pickup.HardDeadlineAt }.Min() : null,
                InventoryReservationActive = true, CreatedAt = now, UpdatedAt = now
            };
            // Record immutable product names, prices and quantities from the validated DB items for order history.
            foreach (var item in summary.Items)
                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(), OrderId = order.Id, FlashDealVariantId = item.FlashDealVariantId,
                    ProductNameSnapshot = item.ProductName, VariantNameSnapshot = item.VariantName,
                    OriginalPriceSnapshot = item.OriginalPrice, DealPriceSnapshot = item.UnitPrice,
                    UnitPrice = item.UnitPrice, Quantity = item.Quantity, Subtotal = item.Subtotal
                });
            // Create the initial payment record; a zero-deposit order needs no online collection.
            order.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(), OrderId = order.Id, PaymentType = method == PaymentMethod.OnlinePayment ? PaymentType.Full : PaymentType.Deposit,
                Method = PaymentMethod.OnlinePayment, Amount = online, Status = online > 0 ? PaymentStatus.Pending : PaymentStatus.Succeeded,
                GatewayTransactionId = online > 0 && _paymentGateway is DevelopmentCheckoutGateway ? DevelopmentCheckoutGateway.Prefix + "PENDING" : null,
                GatewayOrderCode = online > 0 ? Random.Shared.NextInt64(1, 9007199254740991) : null,
                PaidAt = online == 0 ? now : null, CreatedAt = now, UpdatedAt = now
            });
            // Record the customer as the actor who submitted the order, awaiting payment when online collection is required.
            order.StatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(), OrderId = order.Id, ToStatus = online > 0 ? OrderStatus.AwaitingPayment : OrderStatus.Pending, ChangedBy = userId,
                Note = online > 0 ? "Order created; inventory reserved while awaiting online payment." : "Order created; no online deposit required.", CreatedAt = now
            });
            // Conditionally reserve every item; any shortage rolls back the entire order transaction.
            await _checkoutRepository.ReserveAsync(state.Items, ct);
            _checkoutRepository.AddOrder(order);
            // Settle inventory immediately when no online payment is due; otherwise keep it reserved for payment.
            if (online == 0) await _checkoutRepository.CompleteInventoryAsync(order, true, ct);
            // Remove only this shop from the cart and commit its order, payment and inventory changes together.
            await _checkoutRepository.RemoveShopCartAsync(userId, quote.ShopId, ct);
            await _checkoutRepository.SaveAsync(ct);
            // Persist the expiry job before commit; scheduling failure rolls back the order and its reservation.
            if (order.PaymentExpiresAt.HasValue)
            {
                order.PaymentExpirationJobId = _paymentExpirationScheduler.Schedule(order.Id, order.PaymentExpiresAt.Value);
                await _checkoutRepository.SaveAsync(ct);
            }
            if (order.Status == OrderStatus.Pending)
                await _orderEvents.QueuePendingAsync(order, ct);
            return order.Id;
        }, ct), ct);
        if (notifyShopPending)
            await _shopOrderEvents.NotifyAsync(shopId, orderId, RealtimeEvents.OrderReasons.Pending, ct);
        // After the transaction commits, refresh Redis stock and create or reuse the payment link.
        await _stockCache.SyncAsync(orderId, ct);
        var result = await _paymentService.StartAsync(userId, orderId, ct);
        return result;
    }

    // Use the earliest deal order cutoff or shop closing time across all items in the selected shop.
    private static DateTime EndTime(CheckoutState state)
        => state.Items.Min(x => x.FlashDealVariant.FlashDeal.OrderEndTime < x.FlashDealVariant.FlashDeal.ShopClosingTime
            ? x.FlashDealVariant.FlashDeal.OrderEndTime : x.FlashDealVariant.FlashDeal.ShopClosingTime);

    // Hash quote-sensitive data to detect changes without storing a full shop or item snapshot in Redis.
    // Redis data loss can overlap an uncommitted allocation; the DB unique constraint is the final arbiter.
    private static async Task<Guid> RetryOrderCodeAsync(Func<Task<Guid>> create, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return await create(); }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_orders_shop_date_code" }) { }
        }
        throw AppException.ServiceUnavailable("Could not allocate a pickup code. Please retry checkout.");
    }

    // Hash current cart and policy inputs to detect changes after the customer reviewed a quote.
    private static string Fingerprint(CheckoutState state, CheckoutPolicySnapshot policy)
    {
        var shop = state.Items[0].FlashDealVariant.FlashDeal.Shop;
        var data = JsonSerializer.Serialize(new
        {
            shop.Id, shop.Latitude, shop.Longitude, policy,
            // Stable item ordering prevents equivalent cart contents from producing different hashes.
            Items = state.Items.OrderBy(x => x.Id).Select(x => new
            {
                x.Id, x.FlashDealVariantId, x.Quantity, x.FlashDealVariant.DealPrice, x.FlashDealVariant.OriginalPrice,
                x.FlashDealVariant.FlashDeal.SaleStartTime, x.FlashDealVariant.FlashDeal.OrderEndTime, x.FlashDealVariant.FlashDeal.ShopClosingTime
            })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data)));
    }

}
