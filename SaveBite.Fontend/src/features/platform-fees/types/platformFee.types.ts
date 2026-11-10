export type PlatformFeeStatementStatus = "Draft" | "Issued" | "Paid" | "Overdue";

export interface PlatformFeeConfigResponse {
  status: "Scheduled" | "Active" | "Ended" | "Cancelled" | "Superseded";
  reason: string | null;
  replacesConfigId: string | null;
  closedBy: string | null;
  closedAt: string | null;
  closureReason: string | null;
  activationProcessedAt: string | null;
  createdByName: string | null;
  maxFutureSchedules: number;
  id: string;
  feeRate: number;
  feePercentage: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  createdBy: string;
  createdAt: string;
}

export interface PlatformPayOsConfigResponse {
  maskedClientId: string | null;
  hasClientId: boolean;
  hasApiKey: boolean;
  hasChecksumKey: boolean;
  isVerified: boolean;
  paymentsAvailable: boolean;
  updatedAt: string | null;
  updatedByUserId: string | null;
  updatedByName: string | null;
}

export interface UpdatePlatformPayOsConfigRequest {
  clientId?: string;
  apiKey?: string;
  checksumKey?: string;
}

export interface PlatformPayOsConfigHistoryItem {
  id: string;
  changedByUserId: string;
  changedByName: string;
  changedFields: string[];
  previousMaskedClientId: string | null;
  maskedClientId: string;
  changedAt: string;
}

export interface GetAdminPlatformFeesParams {
  year?: number;
  month?: number;
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface UpdatePlatformFeeConfigRequest {
  replacesConfigId?: string;
  reason: string;
  feeRate: number;
  effectiveFrom?: string;
  cancelUpcomingScheduled?: boolean;
}

export interface PlatformFeeStatementSummary {
  id: string;
  shopId: string;
  shopName?: string;
  shopLogoUrl?: string | null;
  billingYear: number;
  billingMonth: number;
  status: PlatformFeeStatementStatus;
  totalFeeAmount: number;
  issuedAt: string | null;
  dueAt: string | null;
  paymentPauseReason: string | null;
  paymentPausedAt: string | null;
  paidAt: string | null;
  createdAt: string;
}

export interface OrderPlatformFeeItem {
  feeConfigId: string;
  id: string;
  orderId: string;
  grossAmount: number;
  feeRate: number;
  feeAmount: number;
  refundCreditAmount?: number;
  netFeeAmount?: number;
  calculatedAt: string;
}

export interface PlatformFeeAdjustmentItem {
  id: string;
  refundId: string;
  amount: number;
  reason: string;
  createdAt: string;
}

export interface PlatformFeePaymentItem {
  id: string;
  amount: number;
  paymentMethod: string;
  gatewayTransactionId?: string;
  status: string;
  paidAt?: string;
  createdAt: string;
}
export interface PlatformFeeStatementHistoryItem {
  id: string;
  statementId: string;
  changeType: string;
  previousAmount: number;
  changeAmount: number;
  adjustmentAmount?: number;
  newAmount: number;
  reason: string;
  relatedRefundId?: string | null;
  relatedOrderId?: string | null;
  createdAt: string;
}

export interface PlatformFeeStatementDetail {
  shopAddress?: string | null;
  id: string;
  shopId: string;
  shopName: string;
  shopLogoUrl?: string | null;
  billingYear: number;
  billingMonth: number;
  status: PlatformFeeStatementStatus;
  totalFeeAmount: number;
  issuedAt?: string;
  dueAt?: string | null;
  paymentPauseReason: string | null;
  paymentPausedAt: string | null;
  paidAt?: string;
  createdAt: string;
  orders: OrderPlatformFeeItem[];
  adjustments: PlatformFeeAdjustmentItem[];
  payments: PlatformFeePaymentItem[];
  histories?: PlatformFeeStatementHistoryItem[];
}

export interface PlatformFeePaymentLinkResponse {
  paymentId: string;
  isSimulated?: boolean;
  statementId: string;
  amount: number;
  orderCode: number;
  checkoutUrl: string;
  qrCode?: string;
  expiredAt?: string;
}

export interface GetShopPlatformFeesParams {
  year?: number;
  month?: number;
  status?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface GetShopMonthlyPlatformFeeReportParams {
  year: number;
  month: number;
  pageNumber?: number;
  pageSize?: number;
}

export interface ShopMonthlyPlatformFeeOrder {
  id: string;
  orderId: string;
  orderCode: string;
  feeConfigId: string;
  grossAmount: number;
  feeRate: number;
  originalFeeAmount: number;
  refundAdjustmentAmount: number;
  feeAmount: number;
  calculatedAt: string;
  statementId: string | null;
}

export interface ShopMonthlyPlatformFeeReport {
  billingYear: number;
  billingMonth: number;
  orderCount: number;
  totalGrossAmount: number;
  totalFeeAmount: number;
  unbilledFeeAmount: number;
  orders: PagedResult<ShopMonthlyPlatformFeeOrder>;
}

export interface ShopCurrentPlatformFeeRate {
  feePercentage: number | null;
  effectiveFrom: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page?: number;
  pageNumber?: number;
  pageSize: number;
  totalCount?: number;
  totalItems?: number;
  totalPages: number;
  hasPrevious?: boolean;
  hasNext?: boolean;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}

export interface PlatformFeePeriodStatistics {
  totalStatements: number;
  paidStatements: number;
  unpaidStatements: number;
  overdueStatements: number;
  zeroFeeStatements: number;
  year: number;
  month: number;
  totalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  overdueAmount: number;
  totalShops: number;
  paidShops: number;
  unpaidShops: number;
  overdueShops: number;
  zeroFeeShops: number;
  asOf: string;
}
