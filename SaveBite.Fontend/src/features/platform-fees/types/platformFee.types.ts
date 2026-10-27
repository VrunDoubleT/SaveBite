export type PlatformFeeStatementStatus = "Draft" | "Issued" | "Paid" | "Overdue";

export interface PlatformFeeConfigResponse {
  id: string;
  feeRate: number;
  feePercentage: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  createdBy: string;
  createdAt: string;
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
  feeRate: number;
  effectiveFrom?: string;
  cancelUpcomingScheduled?: boolean;
}

export interface PlatformFeeStatementSummary {
  id: string;
  shopId: string;
  shopName?: string;
  billingYear: number;
  billingMonth: number;
  status: PlatformFeeStatementStatus;
  totalFeeAmount: number;
  issuedAt: string | null;
  dueAt: string | null;
  paidAt: string | null;
  createdAt: string;
}

export interface OrderPlatformFeeItem {
  id: string;
  orderId: string;
  grossAmount: number;
  feeRate: number;
  feeAmount: number;
  calculatedAt: string;
}

export interface PlatformFeeAdjustmentItem {
  id: string;
  type: string;
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

export interface PlatformFeeStatementDetail {
  id: string;
  shopId: string;
  shopName: string;
  billingYear: number;
  billingMonth: number;
  status: PlatformFeeStatementStatus;
  totalFeeAmount: number;
  issuedAt?: string;
  dueAt?: string;
  paidAt?: string;
  createdAt: string;
  orders: OrderPlatformFeeItem[];
  adjustments: PlatformFeeAdjustmentItem[];
  payments: PlatformFeePaymentItem[];
}

export interface PlatformFeePaymentLinkResponse {
  paymentId: string;
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

export interface RecordExternalPaymentRequest {
  amount: number;
  referenceCode?: string;
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
