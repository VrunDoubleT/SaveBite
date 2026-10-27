using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Constants;
using System.Security.Cryptography;
using System.Text;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Manage shop fulfillment, payment collection, deadlines, refunds and operator audit history.
public sealed class ShopOrderService : IShopOrderService
{
    private readonly IShopOrderRepository _shops;
    private readonly ICheckoutRepository _orders;
    private readonly ICheckoutPaymentService _payments;
    private readonly ICheckoutPaymentGateway _gateway;
    private readonly ICheckoutStockCache _stock;
    private readonly IEmailService _email;
    private readonly TimeProvider _clock;
    private readonly ILogger<ShopOrderService> _logger;
    private readonly IShopOrderConfirmationScheduler _confirmationScheduler;
    private readonly IShopOrderEventService _orderEvents;
    private readonly IShopOrderNoShowScheduler _noShowScheduler;
    private readonly IOrderEventPublisher _eventPublisher;

    public ShopOrderService(
        IShopOrderRepository shops,
        ICheckoutRepository orders,
        ICheckoutPaymentService payments,
        ICheckoutPaymentGateway gateway,
        ICheckoutStockCache stock,
        IEmailService email,
        TimeProvider clock,
        ILogger<ShopOrderService> logger,
        IShopOrderConfirmationScheduler confirmationScheduler,
        IShopOrderEventService orderEvents,
        IShopOrderNoShowScheduler noShowScheduler,
        IOrderEventPublisher eventPublisher)
    {
        _shops = shops;
        _orders = orders;
        _payments = payments;
        _gateway = gateway;
        _stock = stock;
        _email = email;
        _clock = clock;
        _logger = logger;
        _confirmationScheduler = confirmationScheduler;
        _orderEvents = orderEvents;
        _noShowScheduler = noShowScheduler;
        _eventPublisher = eventPublisher;
    }

    // Keep the count endpoint subject to the same active shop access checks as order operations.
    public async Task<int> PendingCountAsync(Guid userId, Guid shopId, CancellationToken ct = default)
    {
        await _shops.RequireAccessAsync(userId, shopId, ct);
        return await _shops.PendingCountAsync(shopId, ct);
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;
    // Resolve the authenticated owner's shop for the order workspace.
    public Task<Guid> OwnerShopAsync(Guid userId, CancellationToken ct = default) => _shops.OwnerShopAsync(userId, ct);
    // Validate shop access, paging and search filters before mapping the visible order page.
    public async Task<ShopOrderPageResponse> PageAsync(Guid userId, Guid shopId, string? status, string? search, int page, int size, CancellationToken ct = default, DateOnly? orderDate = null)
    {
        var owner = await _shops.RequireAccessAsync(userId, shopId, ct);
        if (page < 1 || (long)(page - 1) * size > int.MaxValue) throw AppException.BadRequest("Invalid page number.");
        if (size is < 1 or > 30) throw AppException.BadRequest("Page size must be between 1 and 30.");
        status = string.IsNullOrEmpty(status) || status == "All" ? null : status;
        if (status != null && (!Enum.TryParse<OrderStatus>(status, out var parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != status))
            throw AppException.BadRequest("Invalid order status.");
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (search?.Length > 64) throw AppException.BadRequest("Search by the exact order ID or order code.");
        if (search is { Length: >= 1 and <= 6 } && search.All(char.IsAsciiDigit))
        {
            search = search.PadLeft(6, '0');
            orderDate ??= OrderBusinessDate.FromUtc(Now);
        }
        var result = await _shops.PageAsync(shopId, status, search, page, size, ct, orderDate);
        return new ShopOrderPageResponse
        {
            Page = PagedResult<ShopOrderResponse>.Create(result.Items.Select(x => Map(x, owner)).ToList(),
                page, size, result.TotalItems),
            StatusCounts = result.Counts
        };
    }
    // Reconcile payment state and return detail only to an authorized shop operator.
    public async Task<ShopOrderResponse> GetAsync(Guid userId, Guid shopId, Guid orderId, CancellationToken ct = default)
    {
        var owner = await _shops.RequireAccessAsync(userId, shopId, ct);
        var submitted = await _shops.GetAsync(shopId, orderId, ct);
        if (submitted == null || !ShopOrderVisibility.IsVisible(submitted))
            throw AppException.NotFound("Order not found in this shop.");
        await _payments.ReconcileOrderAsync(orderId, false, ct);
        return Map(await _shops.GetAsync(shopId, orderId, ct) ?? throw AppException.NotFound(), owner);
    }
    // Validate and commit a shop state transition with payment checks, audit history and job updates.
    public async Task<ShopOrderResponse> ActionAsync(Guid userId, Guid shopId, Guid orderId, string action, string? reason = null, CancellationToken ct = default, bool cashReceived = false)
    {
        await GetAsync(userId, shopId, orderId, ct);
        string? confirmationJobId = null;
        await _orders.WithOrderLockAsync(orderId, async () =>
        {
            await _shops.RequireAccessAsync(userId, shopId, ct);
            var order = await _shops.GetAsync(shopId, orderId, ct) ?? throw AppException.NotFound();
            confirmationJobId = order.ConfirmationExpirationJobId;
            var target = action switch
            {
                "confirm" => OrderStatus.Confirmed, "reject" => OrderStatus.Rejected,
                "Preparing" => OrderStatus.Preparing, "ReadyForPickup" => OrderStatus.ReadyForPickup,
                "complete" => OrderStatus.Completed, _ => throw AppException.BadRequest("Invalid order operation.")
            };
            if (order.Status == target) return true;
            if (order.HardDeadlineAt == null || order.HardDeadlineAt <= Now || order.NoShow)
                throw AppException.Conflict("The pickup deadline has passed.");
            var initial = CheckoutOrderMapper.RequiredPayment(order);
            if (action == "reject")
            {
                if (order.Status != OrderStatus.Pending) throw AppException.Conflict("Only Pending orders can be rejected.");
                if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) throw AppException.BadRequest("A rejection reason of 1–1000 characters is required.");
                await CloseUnfulfilledAsync(order, userId, "Shop rejected the order: " + reason.Trim(), ct);
                order.CancelReason = reason.Trim(); order.CancelledBy = userId; order.CancelledAt = Now;
            }
            else
            {
                if (initial?.Status != PaymentStatus.Succeeded || order.PaymentReviewRequired)
                    throw AppException.Conflict("Required payment/deposit must be verified before processing this order.");
                var expected = action switch { "confirm" => OrderStatus.Pending, "Preparing" => OrderStatus.Confirmed,
                    "ReadyForPickup" => OrderStatus.Preparing, _ => OrderStatus.ReadyForPickup };
                if (order.Status != expected) throw AppException.Conflict($"This operation requires {expected} status.");
                if (action == "confirm" && ConfirmationDeadline(order) <= Now)
                    throw AppException.Conflict("The five-minute confirmation window has expired.");
                if (action == "complete" && cashReceived) await CollectCashAsync(order, ct);
                if (action == "complete" && order.Payments.Where(x => x.Status == PaymentStatus.Succeeded).Sum(x => x.Amount) < order.TotalAmount)
                    throw AppException.Conflict("Verify the remaining payment before handing over and completing this order.");
                if (action == "confirm")
                {
                    order.ConfirmedAt = Now;
                    // Schedule within the transaction; a scheduling failure rolls back confirmation.
                    _noShowScheduler.Schedule(order.Id, order.HardDeadlineAt!.Value);
                }
                if (action == "ReadyForPickup") order.ReadyAt = Now;
                if (action == "complete") { order.CompletedAt = Now; order.RemainingAmount = 0; order.IsLateArrival = order.SoftDeadlineAt < Now; }
            }
            ChangeStatus(order, target, await _shops.ActorAsync(userId, shopId, ct), action == "reject" ? reason! : action == "complete" && cashReceived ? "Completed after receiving the remaining balance in cash." : "Shop order operation: " + action);
            await _orders.SaveAsync(ct);
            if (target == OrderStatus.Completed) await _eventPublisher.QueueCompletedAsync(order, ct);
            return true;
        }, ct);
        // Cancel the confirmation timeout only after the shop action has committed.
        if (confirmationJobId != null && action is "confirm" or "reject")
        {
            try { _confirmationScheduler.Delete(confirmationJobId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete confirmation job {JobId} for order {OrderId}.", confirmationJobId, orderId); }
        }
        if (action == "reject") await _stock.SyncAsync(orderId, ct);
        await _orderEvents.NotifyAsync(shopId, orderId, action, ct);
        return await GetAsync(userId, shopId, orderId, ct);
    }
    // Record actual cash receipt atomically with handover; no online link is created for cash.
    private async Task CollectCashAsync(Order order, CancellationToken ct)
    {
        if (order.PaymentMethod != PaymentMethod.PayAtShop)
            throw AppException.Conflict("Cash collection is available only for PayAtShop orders.");
        var payment = CheckoutOrderMapper.RemainingPayment(order);
        if (payment?.Status == PaymentStatus.Succeeded)
            throw AppException.Conflict("The remaining balance has already been paid. Complete without collecting cash again.");
        var amount = order.RemainingAmount;
        if (amount <= 0 || order.Payments.Where(x => x.Status == PaymentStatus.Succeeded).Sum(x => x.Amount) + amount != order.TotalAmount)
            throw AppException.Conflict("The recorded remaining balance needs review before cash collection.");
        if (order.Payments.Any(x => x.PaymentType == PaymentType.Remaining && CheckoutOrderMapper.Collected(x) > 0))
            throw AppException.Conflict("An online amount has already been collected. Review the payment before accepting cash.");
        if (payment != null)
        {
            if (payment.GatewayCreationAttempted)
            {
                if (!_gateway.IsConfigured(order.Shop.PaymentConfig))
                    throw AppException.ServiceUnavailable("The existing online payment must be closed before accepting cash.");
                var closed = await _gateway.CancelAsync(payment, order.Shop.PaymentConfig!, ct);
                if (closed.AmountPaid > 0 || closed.Status == "PAID")
                    throw AppException.Conflict("Online payment was received. Refresh the order before accepting cash.");
                if (closed.Status is not ("CANCELLED" or "EXPIRED" or "NOT_FOUND"))
                    throw AppException.Conflict("The online payment request has not been closed. Retry before accepting cash.");
            }
        }
        else
        {
            payment = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, PaymentType = PaymentType.Remaining, CreatedAt = Now };
            order.Payments.Add(payment);
            _shops.AddPayment(payment);
        }
        // Reuse the selected unpaid attempt as the cash receipt while retaining earlier attempt history.
        payment.Method = PaymentMethod.Cash;
        payment.Amount = amount;
        payment.CollectedAmount = amount;
        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = Now;
        payment.UpdatedAt = Now;
        payment.GatewayCheckoutUrl = null;
        payment.GatewayQrCode = null;
    }

    // Create or reuse the recorded PayAtShop balance payment and its provider QR link.
    public async Task<ShopOrderResponse> RemainingPaymentAsync(Guid userId, Guid shopId, Guid orderId, CancellationToken ct = default)
    {
        await GetAsync(userId, shopId, orderId, ct);
        await _orders.WithOrderLockAsync(orderId, async () =>
        {
            await _shops.RequireAccessAsync(userId, shopId, ct);
            var order = await _shops.GetAsync(shopId, orderId, ct) ?? throw AppException.NotFound();
            if (order.PaymentMethod != PaymentMethod.PayAtShop || order.Status is not (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup)
                || order.HardDeadlineAt <= Now || order.RemainingAmount <= 0 || order.PaymentReviewRequired
                || CheckoutOrderMapper.RequiredPayment(order)?.Status != PaymentStatus.Succeeded)
                throw AppException.Conflict("This order is not eligible for remaining payment.");
            if (!_gateway.IsConfigured(order.Shop.PaymentConfig)) throw AppException.ServiceUnavailable("Shop payment configuration is unavailable.");
            var payment = CheckoutOrderMapper.RemainingPayment(order);
            if (payment?.Status == PaymentStatus.Succeeded) return true;
            if (order.Payments.Any(x => x.PaymentType == PaymentType.Remaining && CheckoutOrderMapper.Collected(x) > 0))
                throw AppException.Conflict("An online amount has already been collected. Review the payment before creating another QR.");
            // Retain the old provider code so delayed webhooks can still resolve their original attempt.
            if (payment != null && payment.Status != PaymentStatus.Pending)
            {
                var closed = await _gateway.GetAsync(payment, order.Shop.PaymentConfig!, ct);
                if (closed.AmountPaid > 0 || closed.Status == "PAID")
                    throw AppException.Conflict("Payment was received. Refresh the order before creating another QR.");
                if (closed.Status is not ("CANCELLED" or "EXPIRED" or "NOT_FOUND"))
                    throw AppException.Conflict("The previous payment request is not confirmed closed. Please retry.");
                payment = null;
            }
            if (payment == null)
            {
                payment = new Payment
                {
                    Id = Guid.NewGuid(), OrderId = order.Id, PaymentType = PaymentType.Remaining, Method = PaymentMethod.OnlinePayment,
                    Amount = order.RemainingAmount, Status = PaymentStatus.Pending, GatewayOrderCode = Random.Shared.NextInt64(1, 9007199254740991),
                    GatewayTransactionId = _gateway is DevelopmentCheckoutGateway ? DevelopmentCheckoutGateway.Prefix + "PENDING" : null,
                    CreatedAt = Now, UpdatedAt = Now
                };
                order.Payments.Add(payment);
                _shops.AddPayment(payment);
            }
            payment.GatewayCreationAttempted = true;
            AddActivity(order, await _shops.ActorAsync(userId, shopId, ct), "Requested remaining payment QR/link for the recorded balance.");
            await _orders.SaveAsync(ct);
            return true;
        }, ct);
        await _payments.ReconcileOrderAsync(orderId, true, ct);
        var result = await GetAsync(userId, shopId, orderId, ct);
        if (result.Payments.Any(x => x.Type == "Remaining" && x.Status == "Pending" && x.CheckoutUrl == null))
            throw AppException.ServiceUnavailable("The payment QR is not available yet. Retry to recover the same payment request.");
        return result;
    }
    // Derive the shop's five-minute confirmation deadline from successful initial payment.
    private static DateTime? ConfirmationDeadline(Order order)
    {
        var p = CheckoutOrderMapper.RequiredPayment(order);
        return p?.Status == PaymentStatus.Succeeded ? (p.PaidAt ?? order.CreatedAt).AddMinutes(5) : null;
    }
    // Record a staff operation with an immutable role and display-name snapshot.
    private void AddActivity(Order order, ShopOrderActor actor, string note)
    {
        _shops.AddActivity(new StaffActivityLog
        {
            Id = Guid.NewGuid(), ShopId = order.ShopId, OrderId = order.Id, ActionBy = actor.Id,
            ActorNameSnapshot = actor.Name, ActorRoleSnapshot = actor.Role,
            Action = order.Status, Note = note, CreatedAt = Now
        });
    }
    // Change order state and capture the acting operator in history and staff activity.
    private void ChangeStatus(Order order, OrderStatus next, ShopOrderActor? actor, string note)
    {
        var previous = order.Status;
        order.Status = next; order.UpdatedAt = Now;
        _orders.AddHistory(new OrderStatusHistory
        {
            Id = Guid.NewGuid(), OrderId = order.Id, FromStatus = previous, ToStatus = next,
            ChangedBy = actor?.Id, ActorNameSnapshot = actor?.Name ?? "System", ActorRoleSnapshot = actor?.Role ?? "System",
            Note = note, CreatedAt = Now
        });
        if (actor != null) AddActivity(order, actor, note);
    }
    // Display saved actor identities and resolve legacy entries when snapshots are missing.
    private static ShopOrderHistoryResponse MapHistory(Order order, OrderStatusHistory history)
    {
        var shopAction = history.ToStatus is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup or OrderStatus.Completed or OrderStatus.Rejected;
        var role = history.ActorRoleSnapshot ?? (history.ChangedBy == null ? "System" : !shopAction ? "Customer"
            : history.ChangedBy == order.Shop.OwnerUserId ? "StoreOwner" : "Staff");
        var staffName = role == "Staff" ? order.Shop.StaffMembers.FirstOrDefault(x => x.UserId == history.ChangedBy)?.DisplayName : null;
        var name = history.ActorNameSnapshot ?? (role == "System" ? "System"
            : !string.IsNullOrWhiteSpace(staffName) ? staffName : history.ChangedByUser?.FullName);
        return new ShopOrderHistoryResponse
        {
            FromStatus = history.FromStatus?.ToString(),
            ToStatus = history.ToStatus.ToString(),
            Note = history.Note,
            CreatedAt = history.CreatedAt,
            ActorId = history.ChangedBy,
            ActorRole = role,
            ActorName = name ?? (role == "Staff" ? "Staff member" : role == "StoreOwner" ? "Shop owner" : "Customer")
        };
    }
    // Reject and confirmation expiry release stock and record a refund request without transferring money.
    private async Task CloseUnfulfilledAsync(Order order, Guid actor, string reason, CancellationToken ct)
    {
        await CloseLinksAsync(order, ct);
        await _orders.ReleaseCancelledInventoryAsync(order, ct);
        order.RemainingAmount = 0;
        EnsureRefund(order, actor, reason);
    }

    // Record an outstanding full-refund request for collected funds without executing a transfer.
    private void EnsureRefund(Order order, Guid actor, string reason)
    {
        var amount = order.Payments.Sum(CheckoutOrderMapper.Collected);
        if (amount <= 0) return;
        var existing = order.Refunds.SingleOrDefault(x => x.RefundType == RefundType.ShopCancellation);
        if (existing != null)
        {
            if (amount > existing.RequestedAmount)
            {
                existing.RequestedAmount = amount; existing.UpdatedAt = Now; order.PaymentReviewRequired = true;
                if (existing.Status == RefundStatus.Transferred) existing.Status = RefundStatus.AwaitingTransfer;
            }
            return;
        }
        var refund = new Refund { Id = Guid.NewGuid(), OrderId = order.Id, RefundType = RefundType.ShopCancellation,
            Status = RefundStatus.AwaitingCustomerDetails, RequestedAmount = amount, InitiatedBy = actor,
            Reason = reason, CreatedAt = Now, UpdatedAt = Now };
        order.Refunds.Add(refund);
        _shops.AddRefund(refund);
    }
    // Close unpaid provider links and preserve any verified money collected during closure.
    private async Task CloseLinksAsync(Order order, CancellationToken ct)
    {
        foreach (var p in order.Payments.Where(x => x.Status == PaymentStatus.Pending))
        {
            if (p.GatewayCreationAttempted)
            {
                if (!_gateway.IsConfigured(order.Shop.PaymentConfig)) throw AppException.ServiceUnavailable("Payment must be reconciled before closing this order.");
                var status = await _gateway.CancelAsync(p, order.Shop.PaymentConfig!, ct);
                p.CollectedAmount = Math.Max(p.CollectedAmount, status.AmountPaid);
                if (status.Status == "PAID" && status.AmountPaid >= p.Amount) { p.Status = PaymentStatus.Succeeded; p.PaidAt = Now; }
                else if (status.Status is "CANCELLED" or "EXPIRED" or "NOT_FOUND") p.Status = PaymentStatus.Cancelled;
                else throw AppException.Conflict("Payment closure is not confirmed. Please retry.");
            }
            else p.Status = PaymentStatus.Cancelled;
            p.GatewayCheckoutUrl = null; p.UpdatedAt = Now;
        }
    }
    // Hangfire enforces the original payment-based five-minute confirmation deadline for one order.
    public async Task ExpireConfirmationAsync(Guid orderId, CancellationToken ct = default)
    {
        var shopId = await _orders.WithOrderLockAsync(orderId, async () =>
        {
            var order = await _shops.GetAsync((await _orders.GetOrderAsync(orderId, null, ct))?.ShopId ?? Guid.Empty, orderId, ct);
            if (order == null || order.Status != OrderStatus.Pending) return (Guid?)null;
            var deadline = ConfirmationDeadline(order);
            if (!deadline.HasValue) return null;
            if (deadline > Now) throw new InvalidOperationException("The shop confirmation job ran before its deadline.");
            await CloseUnfulfilledAsync(order, order.Shop.OwnerUserId, "Shop did not confirm within five minutes of payment.", ct);
            order.ExpiredAt = Now;
            ChangeStatus(order, OrderStatus.Expired, null, "Shop confirmation window expired; full refund required for collected amounts.");
            await _orders.SaveAsync(ct);
            return (Guid?)order.ShopId;
        }, ct);
        if (shopId.HasValue)
        {
            await _stock.SyncAsync(orderId, ct);
            await _orderEvents.NotifyAsync(shopId.Value, orderId, RealtimeEvents.OrderReasons.ConfirmationExpired, ct);
        }
    }

    // Commit the NoShow state and durable broker event before notifying shop and customer subscribers.
    public async Task MarkNoShowAsync(Guid orderId, CancellationToken ct = default)
    {
        var shopId = await _orders.WithOrderLockAsync(orderId, async () =>
        {
            var order = await _orders.GetOrderAsync(orderId, null, ct);
            if (order == null || order.Status is not (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup)) return (Guid?)null;
            if (order.HardDeadlineAt == null) throw new InvalidOperationException("The order has no pickup deadline.");
            if (order.HardDeadlineAt > Now) throw new InvalidOperationException("The NoShow job ran before its deadline.");
            order.NoShow = true;
            ChangeStatus(order, OrderStatus.NoShow, null, "Pickup deadline passed before collection.");
            // The outbox and status commit together; retries cannot create a second business event.
            await _eventPublisher.QueueNoShowAsync(order, ct);
            await _orders.SaveAsync(ct);
            return (Guid?)order.ShopId;
        }, ct);
        // Repeated or concurrent jobs that did not change the order must not send another event.
        if (shopId.HasValue)
        {
            try { await _orderEvents.NotifyAsync(shopId.Value, orderId, RealtimeEvents.OrderReasons.NoShow, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { _logger.LogWarning(ex, "Could not notify subscribers after NoShow for order {OrderId}.", orderId); }
        }
    }

    // Map recorded refund amounts, destination verification and transfer progress.
    private static OrderRefundResponse MapRefund(Refund f) => new()
    {
        Id = f.Id,
        Status = f.Status.ToString(),
        Amount = f.RequestedAmount,
        Reason = f.Reason,
        BankName = f.ReceiverBankName,
        AccountNumber = f.ReceiverAccountNumber,
        AccountHolder = f.ReceiverAccountHolder,
        ConfirmationExpiresAt = f.VerificationExpiresAt,
        DestinationConfirmedAt = f.DestinationConfirmedAt,
        TransferReference = f.TransferReference,
        TransferredAmount = f.TransferredAmount
    };
    // Build shop detail from recorded order data, payments, audit history and refund state.
    private ShopOrderResponse Map(Order order, bool owner) => new()
    {
        Order = CheckoutOrderMapper.Map(order, now: Now),
        CustomerName = order.Customer.FullName,
        CustomerPhone = order.Customer.Phone,
        ConfirmationDeadlineAt = order.Status == OrderStatus.Pending ? ConfirmationDeadline(order) : null,
        IsOwner = owner,
        Payments = order.Payments.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new ShopOrderPaymentResponse
        {
            Id = x.Id,
            Type = x.PaymentType.ToString(),
            Status = x.Status.ToString(),
            Amount = x.Amount,
            PaidAt = x.PaidAt,
            CheckoutUrl = x.Status == PaymentStatus.Pending && order.HardDeadlineAt > Now
                && order.Status is not (OrderStatus.Rejected or OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.NoShow or OrderStatus.Completed)
                ? x.GatewayCheckoutUrl
                : null,
            QrCode = x.GatewayQrCode,
            CollectedAmount = CheckoutOrderMapper.Collected(x),
            Method = x.Method.ToString()
        }).ToList(),
        History = order.StatusHistories.OrderBy(x => x.CreatedAt).Select(x => MapHistory(order, x)).ToList(),
        Refund = order.Refunds.Where(x => x.RefundType == RefundType.ShopCancellation).Select(MapRefund).FirstOrDefault()
    };
    // Read the customer's own refund after reconciling any overdue order processing.
    public async Task<OrderRefundResponse?> CustomerRefundAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var order = await _orders.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound();
        return (await _shops.GetAsync(order.ShopId, orderId, ct))!.Refunds.Where(x => x.RefundType == RefundType.ShopCancellation).Select(MapRefund).FirstOrDefault();
    }
    // Load a refund-bearing order with active Customer access and ownership checks.
    private async Task<(Order Order, Refund Refund)> CustomerRefundOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
    {
        var order = await _orders.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound();
        order = await _shops.GetAsync(order.ShopId, orderId, ct) ?? throw AppException.NotFound();
        var refund = order.Refunds.SingleOrDefault(x => x.RefundType == RefundType.ShopCancellation) ?? throw AppException.NotFound("No refund is due for this order.");
        return (order, refund);
    }
    // Hash an order-specific refund verification code instead of storing its plain value.
    private static string CodeHash(Guid id, string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{id:N}:{code}")));
    // Save the customer's refund bank destination and send an email verification code valid for fifteen minutes.
    public async Task<OrderRefundResponse> DestinationAsync(Guid userId, Guid orderId, RefundDestinationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BankName) || request.BankName.Length > 100 || string.IsNullOrWhiteSpace(request.AccountNumber)
            || request.AccountNumber.Length > 50 || !request.AccountNumber.All(char.IsDigit) || string.IsNullOrWhiteSpace(request.AccountHolder) || request.AccountHolder.Length > 150)
            throw AppException.BadRequest("Enter a valid bank name, account number and account holder.");
        return await _orders.WithOrderLockAsync(orderId, async () =>
        {
            var (order, refund) = await CustomerRefundOrderAsync(userId, orderId, ct);
            if (refund.Status is not (RefundStatus.AwaitingCustomerDetails or RefundStatus.PendingVerification)) throw AppException.Conflict("The refund destination can no longer be changed.");
            if (refund.VerificationExpiresAt > Now.AddMinutes(14)) throw AppException.Conflict("Please wait one minute before requesting another confirmation code.");
            var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            refund.ReceiverMethod = RefundReceiverMethod.BankAccount;
            refund.ReceiverBankName = request.BankName.Trim(); refund.ReceiverAccountNumber = request.AccountNumber.Trim(); refund.ReceiverAccountHolder = request.AccountHolder.Trim();
            refund.VerificationCodeHash = CodeHash(refund.Id, code); refund.VerificationExpiresAt = Now.AddMinutes(15);
            refund.VerificationAttempts = 0; refund.DestinationConfirmedAt = null; refund.Status = RefundStatus.PendingVerification; refund.UpdatedAt = Now;
            await _email.SendAsync(order.Customer.Email, "Confirm your SaveBite refund destination",
                $"Your refund confirmation code is {code}. It expires in 15 minutes. Bank: {refund.ReceiverBankName}. Account ending: {refund.ReceiverAccountNumber[^Math.Min(4, refund.ReceiverAccountNumber.Length)..]}", false, ct);
            await _orders.SaveAsync(ct);
            return MapRefund(refund);
        }, ct);
    }
    // Verify the unexpired destination confirmation code and keep repeated confirmation idempotent.
    public async Task<OrderRefundResponse> VerifyDestinationAsync(Guid userId, Guid orderId, string code, CancellationToken ct = default)
    {
        var result = await _orders.WithOrderLockAsync(orderId, async () =>
        {
            var (_, f) = await CustomerRefundOrderAsync(userId, orderId, ct);
            var matches = code != null && code.Length == 6 && f.VerificationCodeHash == CodeHash(f.Id, code);
            if (f.DestinationConfirmedAt != null && matches) return (Refund: MapRefund(f), Valid: true);
            if (f.Status != RefundStatus.PendingVerification || f.VerificationExpiresAt <= Now || f.VerificationAttempts >= 5)
                return (Refund: MapRefund(f), Valid: false);
            f.VerificationAttempts++;
            if (matches) { f.DestinationConfirmedAt = Now; f.Status = RefundStatus.AwaitingTransfer; f.UpdatedAt = Now; }
            await _orders.SaveAsync(ct);
            return (Refund: MapRefund(f), Valid: matches);
        }, ct);
        if (!result.Valid) throw AppException.BadRequest("The confirmation code is invalid, expired, or has reached its attempt limit.");
        return result.Refund;
    }
    // Let the shop owner record a manual refund transfer after the destination has been verified.
    public async Task<OrderRefundResponse> TransferAsync(Guid userId, Guid shopId, Guid orderId, string reference, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 150) throw AppException.BadRequest("A bank transfer reference is required.");
        return await _orders.WithOrderLockAsync(orderId, async () =>
        {
            if (!await _shops.RequireAccessAsync(userId, shopId, ct)) throw AppException.Forbidden("Only the shop owner may record a refund transfer.");
            var order = await _shops.GetAsync(shopId, orderId, ct) ?? throw AppException.NotFound();
            var f = order.Refunds.SingleOrDefault(x => x.RefundType == RefundType.ShopCancellation) ?? throw AppException.NotFound();
            if (f.Status == RefundStatus.Transferred && f.TransferReference == reference.Trim()) return MapRefund(f);
            if (f.Status != RefundStatus.AwaitingTransfer || f.DestinationConfirmedAt == null) throw AppException.Conflict("The customer must confirm the refund destination by email before transfer.");
            if (f.TransferredAmount > 0 && f.TransferReference == reference.Trim())
                throw AppException.Conflict("Use the new bank transfer reference for the additional refund balance.");
            var transferredNow = f.RequestedAmount - f.TransferredAmount;
            f.TransferredAmount = f.RequestedAmount;
            f.Status = RefundStatus.Transferred; f.TransferReference = reference.Trim(); f.TransferredAt = Now; f.ReviewedBy = userId; f.ReviewedAt = Now; f.UpdatedAt = Now;
            AddActivity(order, await _shops.ActorAsync(userId, shopId, ct), $"Recorded refund bank transfer of {transferredNow} VND: " + reference.Trim());
          
            // BR-205: When Store Owner executes refund transfer, record a negative platform-fee adjustment
            if (transferredNow > 0)
            {
                var orderFee = await _shops.GetOrderPlatformFeeAsync(order.Id, ct);
                if (orderFee != null)
                {
                    var adjAmount = -Math.Round(transferredNow * orderFee.FeeRate, 2);
                    if (adjAmount < 0)
                    {
                        var adjustment = new PlatformFeeAdjustment
                        {
                            Id = Guid.NewGuid(),
                            ShopId = order.ShopId,
                            OrderFeeId = orderFee.Id,
                            RefundId = f.Id,
                            StatementId = null,
                            Amount = adjAmount,
                            Reason = $"Fee reduction via refund {transferredNow:N0} VND.",
                            CreatedAt = Now
                        };
                        // If statement is already issued for this fee and is still unpaid (Issued),
                        // directly credit the adjustment to that statement so the shop is relieved immediately!
                        if (orderFee.Statement != null && orderFee.Statement.Status == PlatformFeeStatementStatus.Issued)
                        {
                            adjustment.StatementId = orderFee.Statement.Id;
                            orderFee.Statement.TotalFeeAmount = Math.Max(0, orderFee.Statement.TotalFeeAmount + adjAmount);
                            orderFee.Statement.UpdatedAt = Now;
                            if (orderFee.Statement.TotalFeeAmount == 0)
                            {
                                orderFee.Statement.Status = PlatformFeeStatementStatus.Paid;
                                orderFee.Statement.PaidAt = Now;
                                orderFee.Statement.DueAt = null;
                            }
                        }
                        _shops.AddFeeAdjustment(adjustment);
                    }
                }
            }

            await _orders.SaveAsync(ct);
            return MapRefund(f);
        }, ct);
    }
}
