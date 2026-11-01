/** today is the default. month and year use the `month` (yyyy-MM) and `year` fields. */
export type AnalyticsPreset = "today" | "yesterday" | "7d" | "month" | "year";

export interface AnalyticsPeriodQuery {
  preset?: AnalyticsPreset;
  fromDate?: string;
  toDate?: string;
  /** yyyy-MM, used with the month preset. */
  month?: string;
  year?: number;
}

export type TrendGranularity = "hour" | "day" | "month";

/** The window the "selected" figures cover. Dates are Vietnam calendar days. */
export interface AnalyticsPeriodInfo {
  preset: string;
  granularity: TrendGranularity;
  startDate: string;
  endDate: string;
  startUtc: string;
  endUtc: string;
}

/**
 * date is yyyy-MM-ddTHH for hourly points, yyyy-MM-dd for daily points and yyyy-MM-01 for monthly points.
 * paidRevenue = paidOnline + paidCash.
 */
export interface RevenueTrendPoint {
  date: string;
  paidRevenue: number;
  refundedAmount: number;
  netRevenue: number;
  completedOrders: number;
  paidOnline: number;
  paidCash: number;
}

export interface StoreRevenueOverview {
  grossCompletedOrderValue: number;
  actuallyPaidCompletedAmount: number;
  collectedOnlineAmount: number;
  collectedCashAmount: number;
  refundedAmount: number;
  salesRevenue: number;
  netPlatformFeeAmount: number;
  netShopRevenue: number;
  completedOrdersCount: number;
  averageCompletedOrderValue: number;
  trend: RevenueTrendPoint[];
}

export interface OrderStatusCountItem {
  status: string;
  count: number;
  percentage: number;
}

export interface StoreOrderStatistics {
  totalOrders: number;
  completedOrders: number;
  pendingOrders: number;
  activeProcessingOrders: number;
  cancelledOrRejectedOrders: number;
  expiredOrders: number;
  noShowOrders: number;
  lateArrivalOrders: number;
  completionRatePercent: number;
  cancellationRatePercent: number;
  noShowRatePercent: number;
  statusBreakdown: OrderStatusCountItem[];
}

export interface StoreTopFlashDealItem {
  flashDealId: string;
  productName: string;
  status: string;
  allocatedQuantity: number;
  quantitySoldFromCompletedOrders: number;
  revenueFromCompletedOrders: number;
  saleStartTime: string;
  orderEndTime: string;
}

export interface DealStatusItem {
  label: string;
  count: number;
  amount: number;
  percentage: number;
}

export interface StoreFlashDealStatistics {
  createdDealsCount: number;
  onSaleNowCount: number;
  pendingNowCount: number;
  hiddenNowCount: number;
  /** Cancelled by the owner during the period (final state). */
  cancelledEarlyCount: number;
  /** The sale window ran out during the period (final state). */
  endedOnScheduleCount: number;
  soldOutDealsCount: number;
  totalAllocatedQuantity: number;
  completedOrderItemsQuantitySold: number;
  completedOrderItemsSalesAmount: number;
  ordersWithDealsCount: number;
  sellThroughRatePercent: number;
  statusBreakdown: DealStatusItem[];
  topPerformingDeals: StoreTopFlashDealItem[];
}

export interface PaymentMethodBreakdownItem {
  paymentMethod: string;
  transactionCount: number;
  totalAmount: number;
  sharePercent: number;
}

export interface StorePaymentMethodRevenue {
  totalSucceededPaymentAmount: number;
  totalSucceededPaymentCount: number;
  methods: PaymentMethodBreakdownItem[];
}

export interface StoreAnalyticsSnapshot {
  revenueOverview: StoreRevenueOverview;
  orderStatistics: StoreOrderStatistics;
  flashDealStatistics: StoreFlashDealStatistics;
  revenueByPaymentMethod: StorePaymentMethodRevenue;
}

/** The same figures twice: for the selected period and for everything ever recorded. */
export interface StoreDashboardAnalytics {
  shopId: string;
  shopName: string;
  period: AnalyticsPeriodInfo;
  selected: StoreAnalyticsSnapshot;
  overall: StoreAnalyticsSnapshot;
}
