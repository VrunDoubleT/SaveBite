using SaveBite.Backend.Constants;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Caching.Interfaces;
using System.Text.Json;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Reconcile provider payments, order submission, cancellation and reservation expiry.
public sealed class CheckoutPaymentService : ICheckoutPaymentService
{
    private readonly ICheckoutRepository _checkoutRepository;
    private readonly ICheckoutPaymentGateway _paymentGateway;
    private readonly ICheckoutStockCache _stockCache;
    private readonly TimeProvider _clock;
    private readonly IOrderEventPublisher _orderEvents;
    private readonly ICheckoutPaymentExpirationScheduler _paymentExpirationScheduler;
    private readonly IShopOrderEventService _shopOrderEvents;
    private readonly ILogger<CheckoutPaymentService> _logger;

    public CheckoutPaymentService(
        ICheckoutRepository repository,
        ICheckoutPaymentGateway gateway,
        ICheckoutStockCache stockCache,
        TimeProvider clock,
        ICheckoutPaymentExpirationScheduler paymentExpirationScheduler,
        ILogger<CheckoutPaymentService> logger,
        IOrderEventPublisher orderEvents,
        IShopOrderEventService shopOrderEvents)
    {
        _checkoutRepository = repository;
        _paymentGateway = gateway;
        _stockCache = stockCache;
        _clock = clock;
        _orderEvents = orderEvents;
        _paymentExpirationScheduler = paymentExpirationScheduler;
        _shopOrderEvents = shopOrderEvents;
        _logger = logger;
    }

    // Use the injected UTC clock for consistent payment and pickup deadline checks.
    private DateTime Now => _clock.GetUtcNow().UtcDateTime;
    // Reconcile a trusted internal order operation without a customer ownership filter.
    public Task<CheckoutOrderResponse> ReconcileOrderAsync(Guid orderId, bool createRemainingLink = false, CancellationToken ct = default)
        => ProcessAsync(orderId, null, createRemainingLink, false, ct);
    // Explicitly reconcile a customer-owned payment; call only from commands, never view endpoints.
    public Task<CheckoutOrderResponse> ReconcileAsync(Guid userId, Guid orderId, CancellationToken ct = default)
        => ProcessAsync(orderId, userId, false, false, ct);
    // Commit payment-link creation intent before creating or reusing the initial payment link.
    public async Task<CheckoutOrderResponse> StartAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        // Commit creation intent before contacting the provider so a disconnect cannot hide a potentially payable link.
        await _checkoutRepository.WithOrderLockAsync(orderId, async () =>
        {
            var order = await _checkoutRepository.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound("Order not found.");
            var payment = CheckoutOrderMapper.RequiredPayment(order);
            if (payment?.Status == PaymentStatus.Pending && payment.Amount > 0 && order.Status == OrderStatus.AwaitingPayment
                && order.PaymentExpiresAt > Now && !payment.GatewayCreationAttempted)
            {
                payment.GatewayCreationAttempted = true;
                await _checkoutRepository.SaveAsync(ct);
            }
            return true;
        }, ct);
        return await ProcessAsync(orderId, userId, true, false, ct);
    }
    // Apply customer cancellation through the shared payment and inventory workflow.
    public Task<CheckoutOrderResponse> CancelAsync(Guid userId, Guid orderId, CancellationToken ct = default)
        => ProcessAsync(orderId, userId, false, true, ct);

    // Apply explicit reconciliation, link creation, cancellation and expiry commands.
    private async Task<CheckoutOrderResponse> ProcessAsync(Guid orderId, Guid? userId, bool createLink, bool cancel, CancellationToken ct, bool cancelExpirationJob = true)
    {
        string? expirationJobId = null;
        var notifyShopPending = false;
        var notifyShopCancellation = false;
        var notifyRemainingPayment = false;
        // Serialize customer requests, webhooks and expiry jobs before changing payment or inventory.
        var result = await _checkoutRepository.WithOrderLockAsync(orderId, async () =>
        {
            var order = await _checkoutRepository.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound("Order not found.");
            expirationJobId = order.PaymentExpirationJobId;
            notifyShopCancellation = cancel && order.Status is (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup);
            // Validate cancellation eligibility and make repeated customer cancellation idempotent.
            if (cancel)
            {
                if (order.Status == OrderStatus.Cancelled && order.CancelledBy == userId)
                    return CheckoutOrderMapper.Map(order, now: Now);
                CheckoutRules.ValidateCustomerCancellation(order, Now);
            }
            // Reconcile any remaining-balance payment before processing the initial full payment or deposit.
            var remainingPaidCount = order.Payments.Count(x => x.PaymentType == PaymentType.Remaining && x.Status == PaymentStatus.Succeeded);
            await ReconcileRemainingAsync(order, createLink, cancel, ct);
            notifyRemainingPayment = order.Payments.Count(x => x.PaymentType == PaymentType.Remaining && x.Status == PaymentStatus.Succeeded) > remainingPaidCount;
            var payment = CheckoutOrderMapper.RequiredPayment(order);
            // Free-deposit and already-paid orders need no initial payment link or repeated inventory settlement.
            if (payment == null || payment.Amount == 0 || payment.Status == PaymentStatus.Succeeded)
            {
                if (cancel)
                {
                    await CancelCustomerOrderAsync(order, payment, ct);
                    await _checkoutRepository.SaveAsync(ct);
                }
                return CheckoutOrderMapper.Map(order, now: Now);
            }
            // Continue reconciling a known late payment even if inventory was already released.
            if (order.CheckoutQuoteId == null && !cancel) return CheckoutOrderMapper.Map(order, now: Now);
            var config = order.Shop.PaymentConfig;
            string? message = null;
            var expired = order.PaymentExpiresAt <= Now || order.HardDeadlineAt <= Now;
            // Close directly when no provider link could have been created; otherwise resolve provider state first.
            if (!payment.GatewayCreationAttempted && (expired || cancel))
            {
                await CloseAsync(order, payment, cancel ? OrderStatus.Cancelled : OrderStatus.Expired, ct);
            }
            else if (!_paymentGateway.IsConfigured(config))
            {
                message = "This shop's payment service is unavailable. Payment has not been confirmed. Please contact the shop or retry.";
            }
            else
            {
                try
                {
                    GatewayPayment status;
                    // Create a link only for a payable AwaitingPayment order without an existing checkout URL.
                    if (createLink && !expired && order.Status == OrderStatus.AwaitingPayment && payment.Status == PaymentStatus.Pending && payment.GatewayCheckoutUrl == null)
                    {
                        payment.GatewayCreationAttempted = true;
                        // Persist attempts even if the provider times out: do not assume a link was never created.
                        await _checkoutRepository.SaveAsync(ct);
                        try { status = await _paymentGateway.CreateAsync(order, payment, config!, ct); }
                        catch (AppException) { status = await _paymentGateway.GetAsync(payment, config!, ct); }
                    }
                    else if (!payment.GatewayCreationAttempted)
                    {
                        return CheckoutOrderMapper.Map(order, "Continue to online payment to complete this order.", Now);
                    }
                    else
                    {
                        status = await _paymentGateway.GetAsync(payment, config!, ct);
                    }
                    // Preserve the highest verified collected amount, including partial payments.
                    payment.CollectedAmount = Math.Max(payment.CollectedAmount, status.AmountPaid);
                    // Only verified full payment or deposit can settle inventory and submit the order to the shop.
                    if (status.Status == "PAID" && status.AmountPaid >= payment.Amount)
                    {
                        notifyShopPending |= await PaidAsync(order, payment, status, ct);
                        if (cancel) await CancelCustomerOrderAsync(order, payment, ct);
                    }
                    // Close confirmed inactive links; a missing link remains retryable before the payment deadline.
                    else if (status.Status is "CANCELLED" or "EXPIRED" or "NOT_FOUND")
                    {
                        if (cancel || expired || status.Status != "NOT_FOUND")
                            await CloseAsync(order, payment, cancel ? OrderStatus.Cancelled : OrderStatus.Expired, ct);
                        else message = "A payment link has not been created. Please retry payment.";
                    }
                    else if (cancel || expired)
                    {
                        // Ask the provider to close first, resolving the cancel-vs-paid race using its response.
                        var closed = await _paymentGateway.CancelAsync(payment, config!, ct);
                        payment.CollectedAmount = Math.Max(payment.CollectedAmount, closed.AmountPaid);
                        if (closed.Status == "PAID" && closed.AmountPaid >= payment.Amount)
                        {
                            notifyShopPending |= await PaidAsync(order, payment, closed, ct);
                            if (cancel) await CancelCustomerOrderAsync(order, payment, ct);
                        }
                        else if (closed.Status is "CANCELLED" or "EXPIRED")
                            await CloseAsync(order, payment, cancel ? OrderStatus.Cancelled : OrderStatus.Expired, ct);
                        else message = "Payment cancellation is not confirmed yet. Please check again.";
                    }
                    else
                    {
                        // Save provider link details on the existing Payment row instead of creating another payment.
                        payment.GatewayTransactionId = status.LinkId ?? payment.GatewayTransactionId;
                        payment.GatewayCheckoutUrl = status.CheckoutUrl ?? payment.GatewayCheckoutUrl;
                        payment.UpdatedAt = Now;
                    }
                }
                catch (AppException ex)
                {
                    message = ex.Message;
                    // Keep the reservation until a provider response resolves a possible in-flight payment.
                }
            }
            await _checkoutRepository.SaveAsync(ct);
            return CheckoutOrderMapper.Map(order, message, Now);
        }, ct);
        // Notify detail subscribers only after the verified balance payment has committed.
        if (notifyRemainingPayment)
        {
            try { await _shopOrderEvents.NotifyAsync(result.ShopId, orderId, RealtimeEvents.OrderReasons.PaymentSucceeded, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { _logger.LogWarning(ex, "Could not send the payment realtime notification for order {OrderId}.", orderId); }
        }
        if (notifyShopPending && result.Status == nameof(OrderStatus.Pending)
            && result.PaymentStatus == nameof(PaymentStatus.Succeeded))
        {
            try
            {
                await _shopOrderEvents.NotifyAsync(result.ShopId, orderId, RealtimeEvents.OrderReasons.Pending, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Could not send the paid-order realtime notification for order {OrderId}.", orderId);
            }
        }
        // Notify all active shop operators only after a customer cancellation has committed.
        if (notifyShopCancellation && result.Status == nameof(OrderStatus.Cancelled))
        {
            try { await _shopOrderEvents.NotifyAsync(result.ShopId, orderId, RealtimeEvents.OrderReasons.Cancelled, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { _logger.LogWarning(ex, "Could not send the cancellation realtime notification for order {OrderId}.", orderId); }
        }
        // Delete only after commit so a rollback never removes the job protecting an unpaid reservation.
        if (cancelExpirationJob && expirationJobId != null
            && (result.PaymentStatus is nameof(PaymentStatus.Succeeded) or nameof(PaymentStatus.Cancelled) or nameof(PaymentStatus.Expired)
                || result.Status == nameof(OrderStatus.Cancelled)))
        {
            try
            {
                if (!_paymentExpirationScheduler.Delete(expirationJobId))
                    _logger.LogWarning("Payment expiration job {JobId} for order {OrderId} could not be deleted.", expirationJobId, orderId);
            }
            catch (Exception ex)
            {
                // Payment is already committed; the remaining job safely exits when it sees the resolved state.
                _logger.LogWarning(ex, "Could not delete payment expiration job {JobId} for order {OrderId}.", expirationJobId, orderId);
            }
        }
        // Refresh Redis stock only after the payment transaction has committed.
        await _stockCache.SyncAsync(orderId, ct);
        return result;
    }

    // Record verified money once and distinguish valid payment from late or excess payment requiring review.
    private async Task<bool> PaidAsync(Order order, Payment payment, GatewayPayment status, CancellationToken ct)
    {
        // Duplicate notifications must not settle the same reserved inventory twice.
        if (payment.Status == PaymentStatus.Succeeded) return false;
        var previous = order.Status;
        var late = order.HardDeadlineAt <= Now || order.Status is OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.Rejected or OrderStatus.NoShow || !order.InventoryReservationActive;
        payment.CollectedAmount = Math.Max(payment.CollectedAmount, status.AmountPaid);
        payment.Status = PaymentStatus.Succeeded;
        payment.GatewayTransactionId = status.LinkId ?? payment.GatewayTransactionId;
        payment.GatewayCheckoutUrl = null;
        payment.PaidAt = Now; payment.UpdatedAt = Now; order.UpdatedAt = Now;
        order.PaymentReviewRequired = late || status.AmountPaid > payment.Amount;
        // Record late money without reviving closed orders or selling inventory that was already released.
        if (late)
        {
            await _checkoutRepository.CompleteInventoryAsync(order, false, ct);
            if (order.Status is not (OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.NoShow)) { order.Status = OrderStatus.Expired; order.ExpiredAt ??= Now; }
        }
        else
        {
            await _checkoutRepository.CompleteInventoryAsync(order, true, ct);
            // Only a verified full payment or deposit submits the order to the shop.
            if (order.Status == OrderStatus.AwaitingPayment) order.Status = OrderStatus.Pending;
        }
        _checkoutRepository.AddHistory(new OrderStatusHistory
        {
            Id = Guid.NewGuid(), OrderId = order.Id, FromStatus = previous, ToStatus = order.Status,
            Note = late ? "Payment arrived after pickup/order expiry; manual payment review required. Inventory was not sold." : "Required online payment verified with the payment provider.",
            CreatedAt = Now
        });
        var becamePending = previous == OrderStatus.AwaitingPayment && order.Status == OrderStatus.Pending;
        if (becamePending)
            await _orderEvents.QueuePendingAsync(order, ct);
        return becamePending;
    }
    
    // Expire an unpaid order after provider closure is confirmed or when link creation was never attempted.
    private async Task CloseAsync(Order order, Payment payment, OrderStatus status, CancellationToken ct)
    {
        if (status == OrderStatus.Cancelled)
        {
            await CancelCustomerOrderAsync(order, payment, ct);
            return;
        }
        // Skip an already-released reservation so duplicate expiration jobs cannot return stock twice.
        if (!order.InventoryReservationActive) return;
        await _checkoutRepository.CompleteInventoryAsync(order, false, ct);
        var previous = order.Status;
        order.Status = status; order.UpdatedAt = Now;
        order.ExpiredAt = Now;
        order.RemainingAmount = 0;
        order.PaymentReviewRequired |= payment.CollectedAmount > 0;
        payment.Status = PaymentStatus.Expired; payment.GatewayCheckoutUrl = null; payment.UpdatedAt = Now;
        _checkoutRepository.AddHistory(new OrderStatusHistory { Id = Guid.NewGuid(), OrderId = order.Id, FromStatus = previous, ToStatus = status, Note = "Unpaid order closed; inventory reservation released.", CreatedAt = Now });
    }

    // Cancel an eligible customer order without refunding collected money and release its applicable inventory.
    private async Task CancelCustomerOrderAsync(Order order, Payment? payment, CancellationToken ct)
    {
        CheckoutRules.ValidateCustomerCancellation(order, Now);
        var previous = order.Status;
        await _checkoutRepository.ReleaseCancelledInventoryAsync(order, ct);
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = Now; order.UpdatedAt = Now;
        order.CancelledBy = order.CustomerId;
        order.CancelReason = CheckoutRules.CustomerCancellationPolicy;
        order.RemainingAmount = 0;
        if (payment != null)
        {
            // A succeeded payment remains succeeded: cancellation does not mean a refund.
            if (payment.Status == PaymentStatus.Pending) payment.Status = PaymentStatus.Cancelled;
            payment.GatewayCheckoutUrl = null; payment.UpdatedAt = Now;
        }
        _checkoutRepository.AddHistory(new OrderStatusHistory
        {
            Id = Guid.NewGuid(), OrderId = order.Id, FromStatus = previous, ToStatus = OrderStatus.Cancelled,
            Note = CheckoutRules.CustomerCancellationPolicy + " Applicable inventory released.", CreatedAt = Now
        });
    }

    // Verify the separate PayAtShop balance payment before the shop can complete an order.
    private async Task ReconcileRemainingAsync(Order order, bool createLink, bool cancel, CancellationToken ct)
    {
        if (!_paymentGateway.IsConfigured(order.Shop.PaymentConfig)) return;
        // Reconcile retained attempts as well so late payment on an old QR is never attributed to the new one.
        foreach (var payment in order.Payments.Where(x => x.PaymentType == PaymentType.Remaining
            && x.Status != PaymentStatus.Succeeded && x.GatewayCreationAttempted).OrderBy(x => x.Status == PaymentStatus.Pending).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id).ToList())
        {
            try
            {
                // Close balance-payment links when cancellation, pickup expiry or a terminal order state requires it.
                var closed = cancel || payment.Status != PaymentStatus.Pending
                    || order.Payments.Any(x => x.PaymentType == PaymentType.Remaining && x.Status == PaymentStatus.Succeeded)
                    || order.HardDeadlineAt <= Now || order.Status is OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired or OrderStatus.NoShow or OrderStatus.Completed;
                GatewayPayment status;
                if (closed && payment.Status != PaymentStatus.Pending)
                {
                    status = await _paymentGateway.GetAsync(payment, order.Shop.PaymentConfig!, ct);
                    if (status.Status is not ("PAID" or "CANCELLED" or "EXPIRED" or "NOT_FOUND"))
                        status = await _paymentGateway.CancelAsync(payment, order.Shop.PaymentConfig!, ct);
                }
                else if (closed) status = await _paymentGateway.CancelAsync(payment, order.Shop.PaymentConfig!, ct);
                else if (createLink && payment.GatewayCheckoutUrl == null && payment.Status == PaymentStatus.Pending)
                {
                    try { status = await _paymentGateway.CreateAsync(order, payment, order.Shop.PaymentConfig!, ct); }
                    catch (AppException) { status = await _paymentGateway.GetAsync(payment, order.Shop.PaymentConfig!, ct); }
                }
                else status = await _paymentGateway.GetAsync(payment, order.Shop.PaymentConfig!, ct);
                payment.CollectedAmount = Math.Max(payment.CollectedAmount, status.AmountPaid);
                order.PaymentReviewRequired |= closed && payment.CollectedAmount > 0;
                payment.GatewayQrCode = status.QrCode ?? payment.GatewayQrCode;
                if (status.Status == "PAID" && status.AmountPaid >= payment.Amount)
                {
                    payment.Status = PaymentStatus.Succeeded; payment.PaidAt = Now; payment.GatewayCheckoutUrl = null;
                    order.PaymentReviewRequired |= closed || status.AmountPaid > payment.Amount;
                    ordersHistory("Remaining payment verified with the payment provider.");
                }
                else if (status.Status is "CANCELLED" or "EXPIRED" || (closed && status.Status == "NOT_FOUND"))
                { payment.Status = status.Status == "EXPIRED" ? PaymentStatus.Expired : PaymentStatus.Cancelled; payment.GatewayCheckoutUrl = null; payment.GatewayQrCode = null; }
                else
                {
                    if (closed && cancel) throw AppException.Conflict("Remaining payment closure is not confirmed yet.");
                    payment.GatewayCheckoutUrl = status.CheckoutUrl ?? payment.GatewayCheckoutUrl;
                    payment.GatewayTransactionId = status.LinkId ?? payment.GatewayTransactionId;
                }
                payment.UpdatedAt = Now;
                await _checkoutRepository.SaveAsync(ct);
            }
            catch (AppException)
            {
                if (cancel) throw AppException.ServiceUnavailable("The remaining payment must be closed before cancellation. Please retry.");
                // The amount remains unverified; completion stays blocked until reconciliation succeeds.
            }
        }
        // Record balance-payment verification without changing preparation or completion status.
        void ordersHistory(string note) => _checkoutRepository.AddHistory(new OrderStatusHistory
        { Id = Guid.NewGuid(), OrderId = order.Id, ToStatus = order.Status, Note = note, CreatedAt = Now });
    }

    // Validate the notification signature and query authoritative provider state before updating the order.
    public async Task WebhookAsync(JsonElement body, CancellationToken ct = default)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("orderCode", out var code) || code.ValueKind != JsonValueKind.Number || !code.TryGetInt64(out var gatewayCode))
            throw AppException.BadRequest("Invalid payment notification.");
        var id = await _checkoutRepository.FindGatewayOrderAsync(gatewayCode, ct);
        if (!id.HasValue) return; // Includes payOS's webhook-registration sample; it never creates orders.
        var order = await _checkoutRepository.GetOrderAsync(id.Value, null, ct) ?? throw AppException.NotFound();
        if (!_paymentGateway.IsConfigured(order.Shop.PaymentConfig) || !_paymentGateway.VerifyWebhook(body, order.Shop.PaymentConfig!))
            throw AppException.Forbidden("Invalid payment notification signature.");
        // Never accept success, amounts or order state from client URLs or the webhook alone.
        await ProcessAsync(id.Value, null, false, false, ct);
    }
    // Process only the scheduled order and let Hangfire retry unresolved payment-provider failures.
    public async Task ExpireAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _checkoutRepository.GetOrderAsync(orderId, null, ct);
        // Scheduling precedes commit; a rolled-back checkout can leave a harmless orphan job.
        if (order == null || !order.PaymentExpiresAt.HasValue) return;
        var payment = CheckoutOrderMapper.RequiredPayment(order);
        if (payment == null || payment.Amount == 0 || payment.Status != PaymentStatus.Pending) return;
        if (order.PaymentExpiresAt > Now)
            throw new InvalidOperationException("The payment expiration job ran before its deadline.");
        // Reuse provider closure and inventory release logic without requiring an active customer session.
        var result = await ProcessAsync(orderId, null, false, false, ct, cancelExpirationJob: false);
        // Fail unresolved expiration so Hangfire retries rather than silently keeping stock reserved.
        if (result.PaymentStatus == nameof(PaymentStatus.Pending))
            throw new InvalidOperationException("Payment closure is not confirmed; retry expiration without releasing reserved stock.");
    }
}
