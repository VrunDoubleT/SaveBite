namespace SaveBite.Backend.Models.Responses;

// UC-139 (BR-093, BR-164, BR-190): fees accrued on Completed orders calculated in the period (CalculatedAt), their
// refund adjustments, and statement settlement. TotalOutstandingStatementAmount is a balance across ALL periods.
public sealed record PlatformFeeStatisticsResponse(
    decimal? CurrentFeeRatePercent,
    decimal GrossAccruedFeeAmount,
    decimal RefundFeeAdjustmentAmount,
    decimal NetAccruedFeeAmount,
    decimal TotalPaidStatementAmount,
    decimal TotalOutstandingStatementAmount,
    int TotalFeeBearingOrders,
    IReadOnlyList<StatusMetricItemResponse> StatementStatusBreakdown,
    IReadOnlyList<PlatformFeeStatementSummaryItemResponse> RecentStatements
);

public sealed record PlatformFeeStatementSummaryItemResponse(
    Guid StatementId,
    Guid ShopId,
    string ShopName,
    int BillingYear,
    short BillingMonth,
    string Status,
    decimal TotalFeeAmount,
    DateTime? IssuedAt,
    DateTime? DueAt,
    DateTime? PaidAt
);

// UC-140 (BR-184, BR-190): user growth (new accounts per day) and account activity (buyers who ordered in the period).
public sealed record PlatformUserStatisticsResponse(
    int TotalUsers,
    int NewUsersInPeriod,
    int VerifiedUsers,
    int ActiveAccounts,
    int SuspendedAccounts,
    int ActiveCustomerProfiles,
    int SuspendedCustomerProfiles,
    int ActiveBuyersInPeriod,
    IReadOnlyList<DailyCountPointResponse> NewUsersTrend,
    IReadOnlyList<StatusMetricItemResponse> RoleBreakdown,
    IReadOnlyList<StatusMetricItemResponse> TrustLevelBreakdown
);

public sealed record DailyCountPointResponse(string Date, int Count);

// UC-141 (BR-185, BR-190): shop inventory by status plus activity indicators.
public sealed record PlatformShopStatisticsResponse(
    int TotalShops,
    int NewShopsInPeriod,
    int NewApplicationsInPeriod,
    int ActiveShops,
    int PausedShops,
    int SuspendedShops,
    int ShopsWithActiveDeals,
    int ShopsWithCompletedOrdersInPeriod,
    IReadOnlyList<StatusMetricItemResponse> ShopStatusBreakdown,
    IReadOnlyList<StatusMetricItemResponse> ApplicationStatusBreakdown
);

// UC-142 (BR-190): same Order visibility, same Vietnam-calendar boundaries and same revenue definition as UC-97/98.
// GrossMerchandiseValue excludes orders that never produced a sale (Cancelled, Rejected, Expired, NoShow).
public sealed record PlatformRevenueAndOrderStatisticsResponse(
    int TotalOrdersInPeriod,
    int CompletedOrdersInPeriod,
    decimal GrossMerchandiseValue,
    decimal CompletedOrdersPaidRevenue,
    decimal CollectedOnlineAmount,
    decimal CollectedCashAmount,
    decimal TotalRefundedAmount,
    decimal NetPlatformOrderRevenue,
    decimal AverageCompletedOrderValue,
    decimal OrderCompletionRatePercent,
    IReadOnlyList<OrderStatusCountItemResponse> OrderStatusBreakdown,
    IReadOnlyList<RevenueTrendPointResponse> Trend
);

// UC-143 (BR-116, BR-190): same definitions as the store dashboard, across all shops. StatusBreakdown groups the Deals
// created in the period by FlashDealStatus; CancelledEarly / EndedOnSchedule are the two final states.
public sealed record PlatformFlashDealStatisticsResponse(
    int CreatedDealsCount,
    int OnSaleNowCount,
    int PendingNowCount,
    int HiddenNowCount,
    int CancelledEarlyCount,
    int EndedOnScheduleCount,
    int SoldOutDealsCount,
    int TotalAllocatedUnits,
    int TotalReservedUnits,
    int TotalSoldUnitsFromCompletedOrders,
    int OrdersWithDealsCount,
    decimal CompletedDealSalesAmount,
    decimal CustomerEstimatedSavingsAmount,
    decimal SellThroughRatePercent,
    IReadOnlyList<StatusMetricItemResponse> StatusBreakdown
);

// UC-144 (BR-190): payments by payment date, refund requests by creation date, transfers by transfer date.
public sealed record PlatformPaymentAndRefundStatisticsResponse(
    int TotalPaymentTransactions,
    int SucceededPaymentsCount,
    int PendingPaymentsCount,
    int FailedOrCancelledPaymentsCount,
    decimal TotalCollectedPaymentAmount,
    IReadOnlyList<StatusMetricItemResponse> PaymentStatusBreakdown,
    IReadOnlyList<PaymentMethodBreakdownItemResponse> PaymentMethodBreakdown,
    int TotalRefundRequests,
    int CompletedOrTransferredRefundsCount,
    int InProgressRefundsCount,
    int RejectedOrWithdrawnRefundsCount,
    decimal TotalRequestedRefundAmount,
    decimal TotalTransferredRefundAmount,
    IReadOnlyList<StatusMetricItemResponse> RefundStatusBreakdown,
    IReadOnlyList<StatusMetricItemResponse> RefundTypeBreakdown
);

public sealed record StatusMetricItemResponse(
    string Label,
    int Count,
    decimal Amount = 0m,
    decimal Percentage = 0m
);

public sealed record PlatformAnalyticsSnapshotResponse(
    PlatformFeeStatisticsResponse PlatformFees,
    PlatformUserStatisticsResponse Users,
    PlatformShopStatisticsResponse Shops,
    PlatformRevenueAndOrderStatisticsResponse RevenueAndOrders,
    PlatformFlashDealStatisticsResponse FlashDeals,
    PlatformPaymentAndRefundStatisticsResponse PaymentsAndRefunds
);

// Combined response for the Admin Platform Dashboard (FE-23): the selected period and the all-time totals.
public sealed record PlatformDashboardAnalyticsResponse(
    AnalyticsPeriodInfoResponse Period,
    PlatformAnalyticsSnapshotResponse Selected,
    PlatformAnalyticsSnapshotResponse Overall
);
