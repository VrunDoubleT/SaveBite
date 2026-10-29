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
    private readonly IRefundRepository _refunds;
    private readonly ICheckoutRepository _orders;
    private readonly IShopOrderRepository _shops;
    private readonly ITrustOrderRepository _trust;
    private readonly IEmailService _email;
    private readonly TimeProvider _clock;
    private readonly IConfiguration _config;
    private readonly IRefundDeadlineScheduler _deadlines;
    private readonly ILogger<RefundService> _logger;

    public RefundService(IRefundRepository refunds, ICheckoutRepository orders, IShopOrderRepository shops,
        ITrustOrderRepository trust, IEmailService email, TimeProvider clock, IConfiguration config, IRefundDeadlineScheduler deadlines, ILogger<RefundService> logger)
    {
        _refunds = refunds; _orders = orders; _shops = shops; _trust = trust;
        _email = email; _clock = clock; _config = config; _deadlines = deadlines; _logger = logger;
    }
    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    // Read the customer's action-required badge without changing refund state.
    public async Task<int> PendingDetailsCountAsync(Guid customerId, CancellationToken ct)
    {
        await RequireCustomerAsync(customerId, ct);
        return await _refunds.PendingDetailsCountAsync(customerId, ct);
    }

    // Count unresolved mandatory refunds and confirmed requests still awaiting owner review or transfer.
    public async Task<int> ShopPendingCountAsync(Guid ownerId, Guid shopId, CancellationToken ct)
    {
        await RequireOwnerAsync(ownerId, shopId, ct);
        return await _refunds.ShopPendingCountAsync(shopId, ct);
    }

    // Scope list access before retrieving any records; no status changes occur on a GET.
    public async Task<PagedResult<RefundResponse>> PageAsync(Guid userId, Guid? shopId, string? status, string? type, int page, int size, CancellationToken ct)
    {
        if (size is < 1 or > 50 || page < 1 || (long)(page - 1) * size > int.MaxValue)
            throw AppException.BadRequest("Invalid pagination.");
        if (shopId.HasValue) await RequireOwnerAsync(userId, shopId.Value, ct);
        else await RequireCustomerAsync(userId, ct);
        var result = await _refunds.PageAsync(userId, shopId, Parse<RefundStatus>(status), Parse<RefundType>(type), page, size, ct);
        return PagedResult<RefundResponse>.Create(result.Items.Select(Map).ToList(), page, size, result.Total);
    }

    // Keep shop refund operations available inside order details without exposing unconfirmed customer requests.
    public async Task<IReadOnlyList<RefundResponse>> ShopForOrderAsync(Guid ownerId, Guid shopId, Guid orderId, CancellationToken ct)
    {
        await RequireOwnerAsync(ownerId, shopId, ct);
        _ = await _shops.GetAsync(shopId, orderId, ct) ?? throw AppException.NotFound();
        return (await _refunds.ForOrderAsync(orderId, ct)).Where(f => f.RefundType == RefundType.ShopCancellation
            || (f.Order.Status == OrderStatus.Completed && f.DestinationConfirmedAt != null)).Select(Map).ToList();
    }

    // Read all refunds of one owned order without triggering reconciliation or writes.
    public async Task<IReadOnlyList<RefundResponse>> ForOrderAsync(Guid customerId, Guid orderId, CancellationToken ct)
    {
        _ = await _orders.GetOrderAsync(orderId, customerId, ct) ?? throw AppException.NotFound("Order not found.");
        return (await _refunds.ForOrderAsync(orderId, ct)).Select(Map).ToList();
    }

    // Return only recorded refund information and immutable audit history.
    public async Task<RefundResponse> GetAsync(Guid userId, Guid refundId, Guid? shopId, CancellationToken ct)
    {
        var f = await LoadAsync(refundId, false, ct);
        await AuthorizeAsync(userId, shopId, f, ct);
        return Map(f);
    }

    // Create one full-order refund request within six hours of completion; never accept a client amount.
    public Task<RefundResponse> RequestAsync(Guid userId, Guid orderId, CreateRefundRequest request, CancellationToken ct)
        => _orders.WithOrderLockAsync(orderId, async () =>
        {
            _ = await _orders.GetOrderAsync(orderId, userId, ct) ?? throw AppException.NotFound("Order not found.");
            var order = await _refunds.OrderAsync(orderId, ct) ?? throw AppException.NotFound();
            RefundRules.ValidateRequest(order, Now);
            ValidateText(request.Reason, 1000, "A refund reason is required.");
            if (request.EvidenceUrls == null || request.EvidenceUrls.Count is < 1 or > 10) throw AppException.BadRequest("Supply between one and ten evidence images.");
            foreach (var url in request.EvidenceUrls) ValidateUrl(url);
            if (order.Refunds.Any(x => RefundRules.Active(x.Status))) throw AppException.Conflict("An active refund already exists for this order.");
            var amount = RefundRules.EligibleAmount(order);
            if (amount <= 0) throw AppException.Conflict("This order has no refundable paid balance.");
            var f = new Refund { Id = Guid.NewGuid(), OrderId = order.Id, Order = order, RefundType = RefundType.CustomerRequest,
                InitiatedBy = userId, Reason = request.Reason.Trim(), RequestedAmount = amount,
                Status = RefundStatus.AwaitingCustomerDetails, CreatedAt = Now, UpdatedAt = Now };
            _refunds.Add(f);
            foreach (var url in request.EvidenceUrls.Distinct()) AddEvidence(f, userId, url, RefundEvidenceType.RequestEvidence);
            Audit(f, null, userId, "Customer", order.Customer.FullName, "Requested", f.Reason);
            await _orders.SaveAsync(ct);
            return Map(f);
        }, ct);

    // Store an unconfirmed bank destination and send a new email code; this does not start owner review.
    public Task<RefundResponse> DestinationAsync(Guid userId, Guid refundId, RefundBankRequest request, CancellationToken ct)
    {
        RefundRules.ValidateBank(request);
        return CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, null, f, ct);
            if (f.Status != RefundStatus.AwaitingCustomerDetails || f.DestinationConfirmedAt != null)
                throw AppException.Conflict("The destination can no longer be changed.");
            RefundRules.ValidateCustomerDetailsDeadline(f, Now);
            if (f.VerificationIssuedAt > Now.AddMinutes(-1)) throw AppException.Conflict("Wait one minute before requesting another code.");
            var code = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
            if (request.ReceiverMethod is not ("BankAccount" or "QrImage")) throw AppException.BadRequest("Choose bank details or a bank QR image.");
            if (request.ReceiverMethod == "QrImage") ValidateUrl(request.QrImageUrl);
            f.ReceiverMethod = request.ReceiverMethod == "QrImage" ? RefundReceiverMethod.QrImage : RefundReceiverMethod.BankAccount;
            f.ReceiverQrImageUrl = f.ReceiverMethod == RefundReceiverMethod.QrImage ? request.QrImageUrl : null;
            f.ReceiverBankBin = request.BankBin; f.ReceiverBankName = request.BankName.Trim();
            f.ReceiverAccountNumber = request.AccountNumber; f.ReceiverAccountHolder = request.AccountHolder.Trim();
            f.VerificationCodeHash = CodeHash(f.Id, code); f.VerificationIssuedAt = Now;
            f.VerificationExpiresAt = Now.AddMinutes(15); f.VerificationAttempts = 0;
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
            await AuthorizeAsync(userId, null, f, ct);
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
            f.DestinationConfirmedAt = Now; f.ReviewDeadlineAt = Now.AddHours(24);
            if (f.RefundType == RefundType.CustomerRequest)
            {
                if (f.Order.Status != OrderStatus.Completed) throw AppException.Conflict("Only completed orders can be reviewed.");
                f.Status = RefundStatus.PendingVerification; f.PendingVerificationAt = Now; f.ReviewDeadlineAt = Now.AddHours(24);
            }
            else f.Status = RefundStatus.AwaitingTransfer;
            Audit(f, before, userId, "Customer", f.Order.Customer.FullName, "DestinationConfirmed", "Bank destination confirmed through email.");
            valid = true;
        }, ct);
        if (!valid) throw AppException.BadRequest("The confirmation code is invalid, expired, or has reached its attempt limit.");
        return result;
    }

    // Withdrawal prevents subsequent review and transfer of this refund.
    public Task<RefundResponse> WithdrawAsync(Guid userId, Guid refundId, CancellationToken ct)
        => CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, null, f, ct);
            if (f.Status == RefundStatus.Withdrawn) return;
            if (f.Status == RefundStatus.AwaitingCustomerDetails) RefundRules.ValidateCustomerDetailsDeadline(f, Now);
            if (f.Status == RefundStatus.PendingVerification) RefundRules.ValidateDeadline(f, Now);
            if (f.Status is not (RefundStatus.AwaitingCustomerDetails or RefundStatus.PendingVerification))
                throw AppException.Conflict("This refund can no longer be withdrawn.");
            var before = f.Status; f.Status = RefundStatus.Withdrawn;
            Audit(f, before, userId, "Customer", f.Order.Customer.FullName, "Withdrawn", "Customer withdrew the refund request.");
        }, ct);

    // Only customer requests require a decision; mandatory shop-cancellation refunds cannot be rejected.
    public Task<RefundResponse> ReviewAsync(Guid userId, Guid shopId, Guid refundId, bool approve, string? reason, CancellationToken ct)
        => CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, shopId, f, ct);
            if (f.RefundType != RefundType.CustomerRequest || f.Order.Status != OrderStatus.Completed)
                throw AppException.Conflict("Only post-completion customer requests can be reviewed.");
            if (approve && f.Status == RefundStatus.AwaitingTransfer) return;
            if (!approve && f.Status == RefundStatus.Rejected && f.RejectReason == reason?.Trim()) return;
            if (f.Status != RefundStatus.PendingVerification || f.DestinationConfirmedAt == null)
                throw AppException.Conflict("Review requires PendingVerification and a confirmed bank destination.");
            RefundRules.ValidateDeadline(f, Now);
            if (!approve) ValidateText(reason, 1000, "A rejection reason is required.");
            if (approve && RefundRules.EligibleAmount(f.Order) != f.RequestedAmount)
                throw AppException.Conflict("The full refundable amount changed; refresh the refund request.");
            var before = f.Status;
            f.Status = approve ? RefundStatus.AwaitingTransfer : RefundStatus.Rejected;
            f.ReviewedBy = userId; f.ReviewedAt = Now; f.RejectReason = approve ? null : reason!.Trim();
            Audit(f, before, userId, "StoreOwner", (await _refunds.UserAsync(userId, ct)).FullName,
                approve ? "Approved" : "Rejected", approve ? $"Full eligible amount of {f.RequestedAmount} VND approved." : f.RejectReason);
            if (approve) await WaiveFeeAsync(f, ct);
        }, ct);

    // Record full manual transfer and reclaim the credited order value exactly once in the same transaction.
    public Task<RefundResponse> TransferAsync(Guid userId, Guid shopId, Guid refundId, RefundTransferRequest request, CancellationToken ct)
    {
        ValidateText(request.TransferReference, 150, "A transfer reference is required.");
        ValidateUrl(request.ProofUrl);
        return CommandAsync(refundId, async f =>
        {
            await AuthorizeAsync(userId, shopId, f, ct);
            if (f.Status == RefundStatus.Transferred && f.TransferReference == request.TransferReference.Trim()) return;
            var transferredAt = request.TransferredAt ?? Now;
            RefundRules.ValidateTransfer(f, transferredAt, Now);
            var before = f.Status;
            var transferAmount = f.RequestedAmount - f.TransferredAmount;
            f.Status = RefundStatus.Transferred; f.TransferredAmount = f.RequestedAmount;
            f.TransferReference = request.TransferReference.Trim(); f.TransferredAt = transferredAt;
            if (request.ProofUrl != null) AddEvidence(f, userId, request.ProofUrl, RefundEvidenceType.TransferProof);
            Audit(f, before, userId, "StoreOwner", (await _refunds.UserAsync(userId, ct)).FullName,
                "Transferred", $"Manual transfer of the full remaining {transferAmount} VND recorded; total transferred {f.TransferredAmount} VND.", request.ProofUrl);
            await WaiveFeeAsync(f, ct);
            await ReclaimValueAsync(f, ct);
        }, ct);
    }

    // Generate instructions from persisted approved amount and confirmed destination, never client overrides.
    public async Task<RefundQrResponse> GenerateQrAsync(Guid userId, Guid shopId, Guid refundId, CancellationToken ct)
    {
        var f = await LoadAsync(refundId, false, ct);
        await AuthorizeAsync(userId, shopId, f, ct);
        RefundRules.RequireTransferable(f, Now);
        RefundRules.RequireDestinationFields(f, requireBankBin: true);
        var content = "SB RF " + f.Id.ToString("N")[..16].ToUpperInvariant();
        return new RefundQrResponse { RefundId = f.Id, Amount = f.RequestedAmount - f.TransferredAmount,
            BankBin = f.ReceiverBankBin!, AccountNumber = f.ReceiverAccountNumber!, AccountHolder = f.ReceiverAccountHolder!,
            TransferContent = content,
            ImageUrl = $"https://img.vietqr.io/image/{f.ReceiverBankBin}-{f.ReceiverAccountNumber}-compact2.png?amount={(f.RequestedAmount - f.TransferredAmount).ToString("0.##", CultureInfo.InvariantCulture)}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString(f.ReceiverAccountHolder!)}" };
    }

    // An admin may unlock an overdue request by setting a new audited deadline; the owner still makes the decision.
    public Task<RefundResponse> ExtendDeadlineAsync(Guid adminId, Guid refundId, RefundDeadlineRequest request, CancellationToken ct)
        => CommandAsync(refundId, async f =>
        {
            var admin = await _refunds.UserAsync(adminId, ct);
            if (admin.Role != UserRole.Admin || admin.Status != UserStatus.Active) throw AppException.Forbidden();
            ValidateText(request.Reason, 1000, "An administrative reason is required.");
            if (request.DeadlineAt.Kind != DateTimeKind.Utc || request.DeadlineAt <= Now)
                throw AppException.BadRequest("The new deadline must be a future UTC timestamp.");
            if (f.RefundType != RefundType.CustomerRequest || f.Status != RefundStatus.Overdue
                || !f.ReviewDeadlineAt.HasValue || f.ReviewDeadlineAt > Now) throw AppException.Conflict("Only an overdue active customer request can be extended.");
            var before = f.Status;
            var previous = f.ReviewDeadlineAt; f.ReviewDeadlineAt = request.DeadlineAt;
            if (f.Status == RefundStatus.Overdue) f.Status = f.ReviewedAt.HasValue ? RefundStatus.AwaitingTransfer : RefundStatus.PendingVerification;
            Audit(f, before, adminId, "Admin", admin.FullName, "DeadlineExtended", $"{previous:O} -> {request.DeadlineAt:O}. {request.Reason.Trim()}");
        }, ct);

    // Compatibility lookup is restricted to the customer's system-required refund and remains read-only.
    public async Task<Guid?> SystemRefundIdAsync(Guid customerId, Guid orderId, CancellationToken ct, Guid? shopId = null)
    {
        if (shopId.HasValue)
        {
            await RequireOwnerAsync(customerId, shopId.Value, ct);
            _ = await _shops.GetAsync(shopId.Value, orderId, ct) ?? throw AppException.NotFound();
        }
        else _ = await _orders.GetOrderAsync(orderId, customerId, ct) ?? throw AppException.NotFound();
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
            var customerExpired = f.Status == RefundStatus.AwaitingCustomerDetails && f.RefundType == RefundType.ShopCancellation;
            if (!customerExpired && f.Status is not (RefundStatus.PendingVerification or RefundStatus.AwaitingTransfer or RefundStatus.Overdue)) return Task.CompletedTask;
            var before = f.Status; f.Status = RefundStatus.Rejected;
            f.RejectReason = customerExpired ? RefundRules.CustomerConfirmationExpired : RefundRules.ShopProcessingExpired;
            Audit(f, before, null, "System", "System", "DeadlineExpired", f.RejectReason);
            notify = true;
            return Task.CompletedTask;
        }, ct, deleteSchedule: false);
        return notify ? result : null;
    }

    private async Task<RefundResponse> CommandAsync(Guid id, Func<Refund, Task> action, CancellationToken ct, bool deleteSchedule = true)
    {
        var orderId = (await LoadAsync(id, false, ct)).OrderId;
        string? deleteJob = null;
        var result = await _orders.WithOrderLockAsync(orderId, async () =>
        {
            var f = await LoadAsync(id, true, ct);
            var oldJob = f.DeadlineJobId;
            var oldDeadline = RefundRules.Deadline(f);
            await action(f);
            // Persist schedules for both customer confirmation and shop processing in the order transaction.
            if (f.RefundType == RefundType.ShopCancellation && f.Status == RefundStatus.AwaitingCustomerDetails)
                f.CustomerDetailsDeadlineAt ??= f.CreatedAt.AddHours(24);
            if (f.DestinationConfirmedAt.HasValue && f.Status is (RefundStatus.PendingVerification or RefundStatus.AwaitingTransfer))
                f.ReviewDeadlineAt ??= f.DestinationConfirmedAt.Value.AddHours(24);
            var deadline = RefundRules.Deadline(f);
            if (RefundRules.Active(f.Status) && deadline.HasValue && (oldJob == null || oldDeadline != deadline))
                f.DeadlineJobId = _deadlines.Schedule(f.Id, deadline.Value);
            else if (!RefundRules.Active(f.Status)) f.DeadlineJobId = null;
            if (oldJob != f.DeadlineJobId) deleteJob = oldJob;
            await _orders.SaveAsync(ct);
            return Map(f);
        }, ct);
        // Delete only after commit; a leftover job checks deadline and status before doing anything.
        if (deleteSchedule && deleteJob != null)
        {
            try { if (!_deadlines.Delete(deleteJob)) _logger.LogWarning("Could not delete refund deadline job {JobId}.", deleteJob); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete refund deadline job {JobId}.", deleteJob); }
        }
        return result;
    }
    private async Task<Refund> LoadAsync(Guid id, bool tracked, CancellationToken ct)
    {
        var f = await _refunds.GetAsync(id, tracked, ct) ?? throw AppException.NotFound("Refund not found.");
        // Load the authoritative full payment/refund scope for mutation checks only.
        if (tracked) f.Order = await _refunds.OrderAsync(f.OrderId, ct) ?? throw AppException.NotFound();
        return f;
    }
    private async Task AuthorizeAsync(Guid userId, Guid? shopId, Refund f, CancellationToken ct)
    {
        if (shopId.HasValue)
        {
            await RequireOwnerAsync(userId, shopId.Value, ct);
            if (f.Order.ShopId != shopId.Value || (f.RefundType == RefundType.CustomerRequest
                && (f.Order.Status != OrderStatus.Completed || f.DestinationConfirmedAt == null))) throw AppException.NotFound("Refund not found in this shop.");
        }
        else
        {
            await RequireCustomerAsync(userId, ct);
            if (f.Order.CustomerId != userId) throw AppException.NotFound("Refund not found.");
        }
    }
    private async Task RequireCustomerAsync(Guid id, CancellationToken ct)
    {
        var user = await _refunds.UserAsync(id, ct);
        if (user.Status != UserStatus.Active || user.CustomerStatus != CustomerStatus.Active || user.Role == UserRole.Admin)
            throw AppException.Forbidden("Active Customer access is required.");
    }
    private async Task RequireOwnerAsync(Guid id, Guid shopId, CancellationToken ct)
    {
        if (!await _shops.RequireAccessAsync(id, shopId, ct)) throw AppException.Forbidden("Only the Store Owner may access refund management.");
    }
    private async Task WaiveFeeAsync(Refund f, CancellationToken ct)
    {
        var adjustment = RefundFeePolicy.Waive(f.Order, f, await _refunds.FeeAsync(f.OrderId, ct), Now);
        if (adjustment != null) _refunds.Add(adjustment);
    }
    private async Task ReclaimValueAsync(Refund f, CancellationToken ct)
    {
        if (f.AccumulatedValueReclaimedAt.HasValue || f.Order.Status != OrderStatus.Completed) return;
        var receipt = await _refunds.ProcessingAsync(f.OrderId, ct);
        var credited = receipt?.CreditedOrderValue ?? (await _refunds.LegacyCreditAsync(f.OrderId, ct) ? f.Order.TotalAmount : 0);
        var progress = await _trust.ProgressAsync(f.Order.CustomerId, ct);
        if (credited > 0 && progress == null) throw AppException.ServiceUnavailable("Accumulated progress is missing; refund processing requires review.");
        if (progress != null)
        {
            progress.AccumulatedOrderValue = Math.Max(0, progress.AccumulatedOrderValue - credited);
            progress.UpdatedAt = Now;
            var score = await _trust.ScoreAsync(f.Order.CustomerId, ct);
            var levels = await _trust.LevelsAsync(ct);
            if (score == null || levels.Count == 0) throw AppException.ServiceUnavailable("Trust data is not ready for refund processing.");
            var level = levels.Where(x => x.Requirement != null && x.Requirement.MinScore <= score.CurrentScore
                && x.Requirement.MinCompletedOrders <= progress.CompletedOrdersCount
                && x.Requirement.MinTotalOrderValue <= progress.AccumulatedOrderValue).OrderByDescending(x => x.Rank).FirstOrDefault() ?? levels[0];
            if (level.Id != score.TrustLevelId)
            {
                _refunds.Add(new UserTrustLevelHistory { Id = Guid.NewGuid(), UserId = score.UserId,
                    PreviousTrustLevelId = score.TrustLevelId, NewTrustLevelId = level.Id,
                    Reason = TrustLevelChangeReason.RefundTransferred, OrderId = f.OrderId, CreatedAt = Now });
                score.TrustLevelId = level.Id; score.UpdatedAt = Now;
            }
        }
        if (receipt != null) receipt.CreditedOrderValue = 0;
        f.AccumulatedValueReclaimedAt = Now;
    }
    private void Audit(Refund f, RefundStatus? before, Guid? actorId, string role, string name, string decision, string? note, string? proof = null)
    {
        f.UpdatedAt = Now;
        var history = new RefundStatusHistory { Id = Guid.NewGuid(), RefundId = f.Id, FromStatus = before, ToStatus = f.Status,
            ActorId = actorId, ActorRole = role, ActorName = name, Decision = decision, Note = note,
            TransferReference = f.TransferReference, ProofUrl = proof, CreatedAt = Now };
        f.History.Add(history);
        _refunds.Add(history);
    }
    // Explicitly mark new dependent rows as Added even when their GUID keys are already assigned.
    private void AddEvidence(Refund f, Guid actor, string url, RefundEvidenceType type)
    {
        var evidence = new RefundEvidenceImage { Id = Guid.NewGuid(), RefundId = f.Id, ImageUrl = url,
            ImageType = type, UploadedBy = actor, CreatedAt = Now };
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
        if (!f.ReviewDeadlineAt.HasValue || f.ReviewDeadlineAt <= Now || f.Status == RefundStatus.Overdue) return "DeadlinePassed";
        if (f.DestinationConfirmedAt == null) return "WaitingCustomer";
        if (f.Status == RefundStatus.PendingVerification && f.RefundType == RefundType.CustomerRequest) return "ReviewRequired";
        return f.Status == RefundStatus.AwaitingTransfer ? "TransferRequired" : "Closed";
    }
    private RefundResponse Map(Refund f) => new()
    {
        Id = f.Id, OrderId = f.OrderId, OrderCode = f.Order.OrderCode, OrderStatus = f.Order.Status.ToString(), OrderTotal = f.Order.TotalAmount,
        ShopId = f.Order.ShopId, ShopName = f.Order.Shop.Name, ShopLogoUrl = f.Order.Shop.LogoUrl, CustomerName = f.Order.Customer.FullName, Type = f.RefundType.ToString(), Status = f.Status.ToString(),
        CustomerAvatarUrl = f.Order.Customer.AvatarUrl, CustomerPhone = f.Order.Customer.Phone,
        CustomerLevelName = f.Order.Customer.TrustScore?.TrustLevel?.Name, CustomerLevelRank = f.Order.Customer.TrustScore?.TrustLevel?.Rank,
        ShopAction = ShopAction(f), RequiresShopAction = ShopAction(f) is "ReviewRequired" or "TransferRequired",
        Amount = f.RequestedAmount, TransferredAmount = f.TransferredAmount, Reason = f.Reason, RejectReason = f.RejectReason,
        ReceiverMethod = f.ReceiverMethod?.ToString(), ReceiverQrImageUrl = f.ReceiverQrImageUrl,
        BankBin = f.ReceiverBankBin, BankName = f.ReceiverBankName, AccountNumber = f.ReceiverAccountNumber, AccountHolder = f.ReceiverAccountHolder,
        ConfirmationExpiresAt = f.VerificationExpiresAt, DestinationConfirmedAt = f.DestinationConfirmedAt,
        CustomerDetailsDeadlineAt = f.CustomerDetailsDeadlineAt, PendingVerificationAt = f.PendingVerificationAt, ReviewDeadlineAt = f.ReviewDeadlineAt,
        IsOverdue = f.Status == RefundStatus.Overdue || (f.Status is (RefundStatus.PendingVerification or RefundStatus.AwaitingTransfer) && f.ReviewDeadlineAt <= Now),
        IsPlatformFeeExempt = f.Order.IsPlatformFeeExempt,
        ReviewedBy = f.ReviewedBy, ReviewedAt = f.ReviewedAt, TransferReference = f.TransferReference, TransferredAt = f.TransferredAt,
        CreatedAt = f.CreatedAt, EvidenceUrls = f.EvidenceImages.Where(x => x.ImageType == RefundEvidenceType.RequestEvidence).Select(x => x.ImageUrl).ToList(),
        TransferProofUrls = f.EvidenceImages.Where(x => x.ImageType == RefundEvidenceType.TransferProof).Select(x => x.ImageUrl).ToList(),
        History = f.History.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => new RefundHistoryResponse {
            Id = x.Id, FromStatus = x.FromStatus?.ToString(), ToStatus = x.ToStatus.ToString(), ActorId = x.ActorId,
            ActorRole = x.ActorRole, ActorName = x.ActorName, Decision = x.Decision, Note = x.Note,
            TransferReference = x.TransferReference, ProofUrl = x.ProofUrl, CreatedAt = x.CreatedAt }).ToList()
    };
}
