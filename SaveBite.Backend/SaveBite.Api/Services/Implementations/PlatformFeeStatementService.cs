using SaveBite.Backend.Configurations;
using SaveBite.Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class PlatformFeeStatementService : IPlatformFeeStatementService
{
    private readonly IRealtimePublisher? _feeRealtime;

    private async Task PublishPaidFeeAsync(Guid statementId, CancellationToken ct)
    {
        if (_feeRealtime == null) return;
        try
        {
            var statement = await _db.PlatformFeeStatements.AsNoTracking().Include(s => s.Shop)
                .SingleOrDefaultAsync(s => s.Id == statementId && s.Status == PlatformFeeStatementStatus.Paid, ct);
            if (statement == null) return;
            var recipients = await _db.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync(ct);
            recipients.Add(statement.Shop.OwnerUserId);
            foreach (var recipient in recipients.Distinct())
            {
                try { await _feeRealtime.SendToUserAsync(recipient, RealtimeEvents.PlatformFeeChanged,
                    new { StatementId = statement.Id, ShopId = statement.ShopId, Status = "Paid" }); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not publish paid fee {StatementId} to {UserId}.", statementId, recipient); }
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not publish paid fee {StatementId}.", statementId); }
    }
    public async Task<PlatformFeePeriodStatisticsResponse> GetPeriodStatisticsAsync(int? year, int? month, CancellationToken ct = default)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12)
            throw AppException.BadRequest("Select a valid billing year and month.");
        var bills = await _db.PlatformFeeStatements.AsNoTracking()
            .Where(s => (!year.HasValue || s.BillingYear == year) && (!month.HasValue || s.BillingMonth == month) && s.Status != PlatformFeeStatementStatus.Draft)
            .ToListAsync(ct);
        return PlatformFeePeriodStatistics.Calculate(bills, year, month, _timeProvider.GetUtcNow().UtcDateTime);
    }
    private readonly IAdminAuditWriter _audit;
    private readonly AppDbContext _db;
    private readonly INotificationQueue _notifications;
    private readonly IPlatformFeeStatementRepository _repository;
    private readonly IPlatformPayOsConfigService _payOsConfig;
    private readonly PlatformFeePaymentAvailability _availability;
    private readonly IPayOsApiClient _payOsApi;
    private readonly IPayOsCredentialProtector _credentialProtector;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlatformFeeStatementService> _logger;
    // more
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IShopSuspensionCleanupService _suspensionCleanup;
    private readonly IShopStatusEventService _shopStatusEvents;

    public PlatformFeeStatementService(
        IPlatformFeeStatementRepository repository,
        TimeProvider timeProvider,
       // ILogger<PlatformFeeStatementService> logger)
        ILogger<PlatformFeeStatementService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        INotificationQueue notifications, AppDbContext db, IAdminAuditWriter audit,
        IPlatformPayOsConfigService payOsConfig,
        IShopSuspensionCleanupService suspensionCleanup,
        IShopStatusEventService shopStatusEvents, PlatformFeePaymentAvailability availability,
        IPayOsApiClient payOsApi, IPayOsCredentialProtector credentialProtector, IRealtimePublisher? feeRealtime = null)
    {
        _availability = availability;
        _feeRealtime = feeRealtime;
        _payOsApi = payOsApi;
        _credentialProtector = credentialProtector;
        _audit = audit;
        _payOsConfig = payOsConfig;
        _db = db;
        _notifications = notifications;
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _suspensionCleanup = suspensionCleanup;
        _shopStatusEvents = shopStatusEvents;
    }

    // UC-79: View System Fees (BR-092, BR-093, BR-192, BR-193)
    public async Task<PagedResult<PlatformFeeStatementSummaryResponse>> GetShopStatementsAsync(
        Guid shopId,
        Guid currentUserId,
        GetShopPlatformFeesRequest request,
        CancellationToken ct = default)
    {
        await ValidateStoreOwnerAccessAsync(shopId, currentUserId, ct);

        var paged = await _repository.GetShopStatementsAsync(shopId, request, ct);

        var items = paged.Items.Select(s => new PlatformFeeStatementSummaryResponse(
            s.Id,
            s.ShopId,
            s.BillingYear,
            s.BillingMonth,
            s.Status,
            s.TotalFeeAmount,
            s.IssuedAt,
            s.DueAt,
            s.PaidAt,
            s.CreatedAt,
            s.ShopNameSnapshot ?? s.Shop?.Name,
            s.SnapshotCapturedAt.HasValue ? s.ShopLogoUrlSnapshot : s.Shop?.LogoUrl, s.PaymentPauseReason, s.PaymentPausedAt
        )).ToList();

        return PagedResult<PlatformFeeStatementSummaryResponse>.Create(
            items,
            paged.Page,
            paged.PageSize,
            paged.TotalItems);
    }

    public async Task<ShopMonthlyPlatformFeeReportResponse> GetShopMonthlyPlatformFeeReportAsync(
        Guid shopId,
        Guid currentUserId,
        GetShopMonthlyPlatformFeeReportRequest request,
        CancellationToken ct = default)
    {
        await ValidateStoreOwnerAccessAsync(shopId, currentUserId, ct);

        var report = await _repository.GetShopMonthlyPlatformFeeReportAsync(shopId, request, ct);
        var orderItems = report.Orders.Items.Select(f => new ShopMonthlyPlatformFeeOrderResponse
        {
            Id = f.Id,
            OrderId = f.OrderId,
            OrderCode = f.OrderCode,
            FeeConfigId = f.FeeConfigId,
            GrossAmount = f.GrossAmount,
            FeeRate = f.FeeRate,
            OriginalFeeAmount = f.OriginalFeeAmount,
            RefundAdjustmentAmount = f.RefundAdjustmentAmount,
            FeeAmount = f.FeeAmount,
            CalculatedAt = f.CalculatedAt,
            StatementId = f.StatementId
        }).ToList();

        return new ShopMonthlyPlatformFeeReportResponse
        {
            BillingYear = request.Year,
            BillingMonth = request.Month,
            OrderCount = report.OrderCount,
            TotalGrossAmount = report.TotalGrossAmount,
            TotalFeeAmount = report.TotalFeeAmount,
            UnbilledFeeAmount = report.UnbilledFeeAmount,
            Orders = PagedResult<ShopMonthlyPlatformFeeOrderResponse>.Create(
                orderItems,
                report.Orders.Page,
                report.Orders.PageSize,
                report.Orders.TotalItems)
        };
    }

    public async Task<ShopCurrentPlatformFeeRateResponse> GetShopCurrentPlatformFeeRateAsync(
        Guid shopId,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        await ValidateStoreOwnerAccessAsync(shopId, currentUserId, ct);

        var config = await _repository.GetActiveFeeConfigAsync(_timeProvider.GetUtcNow().UtcDateTime, ct);
        return new ShopCurrentPlatformFeeRateResponse
        {
            FeePercentage = config is null ? null : config.FeeRate * 100m,
            EffectiveFrom = config?.EffectiveFrom
        };
    }

    public async Task<PagedResult<PlatformFeeStatementSummaryResponse>> GetAdminStatementsAsync(
            GetAdminPlatformFeesRequest request,
            CancellationToken ct = default)
        {
            var paged = await _repository.GetAdminStatementsAsync(request, ct);
            var items = paged.Items.Select(s => new PlatformFeeStatementSummaryResponse(
                s.Id,
                s.ShopId,
                s.BillingYear,
                s.BillingMonth,
                s.Status,
                s.TotalFeeAmount,
                s.IssuedAt,
                s.DueAt,
                s.PaidAt,
                s.CreatedAt,
            s.ShopNameSnapshot ?? s.Shop?.Name,
            s.SnapshotCapturedAt.HasValue ? s.ShopLogoUrlSnapshot : s.Shop?.LogoUrl, s.PaymentPauseReason, s.PaymentPausedAt
            )).ToList();
            return PagedResult<PlatformFeeStatementSummaryResponse>.Create(
                items,
                paged.Page,
                paged.PageSize,
                paged.TotalItems);
        }
    // UC-80: View Fee Details (BR-092, BR-205)
    public async Task<PlatformFeeStatementDetailResponse> GetStatementDetailAsync(
        Guid statementId,
        Guid shopId,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        await ValidateStoreOwnerAccessAsync(shopId, currentUserId, ct);

        var statement = await _repository.GetStatementDetailAsync(statementId, ct);
        if (statement == null)
        {
            throw AppException.NotFound("Platform fee statement not found.");
        }

        if (statement.ShopId != shopId)
        {
            throw AppException.Forbidden("This platform fee statement does not belong to your shop.");
        }

        var orderItems = statement.OrderPlatformFees.Select(o => new OrderPlatformFeeItemResponse(
            o.Id,
            o.OrderId,
            o.GrossAmount,
            o.FeeRate,
            o.FeeAmount,
            o.CalculatedAt
        ) { FeeConfigId = o.FeeConfigId, RefundCreditAmount = -o.Adjustments.Sum(a => a.Amount) }).ToList();

        var adjustmentItems = statement.Adjustments.Select(a => new PlatformFeeAdjustmentItemResponse(
            a.Id,
            a.RefundId,
            a.Amount,
            a.Reason,
            a.CreatedAt
        )).ToList();

        var paymentItems = statement.Payments.Select(p => new PlatformFeePaymentItemResponse(
            p.Id,
            p.Amount,
            p.PaymentMethod,
            p.GatewayTransactionId,
            p.Status,
            p.PaidAt,
            p.CreatedAt
        )).ToList();
        var historyItems = statement.Histories
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new PlatformFeeStatementHistoryItemResponse(
                h.Id,
                h.StatementId,
                h.ChangeType,
                h.PreviousAmount,
                h.ChangeAmount,
                h.NewAmount,
                h.Reason,
                h.RelatedRefundId,
                h.RelatedOrderId,
                h.CreatedAt
            )).ToList();

        return new PlatformFeeStatementDetailResponse(
            statement.Id,
            statement.ShopId,
            statement.ShopNameSnapshot ?? statement.Shop.Name,
            statement.BillingYear,
            statement.BillingMonth,
            statement.Status,
            statement.TotalFeeAmount,
            statement.IssuedAt,
            statement.DueAt,
            statement.PaidAt,
            statement.CreatedAt,
            orderItems,
            adjustmentItems,
            paymentItems,
            historyItems,
            statement.SnapshotCapturedAt.HasValue ? statement.ShopLogoUrlSnapshot : statement.Shop?.LogoUrl, statement.PaymentPauseReason, statement.PaymentPausedAt
        ) { ShopAddress = statement.ShopAddressSnapshot };
    }

     public async Task<PlatformFeeStatementDetailResponse> GetAdminStatementDetailAsync(
            Guid statementId,
            CancellationToken ct = default)
        {
            var statement = await _repository.GetStatementDetailAsync(statementId, ct);
            if (statement == null)
            {
                throw AppException.NotFound("Platform fee statement not found.");
            }
            var orderItems = statement.OrderPlatformFees.Select(o => new OrderPlatformFeeItemResponse(
                o.Id,
                o.OrderId,
                o.GrossAmount,
                o.FeeRate,
                o.FeeAmount,
                o.CalculatedAt
            ) { FeeConfigId = o.FeeConfigId, RefundCreditAmount = -o.Adjustments.Sum(a => a.Amount) }).ToList();
            var adjustmentItems = statement.Adjustments.Select(a => new PlatformFeeAdjustmentItemResponse(
                a.Id,
                a.RefundId,
                a.Amount,
                a.Reason,
                a.CreatedAt
            )).ToList();
            var paymentItems = statement.Payments.Select(p => new PlatformFeePaymentItemResponse(
                p.Id,
                p.Amount,
                p.PaymentMethod,
                p.GatewayTransactionId,
                p.Status,
                p.PaidAt,
                p.CreatedAt
            )).ToList();
            var historyItems = statement.Histories
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new PlatformFeeStatementHistoryItemResponse(
                    h.Id,
                    h.StatementId,
                    h.ChangeType,
                    h.PreviousAmount,
                    h.ChangeAmount,
                    h.NewAmount,
                    h.Reason,
                    h.RelatedRefundId,
                    h.RelatedOrderId,
                    h.CreatedAt
                )).ToList();
            return new PlatformFeeStatementDetailResponse(
                statement.Id,
                statement.ShopId,
                statement.ShopNameSnapshot ?? statement.Shop?.Name ?? "Shop",
                statement.BillingYear,
                statement.BillingMonth,
                statement.Status,
                statement.TotalFeeAmount,
                statement.IssuedAt,
                statement.DueAt,
                statement.PaidAt,
                statement.CreatedAt,
                orderItems,
                adjustmentItems,
                paymentItems,
                historyItems, statement.SnapshotCapturedAt.HasValue ? statement.ShopLogoUrlSnapshot : statement.Shop?.LogoUrl, statement.PaymentPauseReason, statement.PaymentPausedAt
            ) { ShopAddress = statement.ShopAddressSnapshot };
        }
    // UC-81: Pay Platform Fee (BR-092, BR-094, BR-097)
    public async Task<PlatformFeePaymentLinkResponse> CreatePaymentLinkAsync(
        Guid statementId, Guid shopId, Guid currentUserId, CancellationToken ct = default)
    {
        await ValidateStoreOwnerAccessAsync(shopId, currentUserId, ct);
        // Keep configuration changes serialized, but commit the payment intent BEFORE contacting payOS.
        // A session lock spans those short transactions without losing attempts on network cancellation.
        await _db.Database.OpenConnectionAsync(ct);
        var locked = false;
        PlatformFeePayment? createdAttempt = null;
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(728194602)", ct);
            locked = true;
            try
            {
                return await CreatePaymentLinkCoreAsync(statementId, shopId, currentUserId, p => createdAttempt = p, ct);
            }
            catch (AppException ex) when (ex.ErrorCode is ErrorCodes.PaymentCredentialsInvalid or ErrorCodes.PlatformPaymentUnavailable)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                if (createdAttempt != null && ex is PayOsCredentialException { RequestRejected: true })
                {
                    createdAttempt.Status = PaymentStatus.Failed;
                    createdAttempt.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
                }
                if (ex.ErrorCode == ErrorCodes.PaymentCredentialsInvalid)
                {
                    var config = await _db.PlatformPayOsConfigs.SingleOrDefaultAsync(ct);
                    if (config != null) config.IsInvalid = true;
                }
                await _availability.PauseOutstandingAsync(ex.ErrorCode == ErrorCodes.PaymentCredentialsInvalid
                    ? PlatformFeePaymentAvailability.InvalidConfiguration : PlatformFeePaymentAvailability.MissingConfiguration, ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                throw AppException.ServiceUnavailable("Platform fee payment is temporarily unavailable due to a system configuration issue. Your deadline is paused; you will be notified when payment is restored.");
            }
        }
        finally
        {
            try
            {
                if (locked) await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(728194602)", CancellationToken.None);
            }
            finally { await _db.Database.CloseConnectionAsync(); }
        }
    }

    private async Task<PlatformFeePaymentLinkResponse> CreatePaymentLinkCoreAsync(
        Guid statementId,
        Guid shopId,
        Guid currentUserId,
        Action<PlatformFeePayment> onCreated,
        CancellationToken ct = default)
    {
        await ValidateStoreOwnerAccessAsync(shopId, currentUserId, ct);

        var statement = await _repository.GetStatementDetailAsync(statementId, ct);
        if (statement == null)
        {
            throw AppException.NotFound("Platform fee statement not found.");
        }

        if (statement.ShopId != shopId)
        {
            throw AppException.Forbidden("This platform fee statement does not belong to your shop.");
        }

        // BR-094: Only an Issued or Overdue fee statement can enter in-app payment
        if (statement.Status != PlatformFeeStatementStatus.Issued && statement.Status != PlatformFeeStatementStatus.Overdue)
        {
            throw AppException.BadRequest("Only an Issued or Overdue fee statement can enter in-app payment.");
        }

        if (statement.TotalFeeAmount <= 0)
        {
            throw AppException.BadRequest("The amount payable must be greater than 0.");
        }

        if (!await _availability.IsReadyAsync(ct))
            throw new AppException("Platform payOS is unavailable.", ErrorCodes.PlatformPaymentUnavailable);
        if (statement.PaymentPauseReason != null)
            throw AppException.ServiceUnavailable("Payment restoration is being processed. Please retry shortly.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var paymentProvider = _configuration["Checkout:PaymentProvider"] ?? "PayOS";
        var credentials = paymentProvider == "PayOS" ? await _payOsConfig.GetCredentialsAsync(ct) : null;
        var previous = statement.Payments.Where(p => p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (previous != null && credentials != null && previous.GatewayTransactionId != null)
        {
            var previousCredentials = previous.ProtectedPayOsCredentials == null ? credentials
                : _credentialProtector.Unprotect(previous.ProtectedPayOsCredentials);
            System.Text.Json.JsonElement previousData;
            try
            {
                previousData = await _payOsApi.SendAsync(HttpMethod.Get,
                    $"v2/payment-requests/{previous.GatewayTransactionId}", previousCredentials, null, ct);
            }
            catch (AppException ex) when (ex.ErrorCode == ErrorCodes.PaymentCredentialsInvalid && previousCredentials != credentials)
            {
                if (previousCredentials.ClientId != credentials.ClientId)
                    throw AppException.ServiceUnavailable("A previous payment attempt could not be reconciled. Please contact support.");
                // Same channel, rotated signing/access key: query with the newly verified credentials.
                previousData = await _payOsApi.SendAsync(HttpMethod.Get,
                    $"v2/payment-requests/{previous.GatewayTransactionId}", credentials, null, ct);
            }
            var state = previousData.GetProperty("status").GetString();
            if (state != "NOT_FOUND" && (!previousData.TryGetProperty("orderCode", out var previousCode)
                || previousCode.GetInt64().ToString() != previous.GatewayTransactionId
                || !previousData.TryGetProperty("amount", out var previousAmount) || previousAmount.GetDecimal() != previous.Amount))
                throw AppException.ServiceUnavailable("The payment provider returned mismatched payment data.");
            if (state == "PENDING")
            {
                var linkId = previousData.GetProperty("id").GetString();
                var checkoutUrlToReturn = previous.GatewayCheckoutUrl ?? $"https://pay.payos.vn/web/{Uri.EscapeDataString(linkId!)}";
                return new PlatformFeePaymentLinkResponse(previous.Id, statement.Id, previous.Amount,
                    long.Parse(previous.GatewayTransactionId), checkoutUrlToReturn,
                    previous.GatewayQrCode ?? string.Empty, previous.GatewayExpiresAt ?? statement.DueAt ?? now.AddDays(1));
            }
            if (state == "PAID")
            {
                if (!previousData.TryGetProperty("amountPaid", out var collected) || !collected.TryGetDecimal(out var collectedAmount))
                    throw AppException.ServiceUnavailable("The payment amount could not be verified. Please retry.");
                await using (var settlement = await _db.Database.BeginTransactionAsync(ct))
                {
                    var reactivatedShopId = await SettleFeePaymentAsync(previous, collectedAmount, ct);
                    await settlement.CommitAsync(ct);
                    if (reactivatedShopId.HasValue)
                        await _shopStatusEvents.NotifyChangedAsync(reactivatedShopId.Value, ShopStatus.Active.ToString(), ct);
                }
                throw AppException.Conflict("Your payment has been confirmed. Please refresh the statement.");
            }
            if (state is not ("CANCELLED" or "EXPIRED" or "NOT_FOUND"))
                throw AppException.ServiceUnavailable("The previous payment is still being verified. Please retry.");
            previous.Status = PaymentStatus.Cancelled;
            previous.UpdatedAt = now;
        }
        var orderCode = (BitConverter.ToInt64(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)) & 0x000FFFFFFFFFFFFF) + 1;
        var orderCodeStr = orderCode.ToString();

        var payment = new PlatformFeePayment
        {
            Id = Guid.NewGuid(),
            StatementId = statement.Id,
            ShopId = shopId,
            Amount = statement.TotalFeeAmount,
            PaymentMethod = PaymentMethod.OnlinePayment,
            ProtectedPayOsCredentials = credentials == null ? null : _credentialProtector.Protect(credentials),
            GatewayTransactionId = orderCodeStr,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        onCreated(payment);
        await _repository.AddPaymentAsync(payment, ct);
        await _repository.SaveChangesAsync(ct);

        string checkoutUrl;
        string qrCode;
        // Ensure expiration is always in the future (especially if statement is Overdue)
        var expiresAtTime = statement.DueAt.HasValue && statement.DueAt.Value > now
            ? statement.DueAt.Value
            : now.AddDays(TimingConfiguration.PlatformFees.PaymentLinkGraceDays); // Give 24h grace window for overdue payment links
        var expiredAt = new DateTimeOffset(expiresAtTime).ToUnixTimeSeconds();


        if (paymentProvider == "PayOS")
        {
            var checksumKey = credentials!.ChecksumKey;
            var frontend = _configuration["Checkout:FrontendBaseUrl"] ?? "http://localhost:5173";
            var returnUrl = $"{frontend.TrimEnd('/')}/store-owner/platform-fees";
            var cancelUrl = $"{frontend.TrimEnd('/')}/store-owner/platform-fees?cancelled=1";
            var amount = (long)statement.TotalFeeAmount;
            
            var description = $"FEE{orderCode % 10000000:0000000}";
            var signatureData = FormattableString.Invariant($"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}");
            var signature = PayOsCheckoutGateway.Sign(signatureData, checksumKey);
            var payload = new
            {
                orderCode,
                amount,
                description,
                cancelUrl,
                returnUrl,
                expiredAt,
                signature
            };
            var data = await _payOsApi.SendAsync(HttpMethod.Post, "v2/payment-requests", credentials, payload, ct);
            if (!data.TryGetProperty("orderCode", out var actualCode) || actualCode.GetInt64() != orderCode
                || !data.TryGetProperty("amount", out var actualAmount) || actualAmount.GetDecimal() != payment.Amount)
                throw AppException.ServiceUnavailable("The payment provider returned mismatched payment data.");
            checkoutUrl = data.TryGetProperty("checkoutUrl", out var checkoutProp) && checkoutProp.ValueKind == JsonValueKind.String
                ? checkoutProp.GetString()! : string.Empty;
            if (!Uri.TryCreate(checkoutUrl, UriKind.Absolute, out var checkoutUri) || checkoutUri.Scheme != "https"
                || !string.IsNullOrEmpty(checkoutUri.UserInfo)
                || !(checkoutUri.Host == "payos.vn" || checkoutUri.Host.EndsWith(".payos.vn", StringComparison.OrdinalIgnoreCase)))
                throw AppException.ServiceUnavailable("The payment provider returned an invalid checkout URL.");
            qrCode = SaveBite.Backend.Helpers.PlatformFeePaymentQr.RequireProviderQr(data);
        }
        else if (paymentProvider == "Simulated")
        {
            checkoutUrl = string.Empty;
            qrCode = string.Empty; // Simulation never provides a real bank payment QR.
        }
        else throw AppException.ServiceUnavailable("Unsupported platform fee payment provider.");
        payment.GatewayCheckoutUrl = checkoutUrl;
        payment.GatewayQrCode = qrCode;
        payment.GatewayExpiresAt = expiresAtTime;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Created platform fee payment link for statement {StatementId}, amount: {Amount}, mode: {Mode}", statement.Id, statement.TotalFeeAmount, paymentProvider);

        return new PlatformFeePaymentLinkResponse(
            payment.Id,
            statement.Id,
            payment.Amount,
            orderCode,
            checkoutUrl,
            qrCode,
            expiresAtTime
        ) { IsSimulated = paymentProvider == "Simulated" };
    }

    // BR-097, BR-167, BR-195: Reconcile pending platform fee payments against authoritative provider data
    public async Task ReconcilePendingPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var payment = await _db.PlatformFeePayments
            .Include(p => p.Statement)
            .Include(p => p.Shop)
            .SingleOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment == null || payment.Status != PaymentStatus.Pending || string.IsNullOrEmpty(payment.GatewayTransactionId))
            return;

        var paymentProvider = _configuration["Checkout:PaymentProvider"] ?? "PayOS";
        if (paymentProvider == "Simulated")
        {
            if (payment.GatewayExpiresAt.HasValue && payment.GatewayExpiresAt.Value <= now)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                payment.Status = PaymentStatus.Expired;
                payment.GatewayCheckoutUrl = null;
                payment.GatewayQrCode = null;
                payment.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            return;
        }

        if (paymentProvider != "PayOS") return;

        if (!await _availability.IsReadyAsync(ct))
        {
            _logger.LogWarning("Skipping platform fee payment reconciliation for {PaymentId}: Platform PayOS is not ready.", paymentId);
            return;
        }

        PlatformPayOsCredentials credentials;
        try
        {
            credentials = await _payOsConfig.GetCredentialsAsync(ct);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Could not get platform PayOS credentials to reconcile payment {PaymentId}.", paymentId);
            return;
        }

        var paymentCredentials = payment.ProtectedPayOsCredentials == null ? credentials
            : _credentialProtector.Unprotect(payment.ProtectedPayOsCredentials);

        JsonElement data;
        try
        {
            data = await _payOsApi.SendAsync(HttpMethod.Get,
                $"v2/payment-requests/{payment.GatewayTransactionId}", paymentCredentials, null, ct);
        }
        catch (AppException ex) when (ex.ErrorCode == ErrorCodes.PaymentCredentialsInvalid && paymentCredentials != credentials)
        {
            try
            {
                data = await _payOsApi.SendAsync(HttpMethod.Get,
                    $"v2/payment-requests/{payment.GatewayTransactionId}", credentials, null, ct);
            }
            catch (AppException innerEx)
            {
                _logger.LogWarning(innerEx, "Failed to reconcile platform fee payment {PaymentId} with rotated PayOS credentials.", paymentId);
                return;
            }
        }
        catch (AppException ex)
        {
            // Do not expire based only on time when provider communication fails. Keep pending and retry.
            _logger.LogWarning(ex, "Failed to communicate with PayOS to reconcile platform fee payment {PaymentId}. Status left as Pending.", paymentId);
            return;
        }

        if (!data.TryGetProperty("status", out var statusProp) || statusProp.ValueKind != JsonValueKind.String)
            return;

        var state = statusProp.GetString();
        if (string.IsNullOrEmpty(state)) return;

        if (state != "NOT_FOUND" && (!data.TryGetProperty("orderCode", out var actualCode)
            || actualCode.GetInt64().ToString() != payment.GatewayTransactionId
            || !data.TryGetProperty("amount", out var actualAmount) || actualAmount.GetDecimal() != payment.Amount))
        {
            _logger.LogWarning("PayOS returned mismatched payment data for platform fee payment {PaymentId}.", paymentId);
            return;
        }

        if (state == "PAID")
        {
            var collectedAmount = data.TryGetProperty("amountPaid", out var paidProp) && paidProp.TryGetDecimal(out var ap)
                ? ap
                : payment.Amount;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);

            var freshPayment = await _db.PlatformFeePayments
                .Include(p => p.Statement)
                .Include(p => p.Shop)
                .SingleOrDefaultAsync(p => p.Id == paymentId, ct);

            if (freshPayment != null && freshPayment.Status == PaymentStatus.Pending)
            {
                var reactivatedShopId = await SettleFeePaymentAsync(freshPayment, collectedAmount, ct);
                await tx.CommitAsync(ct);
                await PublishPaidFeeAsync(freshPayment.StatementId, ct);
                if (reactivatedShopId.HasValue)
                {
                    await _shopStatusEvents.NotifyChangedAsync(reactivatedShopId.Value, ShopStatus.Active.ToString(), ct);
                }
                _logger.LogInformation("Reconciled platform fee payment {PaymentId} to PAID; statement {StatementId} settled.",
                    payment.Id, payment.StatementId);
            }
        }
        else if (state == "EXPIRED")
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);

            var freshPayment = await _db.PlatformFeePayments.SingleOrDefaultAsync(p => p.Id == paymentId, ct);
            if (freshPayment != null && freshPayment.Status == PaymentStatus.Pending)
            {
                freshPayment.Status = PaymentStatus.Expired;
                freshPayment.GatewayCheckoutUrl = null;
                freshPayment.GatewayQrCode = null;
                freshPayment.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                _logger.LogInformation("Reconciled platform fee payment {PaymentId} to EXPIRED per PayOS.", paymentId);
            }
        }
        else if (state == "CANCELLED")
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);

            var freshPayment = await _db.PlatformFeePayments.SingleOrDefaultAsync(p => p.Id == paymentId, ct);
            if (freshPayment != null && freshPayment.Status == PaymentStatus.Pending)
            {
                freshPayment.Status = PaymentStatus.Cancelled;
                freshPayment.GatewayCheckoutUrl = null;
                freshPayment.GatewayQrCode = null;
                freshPayment.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                _logger.LogInformation("Reconciled platform fee payment {PaymentId} to CANCELLED per PayOS.", paymentId);
            }
        }
        else if (state == "NOT_FOUND")
        {
            if (payment.CreatedAt <= now.AddMinutes(-15) || (payment.GatewayExpiresAt.HasValue && payment.GatewayExpiresAt.Value <= now))
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);

                var freshPayment = await _db.PlatformFeePayments.SingleOrDefaultAsync(p => p.Id == paymentId, ct);
                if (freshPayment != null && freshPayment.Status == PaymentStatus.Pending)
                {
                    freshPayment.Status = PaymentStatus.Cancelled;
                    freshPayment.GatewayCheckoutUrl = null;
                    freshPayment.GatewayQrCode = null;
                    freshPayment.UpdatedAt = now;
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    _logger.LogInformation("Platform fee payment {PaymentId} was NOT_FOUND on PayOS; marked Cancelled.", paymentId);
                }
            }
        }
        else if (state == "PENDING")
        {
            _logger.LogDebug("Platform fee payment {PaymentId} is still PENDING on PayOS.", paymentId);
        }
    }

    // UC-151: Update Platform Fee Payment Status via Webhook (BR-097, BR-167, BR-195)
    public async Task ProcessFeeWebhookAsync(JsonElement body, CancellationToken ct = default)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("orderCode", out var code)
            || code.ValueKind != JsonValueKind.Number || !code.TryGetInt64(out var orderCode))
            throw AppException.BadRequest("Invalid payment notification.");
        // confirm-webhook sends a sample while config saving holds the fee lock.
        // Acknowledge unknown codes before acquiring that lock to avoid a circular wait.
        var gatewayCode = orderCode.ToString();
        if (!await _db.PlatformFeePayments.AsNoTracking().AnyAsync(p => p.GatewayTransactionId == gatewayCode, ct))
            return;
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        var reactivatedShopId = await ProcessFeeWebhookCoreAsync(body, ct);
        await transaction.CommitAsync(ct);
        var statementId = await _db.PlatformFeePayments.AsNoTracking()
            .Where(p => p.GatewayTransactionId == gatewayCode).Select(p => (Guid?)p.StatementId).SingleOrDefaultAsync(ct);
        if (statementId.HasValue) await PublishPaidFeeAsync(statementId.Value, ct);
        if (reactivatedShopId.HasValue)
            await _shopStatusEvents.NotifyChangedAsync(reactivatedShopId.Value, ShopStatus.Active.ToString(), ct);
    }

    private async Task<Guid?> ProcessFeeWebhookCoreAsync(JsonElement body, CancellationToken ct = default)
    {
        var paymentProvider = _configuration["Checkout:PaymentProvider"] ?? "PayOS";
        if (body.ValueKind != JsonValueKind.Object
            || !body.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("orderCode", out var codeElement)
            || codeElement.ValueKind != JsonValueKind.Number
            || !codeElement.TryGetInt64(out var orderCode))
        {
            throw AppException.BadRequest("Invalid payment notification.");
        }

        var orderCodeStr = orderCode.ToString();
        var payment = await _repository.GetPaymentByGatewayCodeAsync(orderCodeStr, ct);
        if (payment == null)
        {
            _logger.LogWarning("Platform fee payment not found for gateway orderCode {OrderCode}.", orderCode);
            return null;
        }

        if (paymentProvider == "PayOS")
        {
            var credentials = payment.ProtectedPayOsCredentials == null ? await _payOsConfig.GetCredentialsAsync(ct)
                : _credentialProtector.Unprotect(payment.ProtectedPayOsCredentials);
            if (!PayOsCheckoutGateway.VerifyWebhookData(body, credentials.ChecksumKey))
            {
                var current = await _payOsConfig.GetCredentialsAsync(ct);
                if (current.ClientId != credentials.ClientId || !PayOsCheckoutGateway.VerifyWebhookData(body, current.ChecksumKey))
                    throw AppException.BadRequest("Invalid webhook signature.");
            }
        }
        if (!body.TryGetProperty("code", out var resultCode) || resultCode.GetString() != "00"
            || !data.TryGetProperty("code", out var dataCode) || dataCode.GetString() != "00") return null;
        if (!data.TryGetProperty("amount", out var verifiedAmount) || !verifiedAmount.TryGetDecimal(out var receivedAmount)
            || receivedAmount < payment.Amount || receivedAmount < payment.Statement.TotalFeeAmount)
            throw AppException.BadRequest("Payment amount does not match statement fee.");

        return await SettleFeePaymentAsync(payment, receivedAmount, ct);
    }

    // Shared by signed webhooks and authoritative provider polling after an uncertain create attempt.
    private async Task<Guid?> SettleFeePaymentAsync(PlatformFeePayment payment, decimal receivedAmount, CancellationToken ct)
    {
        if (payment.Status == PaymentStatus.Succeeded || payment.Statement.Status == PlatformFeeStatementStatus.Paid)
        {
            await NotifyFeePaidAsync(payment.StatementId, ct);
            return null;
        }
        if (receivedAmount < payment.Amount || receivedAmount < payment.Statement.TotalFeeAmount)
            throw AppException.BadRequest("Payment amount does not match statement fee.");
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // BR-097 & BR-167: Successful payment records against exactly one fee statement and marks it Paid
        // Cancel older attempts first: the database still sees this tracked payment as Pending until save.
        await _repository.CancelPendingPaymentsForStatementAsync(payment.StatementId, ct);
        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = now;
        payment.UpdatedAt = now;


        if (payment.Statement != null)
        {
            payment.Statement.PaymentPauseReason = null;
            payment.Statement.PaymentPausedAt = null;
            payment.Statement.Status = PlatformFeeStatementStatus.Paid;
            payment.Statement.PaidAt = now;
            payment.Statement.UpdatedAt = now;
        }

        // BR-195: A Suspended Shop is reactivated only if all overdue fee statements are settled
        var hasOtherOverdue = await _repository.HasOtherOverdueStatementsAsync(payment.ShopId, payment.StatementId, ct);
        var reactivatedShopId = (Guid?)null;
        if (!hasOtherOverdue && payment.Shop != null && payment.Shop.Status == ShopStatus.Suspended && payment.Shop.SuspendedForPlatformFees)
        {
            payment.Shop.SuspendedForPlatformFees = false;
            payment.Shop.SuspendedForOverdueRefunds = await _db.Refunds.AnyAsync(refund => refund.Order.ShopId == payment.ShopId
                && refund.RefundType == RefundType.ShopCancellation && refund.DestinationConfirmedAt != null
                && refund.RequestedAmount > refund.TransferredAmount && (refund.Status == RefundStatus.Overdue
                    || (refund.Status == RefundStatus.AwaitingTransfer && refund.ReviewDeadlineAt <= now)), ct);
            payment.Shop.UpdatedAt = now;
            if (ShopSuspensionRules.ReactivateIfResolved(payment.Shop)) reactivatedShopId = payment.Shop.Id;
            if (reactivatedShopId.HasValue)
                _logger.LogInformation("Shop {ShopId} reactivated to Active after full platform fee settlement.", payment.Shop.Id);
        }
        else if (hasOtherOverdue)
        {
            _logger.LogInformation("Shop {ShopId} remains Suspended because other overdue fee statements remain unsettled.", payment.ShopId);
        }

        await _repository.SaveChangesAsync(ct);
        await NotifyFeePaidAsync(payment.StatementId, ct);
        _logger.LogInformation("Platform fee statement {StatementId} marked Paid.", payment.StatementId);
        return reactivatedShopId;
    }

    public async Task AccrueOrderPlatformFeeAsync(Guid orderId, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await RefundEventEffects.LockOrderAsync(_db, orderId, ct);
        await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        var order = await _db.Orders.Include(o => o.Refunds).SingleAsync(o => o.Id == orderId, ct);
        if (order.Status != OrderStatus.Completed) return;
        if (!await _db.OrderPlatformFees.AnyAsync(f => f.OrderId == orderId, ct))
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var asOf = order.CompletedAt ?? now;
            var config = await _repository.GetActiveFeeConfigAsync(asOf, ct)
                ?? throw new InvalidOperationException("An effective platform fee configuration is required.");
            _db.Add(new OrderPlatformFee { Id = Guid.NewGuid(), OrderId = order.Id, ShopId = order.ShopId,
                FeeConfigId = config.Id, GrossAmount = order.TotalAmount, FeeRate = config.FeeRate,
                FeeAmount = Math.Round(order.TotalAmount * config.FeeRate, 2, MidpointRounding.AwayFromZero),
                CalculatedAt = asOf, CreatedAt = now, UpdatedAt = now });
            await _db.SaveChangesAsync(ct);
        }
        await RefundEventEffects.ReconcileFeesAsync(_db, order, _timeProvider.GetUtcNow().UtcDateTime, ct);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    // BR-095, BR-193: Automatically mark overdue statements and suspend delinquent shops
    public async Task<int> CheckAndSuspendOverdueShopsAsync(CancellationToken ct = default, Guid? initiatedByAdminId = null)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        var (suspendedCount, suspendedShopIds) = await CheckAndSuspendOverdueShopsCoreAsync(ct);
        if (initiatedByAdminId.HasValue)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            _audit.Record(new AuditLog
            {
                ActorUserId = initiatedByAdminId,
                Action = AuditActionTypes.ProcessOverduePlatformFees,
                TargetType = "PlatformFeeOverdueProcessingRun",
                TargetId = Guid.NewGuid(),
                NewValuesJson = JsonSerializer.Serialize(new { SuspendedShopCount = suspendedCount, TriggeredAt = now }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);

        var suspendedFeeShopIds = await _db.Shops.AsNoTracking()
            .Where(shop => shop.Status == ShopStatus.Suspended && shop.SuspendedForPlatformFees)
            .Select(shop => shop.Id)
            .ToListAsync(ct);
        foreach (var shopId in suspendedShopIds)
            await _shopStatusEvents.NotifyChangedAsync(shopId, ShopStatus.Suspended.ToString(), ct);

        foreach (var shopId in suspendedFeeShopIds)
            await _suspensionCleanup.CleanupAsync(shopId, ct);

        return suspendedCount;
    }

    private async Task<(int Count, IReadOnlyList<Guid> ShopIds)> CheckAndSuspendOverdueShopsCoreAsync(CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await _availability.IsReadyAsync(ct))
        {
            await _availability.PauseOutstandingAsync(PlatformFeePaymentAvailability.MissingConfiguration, ct);
            return (0, Array.Empty<Guid>());
        }
        var overdueStatements = await _repository.GetOverdueStatementsAsync(now, ct);
        var suspendedCount = 0;
        var suspendedShopIds = new List<Guid>();

        foreach (var statement in overdueStatements)
        {
            statement.Status = PlatformFeeStatementStatus.Overdue;
            statement.UpdatedAt = now;

            if (statement.Shop != null)
            {
                var newlySuspended = statement.Shop.Status != ShopStatus.Suspended;
                statement.Shop.Status = ShopStatus.Suspended;
                statement.Shop.SuspendedForPlatformFees = true;
                statement.Shop.UpdatedAt = now;
                if (newlySuspended)
                {
                    suspendedCount++;
                    suspendedShopIds.Add(statement.Shop.Id);
                }
                _logger.LogWarning("Shop {ShopId} suspended due to overdue platform fee statement {StatementId}.", statement.ShopId, statement.Id);
            }
        }

        if (overdueStatements.Count > 0)
        {
            await _repository.SaveChangesAsync(ct);
        }

        await NotifyFeeDeadlinesAsync(ct);
        return (suspendedCount, suspendedShopIds);
    }

    // BR-093, BR-192: Aggregate unbilled order fees into monthly fee statements.
    public async Task<int> GenerateMonthlyStatementsAsync(
        int billingYear, short billingMonth, CancellationToken ct = default, Guid? initiatedByAdminId = null)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        var result = await GenerateMonthlyStatementsCoreAsync(billingYear, billingMonth, ct);
        if (!await _availability.IsReadyAsync(ct))
            await _availability.PauseOutstandingAsync(PlatformFeePaymentAvailability.MissingConfiguration, ct);
        await NotifyMonthlyStatementsAsync(billingYear, billingMonth, ct);
        if (initiatedByAdminId.HasValue)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            _audit.Record(new AuditLog
            {
                ActorUserId = initiatedByAdminId,
                Action = AuditActionTypes.GenerateMonthlyPlatformFeeStatements,
                TargetType = "PlatformFeeStatementGenerationRun",
                TargetId = Guid.NewGuid(),
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    BillingYear = billingYear,
                    BillingMonth = billingMonth,
                    StatementCount = result,
                    TriggeredAt = now
                }),
                CreatedAt = now
            });
            await _db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<int> GenerateMonthlyStatementsCoreAsync(
            int billingYear,
            short billingMonth,
            CancellationToken ct = default)
        {
            if (billingMonth < 1 || billingMonth > 12)
            {
                throw AppException.BadRequest("Billing month must be between 1 and 12.");
            }
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var paymentReady = await _availability.IsReadyAsync(ct);
            var unbilledFees = await _repository.GetUnbilledOrderFeesAsync(billingYear, billingMonth, ct);
            // Include earlier paid orders too: their refunds become credit for this bill.
            var endOfMonth = new DateTime(billingYear, billingMonth, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            var billedOrders = await _db.Orders.Include(o => o.Refunds)
                .Where(o => o.PlatformFee != null && o.PlatformFee.CalculatedAt < endOfMonth
                    && o.Refunds.Any(r => r.TransferredAmount > 0))
                .ToListAsync(ct);
            // Refund events may still be waiting in the outbox. Reconcile transferred
            // refunds before billing, using the same idempotent credits as the consumer.
            foreach (var order in unbilledFees.Select(f => f.Order).Concat(billedOrders).DistinctBy(o => o.Id))
            {
                if (order.Refunds.Any(r => r.TransferredAmount > 0))
                    await RefundEventEffects.ReconcileFeesAsync(_db, order, now, ct);
            }
            await _repository.SaveChangesAsync(ct);
            // Recover credits consumed by old zero-value bills without changing
            // settled amounts. Only the portion supported by actual fees stays assigned.
            var zeroBills = await _db.PlatformFeeStatements
                .Include(s => s.OrderPlatformFees).Include(s => s.Adjustments).AsSplitQuery()
                .Where(s => s.Status == PlatformFeeStatementStatus.Paid && s.TotalFeeAmount == 0
                    && (s.BillingYear < billingYear || (s.BillingYear == billingYear && s.BillingMonth <= billingMonth)))
                .ToListAsync(ct);
            foreach (var bill in zeroBills)
            {
                var capacity = bill.OrderPlatformFees.Sum(f => f.FeeAmount);
                foreach (var credit in bill.Adjustments.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id).ToList())
                {
                    credit.StatementId = null;
                    var applied = PlatformFeeCreditAllocator.Apply(credit, bill.Id, capacity);
                    if (applied == null) continue;
                    if (!ReferenceEquals(applied, credit)) _db.Add(applied);
                    capacity += applied.Amount;
                }
            }
            await _repository.SaveChangesAsync(ct);
            var unbilledAdjustments = await _repository.GetUnbilledAdjustmentsAsync(billingYear, billingMonth, ct);

            var unpaidShopIds = await _db.PlatformFeeStatements
                .Where(s => s.BillingYear == billingYear && s.BillingMonth == billingMonth
                    && s.Status != PlatformFeeStatementStatus.Paid)
                .Select(s => s.ShopId).ToListAsync(ct);
            var shopIds = unbilledFees.Select(f => f.ShopId)
                .Union(unbilledAdjustments.Select(a => a.ShopId))
                .Union(unpaidShopIds)
                .Distinct()
                .ToList();

            var statementsCreated = 0;
            foreach (var shopId in shopIds)
            {
                var shopFees = unbilledFees.Where(f => f.ShopId == shopId).ToList();
                var shopAdjustments = unbilledAdjustments.Where(a => a.ShopId == shopId).ToList();
                var statement = await _repository.GetShopStatementForPeriodAsync(shopId, billingYear, billingMonth, ct);
                if (statement?.Status == PlatformFeeStatementStatus.Paid)
                    continue; // Outstanding credit remains unassigned for a later bill.

                var isNew = statement == null;
                statement ??= new PlatformFeeStatement
                {
                    Id = Guid.NewGuid(), ShopId = shopId, BillingYear = billingYear,
                    BillingMonth = billingMonth, Status = PlatformFeeStatementStatus.Issued,
                    IssuedAt = now, CreatedAt = now, UpdatedAt = now,
                    DueAt = paymentReady ? now.AddDays(TimingConfiguration.PlatformFees.StatementPaymentDays) : null
                };
                if (isNew)
                {
                    var shop = await _db.Shops.SingleAsync(s => s.Id == shopId, ct);
                    statement.ShopNameSnapshot = shop.Name;
                    statement.ShopAddressSnapshot = string.Join(", ", new[] { shop.AddressLine, shop.Ward, shop.District, shop.City }.Where(v => !string.IsNullOrWhiteSpace(v)));
                    statement.ShopLogoUrlSnapshot = shop.LogoUrl;
                    statement.SnapshotCapturedAt = now;
                    await _repository.AddStatementAsync(statement, ct);
                }

                var previous = statement.TotalFeeAmount;
                foreach (var fee in shopFees)
                {
                    fee.StatementId = statement.Id;
                    fee.UpdatedAt = now;
                }
                // Rebuild unpaid bills from their ledger rather than incrementing a
                // potentially stale total. Pending fees above are not yet persisted.
                var gross = await _db.OrderPlatformFees
                    .Where(f => f.StatementId == statement.Id)
                    .SumAsync(f => (decimal?)f.FeeAmount, ct) ?? 0m;
                var assigned = await _db.Set<PlatformFeeAdjustment>()
                    .Where(a => a.StatementId == statement.Id)
                    .OrderBy(a => a.CreatedAt).ThenBy(a => a.Id).ToListAsync(ct);
                var available = gross + shopFees.Sum(f => f.FeeAmount);

                // Reallocate old credits as well, recovering any excess previously
                // swallowed by Max(0, ...). The remainder stays available next month.
                foreach (var credit in assigned) credit.StatementId = null;
                foreach (var credit in assigned.Concat(shopAdjustments).DistinctBy(a => a.Id)
                    .OrderBy(a => a.CreatedAt).ThenBy(a => a.Id))
                {
                    var applied = PlatformFeeCreditAllocator.Apply(credit, statement.Id, available);
                    if (applied == null) continue;
                    if (!ReferenceEquals(applied, credit)) _db.Add(applied);
                    available += applied.Amount;
                }
                statement.TotalFeeAmount = available;
                statement.UpdatedAt = now;
                if (available == 0)
                {
                    statement.Status = PlatformFeeStatementStatus.Paid;
                    statement.PaidAt = now;
                    statement.DueAt = null;
                    statement.PaymentPauseReason = null;
                    statement.PaymentPausedAt = null;
                }
                if (isNew || previous != available)
                    statement.Histories.Add(new PlatformFeeStatementHistory
                    {
                        Id = Guid.NewGuid(), StatementId = statement.Id,
                        PreviousAmount = previous, ChangeAmount = available - previous, NewAmount = available,
                        ChangeType = isNew ? StatementChangeType.InitialIssuance : StatementChangeType.PriorPeriodCreditApplied,
                        Reason = $"Reconciled statement from order fees and refund credits for {billingMonth:D2}/{billingYear}.",
                        CreatedAt = now
                    });
                if (available > 0 && !paymentReady)
                    await _availability.PauseAsync(statement, PlatformFeePaymentAvailability.MissingConfiguration, ct);
                if (isNew) statementsCreated++;
            }
            await _repository.SaveChangesAsync(ct);
            _logger.LogInformation("Generated {Count} monthly platform fee statements for period {Year}-{Month}.", statementsCreated, billingYear, billingMonth);
            return statementsCreated;
        }


    public async Task RepairNotificationsAsync(Guid statementId, CancellationToken ct = default)
    {
        var statement = await _repository.GetStatementDetailAsync(statementId, ct);
        if (statement == null || statement.Status == PlatformFeeStatementStatus.Draft) return;
        await _availability.NotifyPausedAsync(statement, ct);
        var issuedKey = $"platform-fee-statement:{statement.Id}:issued";
        if (!await _db.Notifications.AnyAsync(n => n.DeduplicationKey == issuedKey, ct))
            await NotifyMonthlyStatementsAsync(statement.BillingYear, statement.BillingMonth, ct);
        var paidKey = $"platform-fee:{statement.Id}:paid";
        if (statement.Status == PlatformFeeStatementStatus.Paid && !await _db.Notifications.AnyAsync(n => n.DeduplicationKey == paidKey, ct))
            await NotifyFeePaidAsync(statement.Id, ct);
    }

    private async Task TryNotifyAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct)
    {
        await _notifications.EnqueueAsync(target, command, ct);
    }

    private async Task NotifyFeePaidAsync(Guid statementId, CancellationToken ct)
    {
        var statement = await _repository.GetStatementDetailAsync(statementId, ct) ?? throw AppException.NotFound();
        await TryNotifyAsync(NotificationTarget.User(statement.Shop.OwnerUserId), new CreateNotificationCommand(
            NotificationType.PlatformFeePaid, NotificationMessages.PlatformFees.PaymentReceivedTitle,
            NotificationMessages.PlatformFees.PaymentReceivedBody(statement.ShopNameSnapshot ?? statement.Shop.Name, statement.BillingMonth, statement.BillingYear),
            NotificationActionUrls.StoreOwnerPlatformFee(statement.Id), $"platform-fee:{statement.Id}:paid"), ct);
    }

    private async Task NotifyFeeDeadlinesAsync(CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var statement in await _repository.GetUnpaidStatementsAsync(ct))
        {
            if (statement.DueAt > now.AddHours(TimingConfiguration.PlatformFees.DueReminderLeadHours)) continue;
            var overdue = statement.DueAt <= now;
            await TryNotifyAsync(NotificationTarget.User(statement.Shop.OwnerUserId), new CreateNotificationCommand(
                overdue ? NotificationType.PlatformFeeOverdue : NotificationType.PlatformFeeDueSoon,
                overdue ? NotificationMessages.PlatformFees.OverdueTitle : NotificationMessages.PlatformFees.DueSoonTitle,
                overdue ? NotificationMessages.PlatformFees.OverdueBody(statement.ShopNameSnapshot ?? statement.Shop.Name, statement.TotalFeeAmount, statement.DueAt)
                        : NotificationMessages.PlatformFees.DueSoonBody(statement.ShopNameSnapshot ?? statement.Shop.Name, statement.TotalFeeAmount, statement.DueAt),

                NotificationActionUrls.StoreOwnerPlatformFee(statement.Id), $"platform-fee:{statement.Id}:{(overdue ? "overdue" : "due-soon")}:revision:{statement.PaymentResumeRevision ?? 0}"), ct);
        }
    }

    // Re-read committed statements so a retry can recover a broker failure after billing succeeded.
    private async Task NotifyMonthlyStatementsAsync(int year, short month, CancellationToken ct)
    {
        var statements = await _repository.GetStatementsForPeriodAsync(year, month, ct);
        foreach (var statement in statements)
        {
            await _availability.NotifyPausedAsync(statement, ct);
            var paymentInfo = statement.TotalFeeAmount == 0 ? NotificationMessages.PlatformFees.PaymentNotRequired
                : statement.PaymentPauseReason != null ? "Payment is temporarily unavailable due to platform configuration. No payment deadline applies until payment is restored."
                : NotificationMessages.PlatformFees.PaymentRequiredInfo(statement.TotalFeeAmount, statement.DueAt);
            await TryNotifyAsync(NotificationTarget.User(statement.Shop.OwnerUserId), new CreateNotificationCommand(
                NotificationType.PlatformFeeStatementIssued, NotificationMessages.PlatformFees.MonthlyStatementIssuedTitle,
                NotificationMessages.PlatformFees.MonthlyStatementIssuedBody(statement.ShopNameSnapshot ?? statement.Shop.Name, month, year, paymentInfo),
                NotificationActionUrls.StoreOwnerPlatformFee(statement.Id), $"platform-fee-statement:{statement.Id}:issued"), ct);
        }
    }

    // BR-092: Only the Store Owner may access platform-fee statements belonging to their Shop
    private async Task ValidateStoreOwnerAccessAsync(Guid shopId, Guid currentUserId, CancellationToken ct)
    {
        var shop = await _repository.GetShopByIdAsync(shopId, ct);
        if (shop == null)
        {
            throw AppException.NotFound("Shop not found.");
        }

        if (shop.OwnerUserId != currentUserId)
        {
            throw AppException.Forbidden("Only the Store Owner may access the platform-fee statements belonging to their Shop.");
        }
    }
}
