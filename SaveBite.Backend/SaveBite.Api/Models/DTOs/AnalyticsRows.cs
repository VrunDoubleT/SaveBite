using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.DTOs;

// Light read-models produced by the analytics repositories. Aggregation happens in the database wherever possible,
// so dashboards never materialise full Order / Payment / User entity graphs.

/// <summary>
/// A Completed order with its money flows (BR-114 / BR-093). Paid is split by channel: online payments versus
/// cash taken at the shop (PaymentMethod.Cash and PaymentMethod.PayAtShop).
/// </summary>
public sealed record CompletedOrderFinancialRow(
    Guid OrderId,
    Guid ShopId,
    decimal TotalAmount,
    decimal PaidOnlineAmount,
    decimal PaidCashAmount,
    decimal RefundedAmount,
    decimal FeeAmount,
    DateTime CompletedAtUtc)
{
    public decimal PaidAmount => PaidOnlineAmount + PaidCashAmount;
}

public sealed record OrderStatusCountRow(OrderStatus Status, int Count, int LateArrivals, decimal TotalAmount);

public sealed record PaymentMethodAmountRow(PaymentMethod Method, int Count, decimal Amount);

public sealed record PaymentStatusRow(PaymentStatus Status, int Count, decimal Amount);

public sealed record RefundStatusRow(RefundStatus Status, int Count, decimal RequestedAmount);

public sealed record RefundTypeRow(RefundType Type, int Count, decimal RequestedAmount);

public sealed record DealStatusCountRow(FlashDealStatus Status, int Count);

/// <summary>
/// Deal counts for a period (BR-116). Created and the status breakdown describe the Deals created in the period.
/// OnSaleNow / PendingNow / HiddenNow are live counts. CancelledEarly and EndedOnSchedule are the two final states:
/// cancelled by the owner before the window ended (moment = the cancel update), or the sale window ran out on its own.
/// </summary>
public sealed record DealStateCountsRow(
    int Created,
    int OnSaleNow,
    int PendingNow,
    int HiddenNow,
    int CancelledEarly,
    int EndedOnSchedule,
    int SoldOut,
    int AllocatedUnits,
    int SoldUnits,
    int ReservedUnits,
    int OrdersWithDeals,
    IReadOnlyList<DealStatusCountRow> StatusBreakdown);

/// <summary>Deal sales counted from completed order items (BR-116).</summary>
public sealed record DealSalesTotalsRow(int Quantity, decimal Revenue, decimal CustomerSavings);

public sealed record TopDealRow(
    Guid FlashDealId,
    string ProductName,
    FlashDealStatus Status,
    int AllocatedQuantity,
    DateTime SaleStartTime,
    DateTime OrderEndTime,
    int Quantity,
    decimal Revenue);

public sealed record FeeAccrualRow(int FeeBearingOrders, decimal GrossFee, decimal Adjustments);

public sealed record StatementStatusRow(PlatformFeeStatementStatus Status, int Count, decimal Amount);

public sealed record FeeStatementRow(
    Guid Id,
    Guid ShopId,
    string ShopName,
    int BillingYear,
    short BillingMonth,
    PlatformFeeStatementStatus Status,
    decimal TotalFeeAmount,
    DateTime? IssuedAt,
    DateTime? DueAt,
    DateTime? PaidAt);

public sealed record FeeStatementSummaryRow(
    decimal PaidInPeriod,
    decimal OutstandingBalance,
    IReadOnlyList<StatementStatusRow> StatusBreakdown,
    IReadOnlyList<FeeStatementRow> Recent);

public sealed record UserCountsRow(
    int Total,
    int New,
    int Verified,
    int ActiveAccounts,
    int SuspendedAccounts,
    int ActiveCustomerProfiles,
    int SuspendedCustomerProfiles,
    int ActiveBuyers,
    IReadOnlyList<KeyValuePair<UserRole, int>> Roles,
    IReadOnlyList<KeyValuePair<string, int>> TrustLevels,
    int UsersWithTrustLevel,
    IReadOnlyList<DateTime> NewUserCreatedAtUtc);

public sealed record ShopCountsRow(
    int Total,
    int New,
    int NewApplications,
    int ShopsWithActiveDeals,
    int ShopsWithCompletedOrders,
    IReadOnlyList<KeyValuePair<ShopStatus, int>> ShopStatuses,
    IReadOnlyList<KeyValuePair<ShopApplicationStatus, int>> ApplicationStatuses);
