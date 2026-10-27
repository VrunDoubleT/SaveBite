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
    private readonly IPlatformFeeStatementRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlatformFeeStatementService> _logger;
    // more
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public PlatformFeeStatementService(
        IPlatformFeeStatementRepository repository,
        TimeProvider timeProvider,
       // ILogger<PlatformFeeStatementService> logger)
        ILogger<PlatformFeeStatementService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
            s.CreatedAt
        )).ToList();

        return PagedResult<PlatformFeeStatementSummaryResponse>.Create(
            items,
            paged.Page,
            paged.PageSize,
            paged.TotalItems);
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
                s.Shop?.Name
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
        )).ToList();

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

        return new PlatformFeeStatementDetailResponse(
            statement.Id,
            statement.ShopId,
            statement.Shop.Name,
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
            paymentItems
        );
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
            )).ToList();
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
            return new PlatformFeeStatementDetailResponse(
                statement.Id,
                statement.ShopId,
                statement.Shop?.Name ?? "Shop",
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
                paymentItems
            );
        }
    // UC-81: Pay Platform Fee (BR-092, BR-094, BR-097)
    public async Task<PlatformFeePaymentLinkResponse> CreatePaymentLinkAsync(
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

        // BR-094: Only an Issued or Overdue fee statement can enter in-app payment
        if (statement.Status != PlatformFeeStatementStatus.Issued && statement.Status != PlatformFeeStatementStatus.Overdue)
        {
            throw AppException.BadRequest("Only an Issued or Overdue fee statement can enter in-app payment.");
        }

        if (statement.TotalFeeAmount <= 0)
        {
            throw AppException.BadRequest("The amount payable must be greater than 0.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        // Invalidate previous pending payment attempts for this statement so only one active attempt remains
        await _repository.CancelPendingPaymentsForStatementAsync(statement.Id, ct);
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000000;
        var orderCodeStr = orderCode.ToString();

        var payment = new PlatformFeePayment
        {
            Id = Guid.NewGuid(),
            StatementId = statement.Id,
            ShopId = shopId,
            Amount = statement.TotalFeeAmount,
            PaymentMethod = PaymentMethod.OnlinePayment,
            GatewayTransactionId = orderCodeStr,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddPaymentAsync(payment, ct);
        await _repository.SaveChangesAsync(ct);

        var paymentProvider = _configuration["Checkout:PaymentProvider"] ?? "PayOS";
        string checkoutUrl;
        string qrCode;
        // Ensure expiration is always in the future (especially if statement is Overdue)
        var expiresAtTime = statement.DueAt.HasValue && statement.DueAt.Value > now
            ? statement.DueAt.Value
            : now.AddDays(1); // Give 24h grace window for overdue payment links
        var expiredAt = new DateTimeOffset(expiresAtTime).ToUnixTimeSeconds();


        if (paymentProvider == "PayOS")
        {
            var clientId = _configuration["PlatformPayOS:ClientId"];
            var apiKey = _configuration["PlatformPayOS:ApiKey"];
            var checksumKey = _configuration["PlatformPayOS:ChecksumKey"];
            var frontend = _configuration["PlatformPayOS:FrontendBaseUrl"]
                           ?? _configuration["Checkout:FrontendBaseUrl"]
                           ?? "http://localhost:5173";
            var returnUrl = $"{frontend.TrimEnd('/')}/shop/platform-fees";
            var cancelUrl = $"{frontend.TrimEnd('/')}/shop/platform-fees?cancelled=1";
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(checksumKey))
            {
                throw AppException.ServiceUnavailable("Platform PayOS account is not configured. Please check PlatformPayOS in .env or switch Checkout__PaymentProvider to Simulated.");
            }
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
            var client = _httpClientFactory.CreateClient("PayOsCheckout");
            using var requestMsg = new HttpRequestMessage(HttpMethod.Post, "v2/payment-requests");
            requestMsg.Headers.Add("x-client-id", clientId);
            requestMsg.Headers.Add("x-api-key", apiKey);
            requestMsg.Content = JsonContent.Create(payload);
            using var response = await client.SendAsync(requestMsg, ct);
            using var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = jsonDoc.RootElement;
            if (!response.IsSuccessStatusCode || !root.TryGetProperty("code", out var code) || code.GetString() != "00")
            {
                var desc = root.TryGetProperty("desc", out var descProp) ? descProp.GetString() : "PayOS service error";
                _logger.LogError("PayOS fee payment creation failed for statement {StatementId}: {Desc}", statement.Id, desc);
                throw AppException.BadRequest($"Unable to create PayOS payment link: {desc}");
            }
            var data = root.GetProperty("data");
            if (!root.TryGetProperty("signature", out var respSig) || !PayOsCheckoutGateway.Verify(data, respSig.GetString(), checksumKey))
            {
                throw AppException.ServiceUnavailable("Invalid response signature from PayOS.");
            }
            checkoutUrl = data.TryGetProperty("checkoutUrl", out var checkoutProp) && checkoutProp.ValueKind == JsonValueKind.String
                ? checkoutProp.GetString()!
                : $"https://pay.payos.vn/web/{orderCodeStr}";
            qrCode = data.TryGetProperty("qrCode", out var qrProp) && qrProp.ValueKind == JsonValueKind.String
                ? qrProp.GetString()!
                : $"00020101021238540010A000000727012600069704220112{orderCodeStr}5303704540{statement.TotalFeeAmount:0}5802VN62070803SB6304";
        }
        else
        {
            // Simulated mode (Checkout:PaymentProvider == "Simulated")
            checkoutUrl = $"https://pay.payos.vn/web/{orderCodeStr}";
            qrCode = $"00020101021238540010A000000727012600069704220112{orderCodeStr}5303704540{statement.TotalFeeAmount:0}5802VN62070803SB6304";
        }
        _logger.LogInformation("Created platform fee payment link for statement {StatementId}, amount: {Amount}, mode: {Mode}", statement.Id, statement.TotalFeeAmount, paymentProvider);

        return new PlatformFeePaymentLinkResponse(
            payment.Id,
            statement.Id,
            payment.Amount,
            orderCode,
            checkoutUrl,
            qrCode,
            expiresAtTime
        );
    }

    // UC-151: Update Platform Fee Payment Status via Webhook (BR-097, BR-167, BR-195)
    public async Task ProcessFeeWebhookAsync(JsonElement body, CancellationToken ct = default)
    {
        var paymentProvider = _configuration["Checkout:PaymentProvider"] ?? "PayOS";
        if (paymentProvider == "PayOS")
        {
            var checksumKey = _configuration["PlatformPayOS:ChecksumKey"];
            if (!string.IsNullOrWhiteSpace(checksumKey))
            {
                if (!PayOsCheckoutGateway.VerifyWebhookData(body, checksumKey))
                {
                    _logger.LogWarning("Invalid PayOS webhook signature for platform fee.");
                    throw AppException.BadRequest("Invalid webhook signature.");
                }
            }
        }
        
        // Validate webhook success status code
        if (!body.TryGetProperty("code", out var codeProperty) || codeProperty.GetString() != "00")
        {
            _logger.LogWarning("PayOS webhook reported unsuccessful status code for platform fee notification: {Code}.", codeProperty.GetString());
            return;
        }
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
            return;
        }

        // Idempotency: duplicate webhooks must not process twice
        if (payment.Status == PaymentStatus.Succeeded)
        {
            return;
        }

        // Validate paid amount against required statement payment amount
        if (data.TryGetProperty("amount", out var amountElement) && amountElement.TryGetDecimal(out var paidAmount))
        {
            if (paidAmount < payment.Amount)
            {
                _logger.LogWarning("PayOS webhook paid amount {PaidAmount} is less than required payment {RequiredAmount} for orderCode {OrderCode}.",
                    paidAmount, payment.Amount, orderCode);
                throw AppException.BadRequest("Payment amount does not match statement fee.");
            }
        }
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // BR-097 & BR-167: Successful payment records against exactly one fee statement and marks it Paid
        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = now;
        payment.UpdatedAt = now;
        // Invalidate any other pending payment attempts for this statement
        await _repository.CancelPendingPaymentsForStatementAsync(payment.StatementId, ct);


        if (payment.Statement != null)
        {
            payment.Statement.Status = PlatformFeeStatementStatus.Paid;
            payment.Statement.PaidAt = now;
            payment.Statement.UpdatedAt = now;
        }

        // BR-195: A Suspended Shop is reactivated only if all overdue fee statements are settled
        var hasOtherOverdue = await _repository.HasOtherOverdueStatementsAsync(payment.ShopId, payment.StatementId, ct);
        if (!hasOtherOverdue && payment.Shop != null && payment.Shop.Status == ShopStatus.Suspended)
        {
            payment.Shop.Status = ShopStatus.Active;
            payment.Shop.UpdatedAt = now;
            _logger.LogInformation("Shop {ShopId} reactivated to Active after full platform fee settlement.", payment.Shop.Id);
        }
        else if (hasOtherOverdue)
        {
            _logger.LogInformation("Shop {ShopId} remains Suspended because other overdue fee statements remain unsettled.", payment.ShopId);
        }

        await _repository.SaveChangesAsync(ct);
        _logger.LogInformation("Platform fee statement {StatementId} marked Paid.", payment.StatementId);
    }

    // BR-093: Accrue platform fee on completed order revenue
    public async Task AccrueOrderPlatformFeeAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _repository.GetOrderByIdAsync(orderId, ct);
        if (order == null || order.Status != OrderStatus.Completed)
        {
            return;
        }
        // Check idempotency: If fee is already accrued for this order, do not duplicate
        if (await _repository.HasOrderPlatformFeeAsync(orderId, ct))
        {
            _logger.LogError("No active platform fee configuration found for completed order {OrderId}. Accrual failed.", orderId);
            throw AppException.InternalError("Platform fee configuration is not configured in the system.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var asOf = order.CompletedAt ?? now;
        var config = await _repository.GetActiveFeeConfigAsync(asOf, ct);
        if (config == null)
        {
            _logger.LogWarning("No active platform fee configuration found for completed order {OrderId}.", orderId);
            return;
        }

        var feeRate = config.FeeRate;
        var grossAmount = order.TotalAmount;
        var feeAmount = Math.Round(grossAmount * feeRate, 2);

        var orderFee = new OrderPlatformFee
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ShopId = order.ShopId,
            FeeConfigId = config.Id,
            StatementId = null,
            GrossAmount = grossAmount,
            FeeRate = feeRate,
            FeeAmount = feeAmount,
            CalculatedAt = asOf,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddOrderPlatformFeeAsync(orderFee, ct);
        await _repository.SaveChangesAsync(ct);
        _logger.LogInformation("Accrued platform fee {FeeAmount} on completed order {OrderId} (Rate: {Rate}).", feeAmount, orderId, feeRate);
    }

    // BR-095, BR-193: Automatically mark overdue statements and suspend delinquent shops
    public async Task<int> CheckAndSuspendOverdueShopsAsync(CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var overdueStatements = await _repository.GetOverdueStatementsAsync(now, ct);
        var suspendedCount = 0;

        foreach (var statement in overdueStatements)
        {
            statement.Status = PlatformFeeStatementStatus.Overdue;
            statement.UpdatedAt = now;

            if (statement.Shop != null && statement.Shop.Status == ShopStatus.Active)
            {
                statement.Shop.Status = ShopStatus.Suspended;
                statement.Shop.UpdatedAt = now;
                suspendedCount++;
                _logger.LogWarning("Shop {ShopId} suspended due to overdue platform fee statement {StatementId}.", statement.ShopId, statement.Id);
            }
        }

        if (overdueStatements.Count > 0)
        {
            await _repository.SaveChangesAsync(ct);
        }

        return suspendedCount;
    }

    // BR-096, BR-194, BR-195: Admin records external payment reconciliation and reactivates shop
    public async Task RecordExternalPaymentAsync(
        Guid adminId,
        Guid statementId,
        RecordExternalFeePaymentRequest request,
        CancellationToken ct = default)
    {
        if (adminId == Guid.Empty)
        {
            throw AppException.Forbidden("Only an Administrator may record external payment reconciliation.");
        }

        if (request.Amount <= 0)
        {
            throw AppException.BadRequest("Payment amount must be greater than zero.");
        }

        var statement = await _repository.GetStatementDetailAsync(statementId, ct);
        if (statement == null)
        {
            throw AppException.NotFound("Platform fee statement not found.");
        }
        
        if (statement.Status == PlatformFeeStatementStatus.Paid)
        {
            throw AppException.BadRequest("This platform fee statement is already paid and settled.");
        }

        if (request.Amount < statement.TotalFeeAmount)
        {
            throw AppException.BadRequest($"Payment amount ({request.Amount:N0} VND) cannot be less than total fee amount ({statement.TotalFeeAmount:N0} VND).");
        }
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var refCode = string.IsNullOrWhiteSpace(request.ReferenceCode)
            ? $"EXT-{Guid.NewGuid():N}"[..20]
            : request.ReferenceCode.Trim();
        var existingPayment = await _repository.GetPaymentByGatewayCodeAsync(refCode, ct);
        if (existingPayment != null)
        {
            throw AppException.Conflict("Reconciliation reference code already exists in the system.");
        }
        // Cancel any pending payment attempts for this statement
        await _repository.CancelPendingPaymentsForStatementAsync(statement.Id, ct);
        var payment = new PlatformFeePayment
        {
            Id = Guid.NewGuid(),
            StatementId = statement.Id,
            ShopId = statement.ShopId,
            Amount = request.Amount,
            PaymentMethod = PaymentMethod.PayAtShop,
            GatewayTransactionId = refCode,
            Status = PaymentStatus.Succeeded,
            PaidAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddPaymentAsync(payment, ct);

        // BR-097 & BR-194: Record against statement and mark Paid
        statement.Status = PlatformFeeStatementStatus.Paid;
        statement.PaidAt = now;
        statement.UpdatedAt = now;

        // BR-195: A Suspended Shop is reactivated only if all overdue fee statements are settled
        var hasOtherOverdue = await _repository.HasOtherOverdueStatementsAsync(statement.ShopId, statement.Id, ct);
        if (!hasOtherOverdue && statement.Shop != null && statement.Shop.Status == ShopStatus.Suspended)
        {
            statement.Shop.Status = ShopStatus.Active;
            statement.Shop.UpdatedAt = now;
            _logger.LogInformation("Shop {ShopId} reactivated to Active after external payment reconciliation by Admin {AdminId}.", statement.ShopId, adminId);
        }
        else if (hasOtherOverdue)
        {
            _logger.LogInformation("Shop {ShopId} remains Suspended because other overdue fee statements remain unsettled.", statement.ShopId);
        }
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = adminId,
            ActorType = AuditActorType.User,
            Action = "RecordExternalPlatformFeePayment",
            TargetType = "PlatformFeeStatement",
            TargetId = statement.Id,
            NewValuesJson = JsonSerializer.Serialize(new { Amount = request.Amount, ReferenceCode = refCode, Note = request.Note }),
            CreatedAt = now
        };
        _repository.AddAuditLog(audit);

        await _repository.SaveChangesAsync(ct);
        _logger.LogInformation("Admin {AdminId} recorded external payment of {Amount} for statement {StatementId}.", adminId, request.Amount, statement.Id);
    }
    
       // BR-093, BR-192: Aggregate unbilled order fees into monthly fee statements
        public async Task<int> GenerateMonthlyStatementsAsync(
            int billingYear,
            short billingMonth,
            CancellationToken ct = default)
        {
            if (billingMonth < 1 || billingMonth > 12)
            {
                throw AppException.BadRequest("Billing month must be between 1 and 12.");
            }
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var unbilledFees = await _repository.GetUnbilledOrderFeesAsync(billingYear, billingMonth, ct);
            var unbilledAdjustments = await _repository.GetUnbilledAdjustmentsAsync(billingYear, billingMonth, ct);

            if (unbilledFees.Count == 0 && unbilledAdjustments.Count == 0)
            {
                return 0;
            }
            var shopIds = unbilledFees.Select(f => f.ShopId)
                .Union(unbilledAdjustments.Select(a => a.ShopId))
                .Distinct()
                .ToList();

            var statementsCreated = 0;
            foreach (var shopId in shopIds)
            {
                var shopFees = unbilledFees.Where(f => f.ShopId == shopId).ToList();
                var shopAdjustments = unbilledAdjustments.Where(a => a.ShopId == shopId).ToList();
                var periodGrossFees = shopFees.Sum(f => f.FeeAmount);
                var periodAdjustments = shopAdjustments.Sum(a => a.Amount); // negative for refunds

                var existingStatement = await _repository.GetShopStatementForPeriodAsync(shopId, billingYear, billingMonth, ct);
                if (existingStatement != null)
                {
              
                    if (existingStatement.Status == PlatformFeeStatementStatus.Paid)
                    {
                        _logger.LogWarning("Statement {StatementId} for shop {ShopId} in period {Year}-{Month} is already Paid. Skipping modification.",
                            existingStatement.Id, shopId, billingYear, billingMonth);
                        continue;
                    }

                    existingStatement.TotalFeeAmount = Math.Max(0, existingStatement.TotalFeeAmount + periodGrossFees + periodAdjustments);
                    existingStatement.UpdatedAt = now;
                    if (existingStatement.TotalFeeAmount == 0 && existingStatement.Status == PlatformFeeStatementStatus.Issued)
                    {
                        existingStatement.Status = PlatformFeeStatementStatus.Paid;
                        existingStatement.PaidAt = now;
                        existingStatement.DueAt = null;
                    }
                    foreach (var fee in shopFees)
                    {
                        fee.StatementId = existingStatement.Id;
                        fee.UpdatedAt = now;
                    }
                    foreach (var adj in shopAdjustments)
                    {
                        adj.StatementId = existingStatement.Id;
                    }
                }
                else
                {
                    var netFee = Math.Max(0, periodGrossFees + periodAdjustments);
                    var isZeroFee = netFee == 0;
                    var statement = new PlatformFeeStatement
                    {
                        Id = Guid.NewGuid(),
                        ShopId = shopId,
                        BillingYear = billingYear,
                        BillingMonth = billingMonth,
                        Status = isZeroFee ? PlatformFeeStatementStatus.Paid : PlatformFeeStatementStatus.Issued,
                        TotalFeeAmount = netFee,
                        IssuedAt = now,
                        DueAt = isZeroFee ? null : now.AddDays(7), // BR-192: statement due 7 days after issuance
                        PaidAt = isZeroFee ? now : null,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _repository.AddStatementAsync(statement, ct);
                    foreach (var fee in shopFees)
                    {
                        fee.StatementId = statement.Id;
                        fee.UpdatedAt = now;
                    }
                    foreach (var adj in shopAdjustments)
                    {
                        adj.StatementId = statement.Id;
                    }
                    statementsCreated++;
                }
            }
            await _repository.SaveChangesAsync(ct);
            _logger.LogInformation("Generated {Count} monthly platform fee statements for period {Year}-{Month}.", statementsCreated, billingYear, billingMonth);
            return statementsCreated;
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
