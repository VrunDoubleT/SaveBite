using SaveBite.Backend.Configurations;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

// Keep refund reads separate from commands; all commands share the customer/order transaction lock.
public class RefundService : IRefundService
{
    private readonly AppDbContext _db;
    private readonly IRefundRepository _refunds;
    private readonly INotificationQueue _notifications;
    private readonly ITrustNotificationService _trustNotifications;
    private readonly ICheckoutRepository _orders;
    private readonly IShopOrderRepository _shops;
    private readonly ITrustOrderRepository _trust;
    private readonly IEmailService _email;
    private readonly TimeProvider _clock;
    private readonly IConfiguration _config;
    private readonly IRefundDeadlineScheduler _deadlines;
    private readonly ILogger<RefundService> _logger;
    private readonly IServiceScopeFactory _scopes;

    public RefundService(IRefundRepository refunds, ICheckoutRepository orders, IShopOrderRepository shops,
        ITrustOrderRepository trust, IEmailService email, TimeProvider clock, IConfiguration config, IRefundDeadlineScheduler deadlines, ILogger<RefundService> logger, INotificationQueue notifications, ITrustNotificationService trustNotifications, AppDbContext db, IServiceScopeFactory scopes)
    {
        _notifications = notifications; _trustNotifications = trustNotifications;
        _refunds = refunds; _orders = orders; _shops = shops; _trust = trust;
        _db = db; _scopes = scopes;
        _email = email; _clock = clock; _config = config; _deadlines = deadlines; _logger = logger;
    }
    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    // Read the customer's action-required badge without changing refund state.
    public async Task<int> PendingDetailsCountAsync(Guid customerId, CancellationToken ct)
    {
        await RequireCustomerHistoryAsync(customerId, ct);
        return await _refunds.PendingDetailsCountAsync(customerId, ct);
    }

    // Count confirmed, unexpired refunds that currently require owner review or transfer.
    public async Task<int> ShopPendingCountAsync(Guid ownerId, CancellationToken ct)
    {
        var shopId = await _shops.OwnerShopAsync(ownerId, ct);
        return await _refunds.ShopPendingCountAsync(shopId, ct);
    }

    // Scope list access before retrieving any records; no status changes occur on a GET.
    public async Task<PagedResult<RefundResponse>> PageAsync(Guid userId, bool forShop, string? status, string? type, int page, int size, CancellationToken ct)
    {
        if (size is < 1 or > 50 || page < 1 || (long)(page - 1) * size > int.MaxValue)
            throw AppException.BadRequest("Invalid pagination.");
        Guid? shopId = forShop ? await _shops.OwnerShopAsync(userId, ct) : null;
        if (!forShop) await RequireCustomerHistoryAsync(userId, ct);
        var result = await _refunds.PageAsync(userId, shopId, Parse<RefundStatus>(status), Parse<RefundType>(type), page, size, ct);
        return PagedResult<RefundResponse>.Create(result.Items.Select(Map).ToList(), page, size, result.Total);
    }

    // Keep shop refund operations available inside order details without exposing unconfirmed customer requests.
    public async Task<IReadOnlyList<RefundResponse>> ShopForOrderAsync(Guid ownerId, Guid orderId, CancellationToken ct)
    {
        var shopId = await _shops.OwnerShopAsync(ownerId, ct);
        _ = await _shops.GetAsync(shopId, orderId, ct) ?? throw AppException.NotFound();
        return (await _refunds.ForOrderAsync(orderId, ct)).Where(f => f.RefundType == RefundType.ShopCancellation
            || (f.Order.Status == OrderStatus.Completed && f.DestinationConfirmedAt != null)).Select(Map).ToList();
    }

    // Read all refunds of one owned order without triggering reconciliation or writes.
    public async Task<IReadOnlyList<RefundResponse>> ForOrderAsync(Guid customerId, Guid orderId, CancellationToken ct)
    {
        _ = await _orders.GetOrderForHistoryAsync(orderId, customerId, ct) ?? throw AppException.NotFound("Order not found.");
        return (await _refunds.ForOrderAsync(orderId, ct)).Select(Map).ToList();
    }

    // Return only recorded refund information and immutable audit history.
    public async Task<RefundResponse> GetAsync(Guid userId, Guid refundId, bool forShop, CancellationToken ct)
    {
        var f = await LoadAsync(refundId, false, ct);
        await AuthorizeAsync(userId, forShop, f, ct, readOnly: true);
        return Map(f);
    }

    // Create one item-based refund request within six hours of completion; never accept a client amount.
    public async Task<RefundResponse> RequestAsync(Guid userId, Guid orderId, CreateRefundRequest request, CancellationToken ct)
    {
        var result = await _orders.WithOrderLockAsync(orderId, async () =>
        {
            _ = await _orders.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound("Order not found.");
            var order = await _refunds.OrderAsync(orderId, ct) ?? throw AppException.NotFound();
            RefundRules.ValidateRequest(order, Now);
            if (order.Refunds.Any(x => x.RefundType == RefundType.CustomerRequest) || await _refunds.HasCustomerRequestAsync(orderId, ct)) throw AppException.Conflict("Only one refund request may be submitted per order, including withdrawn, rejected or expired requests.");
            if (order.Refunds.Any(x => RefundRules.Active(x.Status))) throw AppException.Conflict("An active refund already exists for this order.");
            var selected = BuildRequestedItems(order, request.Items);
            var amount = selected.Sum(x => x.Amount);
            if (amount > RefundRules.EligibleAmount(order)) throw AppException.Conflict("The selected amount exceeds the refundable paid balance.");
            if (amount <= 0) throw AppException.Conflict("This order has no refundable paid balance.");
            var f = new Refund
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Order = order,
                RefundType = RefundType.CustomerRequest,
                InitiatedBy = userId,
                Reason = "Customer requested a refund for selected items. See each item for its reason.",
                RequestedAmount = amount,
                Status = RefundStatus.AwaitingCustomerDetails,
                CreatedAt = Now,
                UpdatedAt = Now
            };
            f.CustomerDetailsDeadlineAt = f.CreatedAt.AddHours(TimingConfiguration.Refund.CustomerDetailsHours);
            f.DeadlineJobId = _deadlines.Schedule(f.Id, f.CustomerDetailsDeadlineAt.Value);
            foreach (var item in selected)
            {
                item.RefundId = f.Id;
                f.Items.Add(item);
            }
            _refunds.Add(f);
            _refunds.Add(new OrderRefundRequest { OrderId = order.Id, RefundId = f.Id, CreatedAt = f.CreatedAt });
            var selectedByOrderItem = selected.ToDictionary(x => x.OrderItemId);
            foreach (var selection in request.Items)
            {
                var refundItem = selectedByOrderItem[selection.OrderItemId];
                foreach (var url in selection.EvidenceUrls) AddEvidence(f, userId, url, RefundEvidenceType.RequestEvidence, refundItem.Id);
            }
            Audit(f, null, userId, "Customer", order.Customer.FullName, "Requested", f.Reason);
            await _orders.SaveAsync(ct);
            await _notifications.EnqueueAsync(NotificationTarget.User(order.CustomerId),
                RefundNotificationCommands.DestinationRequired(f.Id, order.OrderCode, f.CustomerDetailsDeadlineAt.Value), ct);
            return Map(f);
        }, ct);
        return result;
    }



    // Store an unconfirmed bank destination and send a new email code; this does not start owner review.
    public Task<RefundResponse> DestinationAsync(Guid userId, Guid refundId, RefundBankRequest request, CancellationToken ct)
    {
        RefundRules.ValidateBank(request);
        return CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, false, f, ct);
            if (f.Status != RefundStatus.AwaitingCustomerDetails || f.DestinationConfirmedAt != null)
                throw AppException.Conflict("The destination can no longer be changed.");
            RefundRules.ValidateCustomerDetailsDeadline(f, Now);
            if (f.VerificationIssuedAt > Now.AddMinutes(-TimingConfiguration.Refund.EmailCodeResendMinutes)) throw AppException.Conflict("Wait one minute before requesting another code.");
            var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            if (request.ReceiverMethod is not ("BankAccount" or "QrImage")) throw AppException.BadRequest("Choose bank details or a bank QR image.");
            if (request.ReceiverMethod == "QrImage") ValidateUrl(request.QrImageUrl);
            f.ReceiverMethod = request.ReceiverMethod == "QrImage" ? RefundReceiverMethod.QrImage : RefundReceiverMethod.BankAccount;
            f.ReceiverQrImageUrl = f.ReceiverMethod == RefundReceiverMethod.QrImage ? request.QrImageUrl : null;
            f.ReceiverBankBin = request.BankBin; f.ReceiverBankName = request.BankName.Trim();
            f.ReceiverAccountNumber = request.AccountNumber; f.ReceiverAccountHolder = request.AccountHolder.Trim();
            f.VerificationCodeHash = CodeHash(f.Id, code); f.VerificationIssuedAt = Now;
            f.VerificationExpiresAt = Now.AddMinutes(TimingConfiguration.Refund.EmailCodeLifetimeMinutes); f.VerificationAttempts = 0;
            Audit(f, f.Status, userId, "Customer", f.Order.Customer.FullName, "DestinationProvided", "Bank destination awaiting email confirmation.");
            await _email.SendAsync(f.Order.Customer.Email, "Confirm your SaveBite refund destination",
                $"Your refund confirmation code is {code}. It expires in 15 minutes. Bank: {f.ReceiverBankName}. Account ending: {f.ReceiverAccountNumber[^Math.Min(4, f.ReceiverAccountNumber.Length)..]}", false, ct);
        }, ct);
    }

    // Persist failed attempts too; a repeated successful confirmation has no further business effect.
    public async Task<RefundResponse> VerifyAsync(Guid userId, Guid refundId, string code, CancellationToken ct)
    {
        var valid = false;
        var result = await CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, false, f, ct);
            if (f.Status == RefundStatus.Rejected) throw AppException.Conflict("This refund has been closed.");
            if (f.Status == RefundStatus.AwaitingCustomerDetails) RefundRules.ValidateCustomerDetailsDeadline(f, Now);
            var matches = MatchesCode(f, code);
            if (f.DestinationConfirmedAt.HasValue && matches) { valid = true; return; }
            if (f.Status != RefundStatus.AwaitingCustomerDetails || !f.VerificationExpiresAt.HasValue
                || f.VerificationExpiresAt <= Now || f.VerificationAttempts >= 5) return;
            f.VerificationAttempts++;
            if (!matches) return;
            RefundRules.RequireDestinationFields(f);
            var before = f.Status;
            f.DestinationConfirmedAt = Now; f.ReviewDeadlineAt = Now.AddHours(RefundRules.ShopProcessingHours);
            if (f.RefundType == RefundType.CustomerRequest)
            {
                if (f.Order.Status != OrderStatus.Completed) throw AppException.Conflict("Only completed orders can be reviewed.");
                f.Status = RefundStatus.PendingVerification; f.PendingVerificationAt = Now; f.ReviewDeadlineAt = Now.AddHours(RefundRules.ShopProcessingHours);
            }
            else f.Status = RefundStatus.AwaitingTransfer;
            Audit(f, before, userId, "Customer", f.Order.Customer.FullName, "DestinationConfirmed", "Bank destination confirmed through email.");
            valid = true;
        }, ct);
        if (!valid) throw AppException.BadRequest("The confirmation code is invalid, expired, or has reached its attempt limit.");
        return result;
    }

    // Only voluntary customer requests may be withdrawn; mandatory refunds remain payable until resolved.
    public Task<RefundResponse> WithdrawAsync(Guid userId, Guid refundId, CancellationToken ct)
        => CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, false, f, ct);
            if (f.RefundType != RefundType.CustomerRequest)
                throw AppException.Conflict("Mandatory shop-cancellation refunds cannot be withdrawn.");
            if (f.Status == RefundStatus.Withdrawn) return;
            if (f.Status == RefundStatus.AwaitingCustomerDetails) RefundRules.ValidateCustomerDetailsDeadline(f, Now);
            if (f.Status == RefundStatus.PendingVerification) RefundRules.ValidateDeadline(f, Now);
            if (f.Status is not (RefundStatus.AwaitingCustomerDetails or RefundStatus.PendingVerification))
                throw AppException.Conflict("This refund can no longer be withdrawn.");
            var before = f.Status; f.Status = RefundStatus.Withdrawn;
            Audit(f, before, userId, "Customer", f.Order.Customer.FullName, "Withdrawn", "Customer withdrew the refund request.");
        }, ct);

    // Only customer requests require a decision; mandatory shop-cancellation refunds cannot be rejected.
    public Task<RefundResponse> ReviewAsync(Guid userId, Guid refundId, bool approve, string? reason, CancellationToken ct)
        => CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, true, f, ct);
            if (f.RefundType != RefundType.CustomerRequest || f.Order.Status != OrderStatus.Completed)
                throw AppException.Conflict("Only post-completion customer requests can be reviewed.");
            if (approve && f.Status == RefundStatus.AwaitingTransfer) return;
            if (!approve && f.Status == RefundStatus.Rejected && f.RejectReason == reason?.Trim()) return;
            if (f.Status != RefundStatus.PendingVerification || f.DestinationConfirmedAt == null)
                throw AppException.Conflict("Review requires PendingVerification and a confirmed bank destination.");
            RefundRules.ValidateDeadline(f, Now);
            if (!approve) ValidateText(reason, 1000, "A rejection reason is required.");
            if (approve && RefundRules.EligibleAmount(f.Order) < f.RequestedAmount)
                throw AppException.Conflict("The refundable paid balance is below the requested amount; refresh the refund request.");
            var before = f.Status;
            f.Status = approve ? RefundStatus.AwaitingTransfer : RefundStatus.Rejected;
            f.ReviewedBy = userId; f.ReviewedAt = Now; f.RejectReason = approve ? null : reason!.Trim();
            Audit(f, before, userId, "StoreOwner", (await _refunds.UserAsync(userId, ct)).FullName,
                approve ? "Approved" : "Rejected", approve ? $"Selected refund amount of {f.RequestedAmount} VND approved." : f.RejectReason);
        }, ct);

    // Record the approved manual transfer and reclaim only the refunded order value exactly once in the same transaction.
    public Task<RefundResponse> TransferAsync(Guid userId, Guid refundId, RefundTransferRequest request, CancellationToken ct)
    {
        ValidateText(request.TransferReference, 150, "A transfer reference is required.");
        ValidateUrl(request.ProofUrl);
        return CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, true, f, ct);
            if (f.Status == RefundStatus.Transferred && f.TransferReference == request.TransferReference.Trim()) return;
            var now = Now;
            var transferredAt = RefundRules.ValidateTransfer(f, request.TransferredAt ?? now, now);
            var before = f.Status;
            var transferAmount = f.RequestedAmount - f.TransferredAmount;
            f.Order.RefundedAmount += transferAmount;
            f.Status = RefundStatus.Transferred; f.TransferredAmount = f.RequestedAmount;
            f.TransferReference = request.TransferReference.Trim(); f.TransferredAt = transferredAt;
            if (request.ProofUrl != null) AddEvidence(f, userId, request.ProofUrl, RefundEvidenceType.TransferProof);
            Audit(f, before, userId, "StoreOwner", (await _refunds.UserAsync(userId, ct)).FullName,
                "Transferred", $"Manual transfer of the full remaining {transferAmount} VND recorded; total transferred {f.TransferredAmount} VND.", request.ProofUrl);
            f.FeeAdjustedAt = null; f.AccumulatedValueReclaimedAt = null;
            LifecycleOutbox.RefundTransferred(_db, f, now, transferAmount);
        }, ct);
    }

    // Generate instructions from persisted approved amount and confirmed destination, never client overrides.
    public async Task<RefundQrResponse> GenerateQrAsync(Guid userId, Guid refundId, CancellationToken ct)
    {
        var f = await LoadAsync(refundId, false, ct);
        await AuthorizeAsync(userId, true, f, ct);
        RefundRules.RequireTransferable(f, Now);
        RefundRules.RequireDestinationFields(f, requireBankBin: true);
        var content = "SB RF " + f.Id.ToString("N")[..16].ToUpperInvariant();
        return new RefundQrResponse
        {
            RefundId = f.Id,
            Amount = f.RequestedAmount - f.TransferredAmount,
            BankBin = f.ReceiverBankBin!,
            AccountNumber = f.ReceiverAccountNumber!,
            AccountHolder = f.ReceiverAccountHolder!,
            TransferContent = content,
            ImageUrl = $"https://img.vietqr.io/image/{f.ReceiverBankBin}-{f.ReceiverAccountNumber}-compact2.png?amount={(f.RequestedAmount - f.TransferredAmount).ToString("0.##", CultureInfo.InvariantCulture)}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString(f.ReceiverAccountHolder!)}"
        };
    }

    // Compatibility lookup is restricted to the customer's system-required refund and remains read-only.
    public async Task<Guid?> SystemRefundIdAsync(Guid customerId, Guid orderId, CancellationToken ct, bool forShop = false)
    {
        if (forShop)
        {
            var shopId = await _shops.OwnerShopAsync(customerId, ct);
            _ = await _shops.GetAsync(shopId, orderId, ct) ?? throw AppException.NotFound();
        }
        else _ = await _orders.GetOrderForHistoryAsync(orderId, customerId, ct) ?? throw AppException.NotFound();
        return await _refunds.SystemRefundIdAsync(orderId, ct);
    }

    // Recover missing customer-confirmation and shop-processing schedules after restart.
    public Task<RefundResponse> EnsureDeadlineScheduledAsync(Guid refundId, CancellationToken ct)
        => CommandAsync(refundId, _ => Task.CompletedTask, ct);

    // Close unresolved refunds after the relevant customer or shop deadline; stale jobs are ignored.
    public async Task<RefundResponse?> ExpireDeadlineAsync(Guid refundId, DateTime deadline, CancellationToken ct)
    {
        if (await _refunds.GetAsync(refundId, false, ct) == null) return null;
        var notify = false;
        var result = await CommandAsync(refundId, f =>
        {
            if (deadline > Now) return Task.CompletedTask;
            if (f.Status == RefundStatus.Rejected && f.History.Any(h => h.Decision == "DeadlineExpired")
                && (RefundRules.SameDeadline(f.ReviewDeadlineAt, deadline) || RefundRules.SameDeadline(f.CustomerDetailsDeadlineAt, deadline)))
            { notify = true; return Task.CompletedTask; }
            if (!RefundRules.SameDeadline(RefundRules.Deadline(f), deadline)) return Task.CompletedTask;
            var customerExpired = f.Status == RefundStatus.AwaitingCustomerDetails;
            if (!customerExpired && f.Status is not (RefundStatus.PendingVerification or RefundStatus.AwaitingTransfer or RefundStatus.Overdue)) return Task.CompletedTask;
            var before = f.Status;
            if (RefundRules.ExpiredStatus(f) == RefundStatus.Overdue)
            {
                f.Status = RefundStatus.Overdue;
                if (before != RefundStatus.Overdue)
                    Audit(f, before, null, "System", "System", "MandatoryRefundOverdue", RefundRules.MandatoryRefundOverdue);
            }
            else
            {
                f.Status = RefundStatus.Rejected;
                f.RejectReason = customerExpired ? RefundRules.CustomerConfirmationExpired : RefundRules.ShopProcessingExpired;
                Audit(f, before, null, "System", "System", "DeadlineExpired", f.RejectReason);
            }
            notify = true;
            return Task.CompletedTask;
        }, ct, deleteSchedule: false);
        return notify ? result : null;
    }

    private async Task<RefundResponse> CommandAsync(Guid id, Func<Refund, Task> action, CancellationToken ct, bool deleteSchedule = true)
    {
        var orderId = (await LoadAsync(id, false, ct)).OrderId;
        string? deleteJob = null;
        Guid? changedShopId = null;
        ShopStatus? changedShopStatus = null;
        Guid? cleanupShopId = null;
        var result = await _orders.WithOrderLockAsync(orderId, async () =>
        {
            var shopId = await _db.Orders.Where(o => o.Id == orderId).Select(o => o.ShopId).SingleAsync(ct);
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM shops WHERE id = {shopId} FOR UPDATE", ct);
            var f = await LoadAsync(id, true, ct);
            var oldShopStatus = f.Order.Shop.Status;
            var oldShopReasons = ShopSuspensionRules.Reason(f.Order.Shop);
            var oldJob = f.DeadlineJobId;
            var oldDeadline = RefundRules.Deadline(f);
            await action(f);
            // Persist schedules for both customer confirmation and shop processing in the order transaction.
            if (f.Status == RefundStatus.AwaitingCustomerDetails)
                f.CustomerDetailsDeadlineAt ??= f.CreatedAt.AddHours(TimingConfiguration.Refund.CustomerDetailsHours);
            if (f.DestinationConfirmedAt.HasValue && f.Status is (RefundStatus.PendingVerification or RefundStatus.AwaitingTransfer))
                f.ReviewDeadlineAt ??= f.DestinationConfirmedAt.Value.AddHours(RefundRules.ShopProcessingHours);
            var deadline = RefundRules.Deadline(f);
            if (f.Status != RefundStatus.Overdue && RefundRules.Active(f.Status) && deadline.HasValue && (oldJob == null || oldDeadline != deadline))
                f.DeadlineJobId = _deadlines.Schedule(f.Id, deadline.Value);
            else if (f.Status == RefundStatus.Overdue || !RefundRules.Active(f.Status)) f.DeadlineJobId = null;
            if (oldJob != f.DeadlineJobId) deleteJob = oldJob;
            await _orders.SaveAsync(ct);
            var shop = f.Order.Shop;
            var hasOverdue = await _db.Refunds.AnyAsync(r => r.Order.ShopId == shop.Id && r.RefundType == RefundType.ShopCancellation
                && r.DestinationConfirmedAt != null && r.RequestedAmount > r.TransferredAmount
                && (r.Status == RefundStatus.Overdue || (r.Status == RefundStatus.AwaitingTransfer && r.ReviewDeadlineAt <= Now)), ct);
            shop.SuspendedForOverdueRefunds = hasOverdue;
            // A fee sweep may not have run yet; settling refunds must not bypass another overdue debt.
            if (await _db.PlatformFeeStatements.AnyAsync(statement => statement.ShopId == shop.Id
                && (statement.Status == PlatformFeeStatementStatus.Overdue ||
                    (statement.Status == PlatformFeeStatementStatus.Issued && statement.TotalFeeAmount > 0 && statement.DueAt <= Now)), ct))
                shop.SuspendedForPlatformFees = true;
            if (ShopSuspensionRules.HasReasons(shop))
            {
                shop.Status = ShopStatus.Suspended;
                if (hasOverdue || oldShopStatus != ShopStatus.Suspended) cleanupShopId = shop.Id;
            }
            else ShopSuspensionRules.ReactivateIfResolved(shop);
            if (shop.Status != oldShopStatus || oldShopReasons != ShopSuspensionRules.Reason(shop))
            {
                shop.UpdatedAt = Now;
                changedShopId = shop.Id;
                changedShopStatus = shop.Status;
                await _notifications.EnqueueAsync(NotificationTarget.User(shop.OwnerUserId), new CreateNotificationCommand(
                    NotificationType.ShopStatusChanged, NotificationMessages.Shop.ShopStatusUpdatedTitle,
                    shop.Status == ShopStatus.Suspended ? ShopSuspensionRules.Reason(shop)! : "All overdue mandatory refunds have been paid. Your shop has been reactivated.",
                    NotificationActionUrls.StoreOwnerRefunds("Overdue"), $"shop-refunds:{shop.Id}:{shop.Status}:{id}"), ct);
            }
            await _orders.SaveAsync(ct);
            await NotifyRefundAsync(id, ct);
            return Map(f);
        }, ct);
        if (changedShopId.HasValue && changedShopStatus.HasValue)
        {
            await using var scope = _scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IShopStatusEventService>()
                .NotifyChangedAsync(changedShopId.Value, changedShopStatus.Value.ToString(), ct);

        }
        if (cleanupShopId.HasValue)
        {
            await using var scope = _scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IShopSuspensionCleanupService>().CleanupAsync(cleanupShopId.Value, ct);
        }
        // Delete only after commit; a leftover job checks deadline and status before doing anything.
        if (deleteSchedule && deleteJob != null)
        {
            try { if (!_deadlines.Delete(deleteJob)) _logger.LogWarning("Could not delete refund deadline job {JobId}.", deleteJob); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete refund deadline job {JobId}.", deleteJob); }
        }
        return result;
    }

    private async Task NotifyRefundAsync(Guid id, CancellationToken ct)
    {
        var refund = await _refunds.GetAsync(id, false, ct) ?? throw AppException.NotFound();
        if (refund.RefundType == RefundType.CustomerRequest && refund.DestinationConfirmedAt.HasValue
            && refund.Status == RefundStatus.PendingVerification)
        {
            await _notifications.EnqueueAsync(NotificationTarget.User(refund.Order.Shop.OwnerUserId), new CreateNotificationCommand(
                NotificationType.RefundRequestCreated, NotificationMessages.Refunds.NewRefundRequestTitle,
                NotificationMessages.Refunds.NewRefundRequestBody(refund.Order.Customer.FullName, refund.Order.OrderCode),
                NotificationActionUrls.StoreOwnerOrder(refund.OrderId), $"refund:{id}:requested:owner"), ct);
            return;
        }
        if (refund.Status == RefundStatus.Overdue)
        {
            var command = new CreateNotificationCommand(NotificationType.RefundOverdue, "Mandatory refund overdue",
                RefundRules.MandatoryRefundOverdue, NotificationActionUrls.CustomerRefund(id), $"refund:{id}:overdue:customer");
            await _notifications.EnqueueAsync(NotificationTarget.User(refund.Order.CustomerId), command, ct);
            await _notifications.EnqueueAsync(NotificationTarget.User(refund.Order.Shop.OwnerUserId), command with
                { ActionUrl = NotificationActionUrls.StoreOwnerRefund(id), DeduplicationKey = $"refund:{id}:overdue:owner" }, ct);
            return;
        }
        var type = refund.Status switch {
            RefundStatus.AwaitingTransfer => (NotificationType?)NotificationType.RefundApproved,
            RefundStatus.Rejected => NotificationType.RefundRejected,
            RefundStatus.Transferred or RefundStatus.Completed => NotificationType.RefundTransferred,
            _ => null
        };
        if (type == null) return;
        await _notifications.EnqueueAsync(NotificationTarget.User(refund.Order.CustomerId), new CreateNotificationCommand(
            type.Value, NotificationMessages.Refunds.RefundStatusUpdatedTitle, NotificationMessages.Refunds.RefundStatusUpdatedBody(refund.Order.OrderCode, refund.Status, refund.RejectReason),
            NotificationActionUrls.CustomerRefund(id), $"refund:{id}:{type}"), ct);
    }
    private async Task<Refund> LoadAsync(Guid id, bool tracked, CancellationToken ct)
    {
        var f = await _refunds.GetAsync(id, tracked, ct) ?? throw AppException.NotFound("Refund not found.");
        // Load the authoritative full payment/refund scope for mutation checks only.
        if (tracked) f.Order = await _refunds.OrderAsync(f.OrderId, ct) ?? throw AppException.NotFound();
        return f;
    }
    private async Task AuthorizeAsync(Guid userId, bool forShop, Refund f, CancellationToken ct, bool readOnly = false)
    {
        if (forShop)
        {
            var shopId = await _shops.OwnerShopAsync(userId, ct);
            if (readOnly)
                await _shops.RequireViewAccessAsync(userId, shopId, ct);
            else
                await _shops.RequireAccessAsync(userId, shopId, ct, allowSuspended: true);

            if (f.Order.ShopId != shopId || (f.RefundType == RefundType.CustomerRequest
                && (f.Order.Status != OrderStatus.Completed || f.DestinationConfirmedAt == null)))
                throw AppException.NotFound("Refund not found in this shop.");
        }
        else
        {
            if (readOnly) await RequireCustomerHistoryAsync(userId, ct);
            else await RequireCustomerAsync(userId, ct);
            if (f.Order.CustomerId != userId) throw AppException.NotFound("Refund not found.");
        }
    }

    // History access requires an active account, but does not require the Customer feature to be active.
    private async Task RequireCustomerHistoryAsync(Guid id, CancellationToken ct)
    {
        var user = await _refunds.UserAsync(id, ct);
        if (user.Status != UserStatus.Active || user.Role == UserRole.Admin)
            throw AppException.Forbidden("An active account is required to view refund history.");
    }

    // Commands that create or change refund data still require active Customer access.
    private async Task RequireCustomerAsync(Guid id, CancellationToken ct)
    {
        var user = await _refunds.UserAsync(id, ct);
        if (user.Status != UserStatus.Active || user.CustomerStatus != CustomerStatus.Active || user.Role == UserRole.Admin)
            throw AppException.Forbidden("Active Customer access is required.");
    }
    // Validate selected quantities, per-item reasons and evidence before creating immutable item snapshots.
    private static List<RefundItem> BuildRequestedItems(Order order, IReadOnlyList<RefundItemRequest>? selections)
    {
        var selected = new List<RefundItem>();
        if (selections == null || selections.Count is < 1 or > 100 || selections.Select(x => x.OrderItemId).Distinct().Count() != selections.Count)
            throw AppException.BadRequest("Select distinct order items and their refund quantities.");
        var purchasedItems = order.Items.ToDictionary(x => x.Id);
        var evidenceUrls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var selection in selections)
        {
            var item = purchasedItems.GetValueOrDefault(selection.OrderItemId);
            if (item == null || selection.Quantity < 1 || selection.Quantity > item.Quantity)
                throw AppException.BadRequest("Refund items must belong to this order and quantities cannot exceed purchased quantities.");
            ValidateText(selection.Reason, 1000, $"{item.ProductNameSnapshot}: enter a refund reason (up to 1,000 characters).");
            // Each refunded portion needs its own evidence; an image cannot be reused across items.
            if (selection.EvidenceUrls == null || selection.EvidenceUrls.Count < selection.Quantity
                || selection.EvidenceUrls.Count > (2L * selection.Quantity))
                throw AppException.BadRequest($"{item.ProductNameSnapshot}: supply at least {selection.Quantity} different evidence images (maximum {(2L * selection.Quantity)}).");
            foreach (var url in selection.EvidenceUrls)
            {
                ValidateUrl(url);
                var imageKey = new Uri(url).GetLeftPart(UriPartial.Path);
                if (!evidenceUrls.Add(imageKey)) throw AppException.BadRequest("Each evidence image may be used only once, including across different items.");
            }
            selected.Add(new RefundItem
            {
                Id = Guid.NewGuid(),
                OrderItemId = item.Id,
                ProductName = item.ProductNameSnapshot,
                Reason = selection.Reason.Trim(),
                ImageUrl = PurchaseSnapshots.ProductImage(item),
                VariantName = item.VariantNameSnapshot,
                PurchasedQuantity = item.Quantity,
                Quantity = selection.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.UnitPrice * selection.Quantity
            });
        }
        return selected;
    }

    private void Audit(Refund f, RefundStatus? before, Guid? actorId, string role, string name, string decision, string? note, string? proof = null)
    {
        f.UpdatedAt = Now;
        var history = new RefundStatusHistory
        {
            Id = Guid.NewGuid(),
            RefundId = f.Id,
            FromStatus = before,
            ToStatus = f.Status,
            ActorId = actorId,
            ActorRole = role,
            ActorName = name,
            Decision = decision,
            Note = note,
            TransferReference = f.TransferReference,
            ProofUrl = proof,
            CreatedAt = Now
        };
        f.History.Add(history);
        _refunds.Add(history);
    }
    // Explicitly mark new dependent rows as Added even when their GUID keys are already assigned.
    private void AddEvidence(Refund f, Guid actor, string url, RefundEvidenceType type, Guid? refundItemId = null)
    {
        var evidence = new RefundEvidenceImage
        {
            Id = Guid.NewGuid(),
            RefundId = f.Id,
            ImageUrl = url,
            RefundItemId = refundItemId,
            ImageType = type,
            UploadedBy = actor,
            CreatedAt = Now
        };
        f.EvidenceImages.Add(evidence);
        _refunds.Add(evidence);
    }
    // Accept still-valid pre-migration codes only for legacy destinations that have no bank BIN.
    private bool MatchesCode(Refund f, string? code)
    {
        if (code == null || code.Length != 6 || !code.All(char.IsAsciiDigit) || f.VerificationCodeHash == null) return false;
        bool Equal(string expected) => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(f.VerificationCodeHash), Encoding.UTF8.GetBytes(expected));
        if (Equal(CodeHash(f.Id, code))) return true;
        return f.ReceiverBankBin == null && Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{f.Id:N}:{code}"))));
    }
    private string CodeHash(Guid id, string code)
    {
        var key = _config["Jwt:SigningKey"] ?? _config["JwtSettings:SigningKey"]
            ?? throw AppException.ServiceUnavailable("Refund email verification is not configured.");
        if (string.IsNullOrWhiteSpace(key)) throw AppException.ServiceUnavailable("Refund email verification is not configured.");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes($"{id:N}:{code}")));
    }
    private static T? Parse<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value) || value == "All") return null;
        if (!Enum.TryParse<T>(value, out var result) || !Enum.IsDefined(result) || result.ToString() != value)
            throw AppException.BadRequest("Invalid refund filter.");
        return result;
    }
    private static void ValidateText(string? value, int max, string error)
    { if (string.IsNullOrWhiteSpace(value) || value.Length > max) throw AppException.BadRequest(error); }
    private static void ValidateUrl(string? url)
    { if (string.IsNullOrWhiteSpace(url) || url.Length > 2048 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https") throw AppException.BadRequest("Evidence must be an HTTPS URL."); }
    // Classify owner work without changing stored refund status during view requests.
    private string ShopAction(Refund f)
    {
        if (f.Status == RefundStatus.AwaitingCustomerDetails) return "WaitingCustomer";
        if (f.Status is RefundStatus.Rejected or RefundStatus.Withdrawn or RefundStatus.Transferred or RefundStatus.Completed) return "Closed";
        if (f.RefundType == RefundType.ShopCancellation && f.Status == RefundStatus.Overdue && f.DestinationConfirmedAt != null) return "TransferRequired";
        if (!f.ReviewDeadlineAt.HasValue || f.ReviewDeadlineAt <= Now) return "DeadlinePassed";
        if (f.DestinationConfirmedAt == null) return "WaitingCustomer";
        if (f.Status == RefundStatus.PendingVerification && f.RefundType == RefundType.CustomerRequest) return "ReviewRequired";
        return f.Status == RefundStatus.AwaitingTransfer ? "TransferRequired" : "Closed";
    }
    private RefundResponse Map(Refund f) => new()
    {
        Id = f.Id,
        OrderId = f.OrderId,
        OrderCode = f.Order.OrderCode,
        OrderStatus = f.Order.Status.ToString(),
        OrderTotal = f.Order.TotalAmount,
        OrderRefundedAmount = f.Order.RefundedAmount,
        OrderNetAmount = Math.Max(0, f.Order.TotalAmount - f.Order.RefundedAmount),
        Items = f.Items.Select(x => new RefundItemResponse { Reason = x.Reason, EvidenceUrls = f.EvidenceImages.Where(image => image.RefundItemId == x.Id && image.ImageType == RefundEvidenceType.RequestEvidence).Select(image => image.ImageUrl).ToList(), OrderItemId = x.OrderItemId, ProductName = x.ProductName, VariantName = x.VariantName, ImageUrl = x.ImageUrl, PurchasedQuantity = x.PurchasedQuantity, Quantity = x.Quantity, UnitPrice = x.UnitPrice, Amount = x.Amount }).ToList(),
        ShopId = f.Order.ShopId,
        ShopName = PurchaseSnapshots.ShopName(f.Order),
        ShopLogoUrl = PurchaseSnapshots.ShopLogo(f.Order),
        CustomerName = f.Order.Customer.FullName,
        Type = f.RefundType.ToString(),
        Status = f.Status.ToString(),
        CustomerAvatarUrl = f.Order.Customer.AvatarUrl,
        CustomerPhone = f.Order.Customer.Phone,
        CustomerLevelName = f.Order.Customer.TrustScore?.TrustLevel?.Name,
        CustomerLevelRank = f.Order.Customer.TrustScore?.TrustLevel?.Rank,
        ShopAction = ShopAction(f),
        RequiresShopAction = ShopAction(f) is "ReviewRequired" or "TransferRequired",
        Amount = f.RequestedAmount,
        TransferredAmount = f.TransferredAmount,
        Reason = f.Reason,
        RejectReason = f.RejectReason,
        ReceiverMethod = f.ReceiverMethod?.ToString(),
        ReceiverQrImageUrl = f.ReceiverQrImageUrl,
        BankBin = f.ReceiverBankBin,
        BankName = f.ReceiverBankName,
        AccountNumber = f.ReceiverAccountNumber,
        AccountHolder = f.ReceiverAccountHolder,
        ConfirmationExpiresAt = f.VerificationExpiresAt,
        DestinationConfirmedAt = f.DestinationConfirmedAt,
        CustomerDetailsDeadlineAt = f.CustomerDetailsDeadlineAt,
        PendingVerificationAt = f.PendingVerificationAt,
        ReviewDeadlineAt = f.ReviewDeadlineAt,
        IsOverdue = f.Status == RefundStatus.Overdue || (f.Status is (RefundStatus.PendingVerification or RefundStatus.AwaitingTransfer) && f.ReviewDeadlineAt <= Now),
        IsPlatformFeeExempt = f.Order.IsPlatformFeeExempt,
        ReviewedBy = f.ReviewedBy,
        ReviewedAt = f.ReviewedAt,
        TransferReference = f.TransferReference,
        TransferredAt = f.TransferredAt,
        CreatedAt = f.CreatedAt,
        EvidenceUrls = f.EvidenceImages.Where(x => x.ImageType == RefundEvidenceType.RequestEvidence).Select(x => x.ImageUrl).ToList(),
        TransferProofUrls = f.EvidenceImages.Where(x => x.ImageType == RefundEvidenceType.TransferProof).Select(x => x.ImageUrl).ToList(),
        History = f.History.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new RefundHistoryResponse
        {
            Id = x.Id,
            FromStatus = x.FromStatus?.ToString(),
            ToStatus = x.ToStatus.ToString(),
            ActorId = x.ActorId,
            ActorRole = x.ActorRole,
            ActorName = x.ActorName,
            Decision = x.Decision,
            Note = x.Note,
            TransferReference = x.TransferReference,
            ProofUrl = x.ProofUrl,
            CreatedAt = x.CreatedAt
        }).ToList()
    };
}
