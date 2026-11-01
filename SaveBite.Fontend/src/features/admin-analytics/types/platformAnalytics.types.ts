import type {
  AnalyticsPeriodInfo,
  OrderStatusCountItem,
  PaymentMethodBreakdownItem,
  RevenueTrendPoint,
} from "@/features/store-analytics/types/storeAnalytics.types";

export interface StatusMetricItem {
  label: string;
  count: number;
  amount: number;
  percentage: number;
}

export interface DailyCountPoint {
  date: string;
  count: number;
}

export interface PlatformFeeStatementSummaryItem {
  statementId: string;
  shopId: string;
  shopName: string;
  billingYear: number;
  billingMonth: number;
  status: string;
  totalFeeAmount: number;
  issuedAt: string | null;
  dueAt: string | null;
  paidAt: string | null;
}

// UC-139
export interface PlatformFeeStatistics {
  currentFeeRatePercent: number | null;
  grossAccruedFeeAmount: number;
  refundFeeAdjustmentAmount: number;
  netAccruedFeeAmount: number;
  totalPaidStatementAmount: number;
  totalOutstandingStatementAmount: number;
  totalFeeBearingOrders: number;
  statementStatusBreakdown: StatusMetricItem[];
  recentStatements: PlatformFeeStatementSummaryItem[];
}

// UC-140
export interface PlatformUserStatistics {
  totalUsers: number;
  newUsersInPeriod: number;
  verifiedUsers: number;
  activeAccounts: number;
  suspendedAccounts: number;
  activeCustomerProfiles: number;
  suspendedCustomerProfiles: number;
  activeBuyersInPeriod: number;
  newUsersTrend: DailyCountPoint[];
  roleBreakdown: StatusMetricItem[];
  trustLevelBreakdown: StatusMetricItem[];
}

// UC-141
export interface PlatformShopStatistics {
  totalShops: number;
  newShopsInPeriod: number;
  newApplicationsInPeriod: number;
  activeShops: number;
  pausedShops: number;
  suspendedShops: number;
  shopsWithActiveDeals: number;
  shopsWithCompletedOrdersInPeriod: number;
  shopStatusBreakdown: StatusMetricItem[];
  applicationStatusBreakdown: StatusMetricItem[];
}

// UC-142
export interface PlatformRevenueAndOrderStatistics {
  totalOrdersInPeriod: number;
  completedOrdersInPeriod: number;
  grossMerchandiseValue: number;
  completedOrdersPaidRevenue: number;
  collectedOnlineAmount: number;
  collectedCashAmount: number;
  totalRefundedAmount: number;
  netPlatformOrderRevenue: number;
  averageCompletedOrderValue: number;
  orderCompletionRatePercent: number;
  orderStatusBreakdown: OrderStatusCountItem[];
  trend: RevenueTrendPoint[];
}

// UC-143
export interface PlatformFlashDealStatistics {
  createdDealsCount: number;
  onSaleNowCount: number;
  pendingNowCount: number;
  hiddenNowCount: number;
  cancelledEarlyCount: number;
  endedOnScheduleCount: number;
  soldOutDealsCount: number;
  totalAllocatedUnits: number;
  totalReservedUnits: number;
  totalSoldUnitsFromCompletedOrders: number;
  ordersWithDealsCount: number;
  completedDealSalesAmount: number;
  customerEstimatedSavingsAmount: number;
  sellThroughRatePercent: number;
  statusBreakdown: StatusMetricItem[];
}

// UC-144
export interface PlatformPaymentAndRefundStatistics {
  totalPaymentTransactions: number;
  succeededPaymentsCount: number;
  pendingPaymentsCount: number;
  failedOrCancelledPaymentsCount: number;
  totalCollectedPaymentAmount: number;
  paymentStatusBreakdown: StatusMetricItem[];
  paymentMethodBreakdown: PaymentMethodBreakdownItem[];
  totalRefundRequests: number;
  completedOrTransferredRefundsCount: number;
  inProgressRefundsCount: number;
  rejectedOrWithdrawnRefundsCount: number;
  totalRequestedRefundAmount: number;
  totalTransferredRefundAmount: number;
  refundStatusBreakdown: StatusMetricItem[];
  refundTypeBreakdown: StatusMetricItem[];
}

export interface PlatformAnalyticsSnapshot {
  platformFees: PlatformFeeStatistics;
  users: PlatformUserStatistics;
  shops: PlatformShopStatistics;
  revenueAndOrders: PlatformRevenueAndOrderStatistics;
  flashDeals: PlatformFlashDealStatistics;
  paymentsAndRefunds: PlatformPaymentAndRefundStatistics;
}

/** The same figures twice: for the selected period and for everything ever recorded. */
export interface PlatformDashboardAnalytics {
  period: AnalyticsPeriodInfo;
  selected: PlatformAnalyticsSnapshot;
  overall: PlatformAnalyticsSnapshot;
}
