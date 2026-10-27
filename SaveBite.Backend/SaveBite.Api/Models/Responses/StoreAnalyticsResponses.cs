namespace SaveBite.Backend.Models.Responses;

// Every dashboard shows two views of the same figures: the selected period (default: today) and everything ever
// recorded ("overall"). Both are built by the same code, so they always agree.

/// <summary>Describes the window the "selected" figures cover. Dates are Vietnam calendar days.</summary>
public sealed record AnalyticsPeriodInfoResponse(
    string Preset,
    string Granularity,
    string StartDate,
    string EndDate,
    DateTime StartUtc,
    DateTime EndUtc
);

// Date is yyyy-MM-ddTHH for hourly points, yyyy-MM-dd for daily points and yyyy-MM-01 for monthly points.
// PaidRevenue = PaidOnline + PaidCash; NetRevenue = PaidRevenue - RefundedAmount (BR-114).
public sealed record RevenueTrendPointResponse(
    string Date,
    decimal PaidRevenue,
    decimal RefundedAmount,
    decimal NetRevenue,
    int CompletedOrders,
    decimal PaidOnline,
    decimal PaidCash
);

// UC-97 (BR-113, BR-114): sales revenue = amounts actually paid for Orders completed in the period, minus refunds
// transferred for those Orders. Money is split into online payments and cash taken at the shop.
public sealed record StoreRevenueOverviewResponse(
    decimal GrossCompletedOrderValue,
    decimal ActuallyPaidCompletedAmount,
    decimal CollectedOnlineAmount,
    decimal CollectedCashAmount,
    decimal RefundedAmount,
    decimal SalesRevenue,
    decimal NetPlatformFeeAmount,
    decimal NetShopRevenue,
    int CompletedOrdersCount,
    decimal AverageCompletedOrderValue,
    IReadOnlyList<RevenueTrendPointResponse> Trend
);

// UC-98 (BR-113, BR-115): visible Orders created in the period, grouped by their recorded status.
public sealed record StoreOrderStatisticsResponse(
    int TotalOrders,
    int CompletedOrders,
    int PendingOrders,
    int ActiveProcessingOrders,
    int CancelledOrRejectedOrders,
    int ExpiredOrders,
    int NoShowOrders,
    int LateArrivalOrders,
    decimal CompletionRatePercent,
    decimal CancellationRatePercent,
    decimal NoShowRatePercent,
    IReadOnlyList<OrderStatusCountItemResponse> StatusBreakdown
);

public sealed record OrderStatusCountItemResponse(
    string Status,
    int Count,
    decimal Percentage
);

// UC-99 (BR-113, BR-116). Deal counts follow the Deal's own status:
//  - StatusBreakdown groups the Deals created in the period by FlashDealStatus.
//  - OnSaleNow / PendingNow / HiddenNow are live counts.
//  - CancelledEarly = cancelled by the owner during the period; EndedOnSchedule = the sale window ran out during the period.
//  - Units and sales come from completed order items.
public sealed record StoreFlashDealStatisticsResponse(
    int CreatedDealsCount,
    int OnSaleNowCount,
    int PendingNowCount,
    int HiddenNowCount,
    int CancelledEarlyCount,
    int EndedOnScheduleCount,
    int SoldOutDealsCount,
    int TotalAllocatedQuantity,
    int CompletedOrderItemsQuantitySold,
    decimal CompletedOrderItemsSalesAmount,
    int OrdersWithDealsCount,
    decimal SellThroughRatePercent,
    IReadOnlyList<StatusMetricItemResponse> StatusBreakdown,
    IReadOnlyList<StoreTopFlashDealItemResponse> TopPerformingDeals
);

public sealed record StoreTopFlashDealItemResponse(
    Guid FlashDealId,
    string ProductName,
    string Status,
    int AllocatedQuantity,
    int QuantitySoldFromCompletedOrders,
    decimal RevenueFromCompletedOrders,
    DateTime SaleStartTime,
    DateTime OrderEndTime
);

// UC-100 (BR-113, BR-117): successful payments attributed to their recorded method and payment date.
public sealed record StorePaymentMethodRevenueResponse(
    decimal TotalSucceededPaymentAmount,
    int TotalSucceededPaymentCount,
    IReadOnlyList<PaymentMethodBreakdownItemResponse> Methods
);

public sealed record PaymentMethodBreakdownItemResponse(
    string PaymentMethod,
    int TransactionCount,
    decimal TotalAmount,
    decimal SharePercent
);

public sealed record StoreAnalyticsSnapshotResponse(
    StoreRevenueOverviewResponse RevenueOverview,
    StoreOrderStatisticsResponse OrderStatistics,
    StoreFlashDealStatisticsResponse FlashDealStatistics,
    StorePaymentMethodRevenueResponse RevenueByPaymentMethod
);

// Combined response for the Store Dashboard (FE-17): the selected period and the all-time totals.
public sealed record StoreDashboardAnalyticsResponse(
    Guid ShopId,
    string ShopName,
    AnalyticsPeriodInfoResponse Period,
    StoreAnalyticsSnapshotResponse Selected,
    StoreAnalyticsSnapshotResponse Overall
);
