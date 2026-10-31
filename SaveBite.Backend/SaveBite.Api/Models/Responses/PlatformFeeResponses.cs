using System.Text.Json.Serialization;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

public sealed class PlatformFeeStatementSummaryResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public int BillingYear { get; set; }
    public short BillingMonth { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PlatformFeeStatementStatus Status { get; set; }
    public decimal TotalFeeAmount { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public PlatformFeeStatementSummaryResponse() { }

    public PlatformFeeStatementSummaryResponse(
        Guid id,
        Guid shopId,
        int billingYear,
        short billingMonth,
        PlatformFeeStatementStatus status,
        decimal totalFeeAmount,
        DateTime? issuedAt,
        DateTime? dueAt,
        DateTime? paidAt,
        DateTime createdAt,
        string? shopName = null)
    {
        Id = id;
        ShopId = shopId;
        ShopName = shopName;
        BillingYear = billingYear;
        BillingMonth = billingMonth;
        Status = status;
        TotalFeeAmount = totalFeeAmount;
        IssuedAt = issuedAt;
        DueAt = dueAt;
        PaidAt = paidAt;
        CreatedAt = createdAt;
    }
}

public sealed class OrderPlatformFeeItemResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal FeeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public DateTime CalculatedAt { get; set; }

    public OrderPlatformFeeItemResponse() { }

    public OrderPlatformFeeItemResponse(
        Guid id,
        Guid orderId,
        decimal grossAmount,
        decimal feeRate,
        decimal feeAmount,
        DateTime calculatedAt)
    {
        Id = id;
        OrderId = orderId;
        GrossAmount = grossAmount;
        FeeRate = feeRate;
        FeeAmount = feeAmount;
        CalculatedAt = calculatedAt;
    }
}
public sealed class PlatformFeeStatementHistoryItemResponse
{
    public Guid Id { get; set; }
    public Guid StatementId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatementChangeType ChangeType { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal AdjustmentAmount => ChangeAmount;
    public decimal NewAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? RelatedRefundId { get; set; }
    public Guid? RelatedOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public PlatformFeeStatementHistoryItemResponse() { }
    public PlatformFeeStatementHistoryItemResponse(
        Guid id,
        Guid statementId,
        StatementChangeType changeType,
        decimal previousAmount,
        decimal changeAmount,
        decimal newAmount,
        string reason,
        Guid? relatedRefundId,
        Guid? relatedOrderId,
        DateTime createdAt)
    {
        Id = id;
        StatementId = statementId;
        ChangeType = changeType;
        PreviousAmount = previousAmount;
        ChangeAmount = changeAmount;
        NewAmount = newAmount;
        Reason = reason;
        RelatedRefundId = relatedRefundId;
        RelatedOrderId = relatedOrderId;
        CreatedAt = createdAt;
    }
}

public sealed class PlatformFeeAdjustmentItemResponse
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public PlatformFeeAdjustmentItemResponse() { }

    public PlatformFeeAdjustmentItemResponse(
        Guid id,
        Guid refundId,
        decimal amount,
        string reason,
        DateTime createdAt)
    {
        Id = id;
        RefundId = refundId;
        Amount = amount;
        Reason = reason;
        CreatedAt = createdAt;
    }
}

public sealed class PlatformFeePaymentItemResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethod PaymentMethod { get; set; }
    public string? GatewayTransactionId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public PlatformFeePaymentItemResponse() { }

    public PlatformFeePaymentItemResponse(
        Guid id,
        decimal amount,
        PaymentMethod paymentMethod,
        string? gatewayTransactionId,
        PaymentStatus status,
        DateTime? paidAt,
        DateTime createdAt)
    {
        Id = id;
        Amount = amount;
        PaymentMethod = paymentMethod;
        GatewayTransactionId = gatewayTransactionId;
        Status = status;
        PaidAt = paidAt;
        CreatedAt = createdAt;
    }
}

public sealed class PlatformFeeStatementDetailResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public int BillingYear { get; set; }
    public short BillingMonth { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PlatformFeeStatementStatus Status { get; set; }
    public decimal TotalFeeAmount { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<OrderPlatformFeeItemResponse> Orders { get; set; } = Array.Empty<OrderPlatformFeeItemResponse>();
    public IReadOnlyList<PlatformFeeAdjustmentItemResponse> Adjustments { get; set; } = Array.Empty<PlatformFeeAdjustmentItemResponse>();
    public IReadOnlyList<PlatformFeePaymentItemResponse> Payments { get; set; } = Array.Empty<PlatformFeePaymentItemResponse>();
    public IReadOnlyList<PlatformFeeStatementHistoryItemResponse> Histories { get; set; } = Array.Empty<PlatformFeeStatementHistoryItemResponse>();
    public PlatformFeeStatementDetailResponse() { }

    public PlatformFeeStatementDetailResponse(
        Guid id,
        Guid shopId,
        string shopName,
        int billingYear,
        short billingMonth,
        PlatformFeeStatementStatus status,
        decimal totalFeeAmount,
        DateTime? issuedAt,
        DateTime? dueAt,
        DateTime? paidAt,
        DateTime createdAt,
        IReadOnlyList<OrderPlatformFeeItemResponse> orders,
        IReadOnlyList<PlatformFeeAdjustmentItemResponse> adjustments,
        IReadOnlyList<PlatformFeePaymentItemResponse> payments,
        IReadOnlyList<PlatformFeeStatementHistoryItemResponse>? histories = null)
    {
        Id = id;
        ShopId = shopId;
        ShopName = shopName;
        BillingYear = billingYear;
        BillingMonth = billingMonth;
        Status = status;
        TotalFeeAmount = totalFeeAmount;
        IssuedAt = issuedAt;
        DueAt = dueAt;
        PaidAt = paidAt;
        CreatedAt = createdAt;
        Orders = orders;
        Adjustments = adjustments;
        Payments = payments;
        Histories = histories ?? Array.Empty<PlatformFeeStatementHistoryItemResponse>();
    }
}

public sealed class PlatformFeePaymentLinkResponse
{
    public Guid PaymentId { get; set; }
    public Guid StatementId { get; set; }
    public decimal Amount { get; set; }
    public long OrderCode { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public string? QrCode { get; set; }
    public DateTime? ExpiredAt { get; set; }

    
    public PlatformFeePaymentLinkResponse() { }

    public PlatformFeePaymentLinkResponse(
        Guid paymentId,
        Guid statementId,
        decimal amount,
        long orderCode,
        string checkoutUrl,
        string? qrCode,
        DateTime? expiredAt)
    {
        PaymentId = paymentId;
        StatementId = statementId;
        Amount = amount;
        OrderCode = orderCode;
        CheckoutUrl = checkoutUrl;
        QrCode = qrCode;
        ExpiredAt = expiredAt;
    }
}
