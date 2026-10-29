import type { CartShop } from "@/features/cart/types/cart.types";

export type CheckoutMethod = "OnlinePayment" | "PayAtShop";
export interface DepositRange {
  id: string;
  fromOrderAmount: number;
  toOrderAmount: number;
  depositAmount: number;
}
export interface CheckoutPolicy {
  levelId?: string;
  levelName: string;
  levelRank?: number;
  isDefaultLevel: boolean;
  policyId?: string;
  policyUpdatedAt?: string;
  freeOrderAmount?: number;
  maxDepositOrderAmount?: number;
  payAtShopAllowed: boolean;
  restrictionCode?: string;
  restrictionReason?: string;
  requiredDeposit: number;
  appliedRange?: DepositRange;
  ranges: DepositRange[];
}
export interface CheckoutContext {
  isSimulatedPayment?: boolean;
  shop: CartShop;
  policy: CheckoutPolicy;
  onlinePaymentAvailable: boolean;
  paymentUnavailableReason?: string;
}
export interface PickupEstimate {
  distanceKm: number;
  etaMinutes: number;
  bufferMinutes: number;
  softDeadlineAt: string;
  hardDeadlineAt: string;
  dealEndsAt: string;
  bufferWasCapped: boolean;
}
export interface CheckoutRoute {
  originLatitude: number;
  originLongitude: number;
  shopLatitude: number;
  shopLongitude: number;
  encodedPolyline: string;
}
export interface CheckoutQuote extends CheckoutContext {
  route?: CheckoutRoute;
  id: string;
  pickup: PickupEstimate;
  createdAt: string;
  expiresAt: string;
}
export interface CheckoutGps {
  latitude: number;
  longitude: number;
  accuracyMeters: number;
  capturedAt: string;
}
export interface CheckoutOrder {
  id: string;
  orderCode: string;
  shopId: string;
  shopName: string;
  shopLogoUrl?: string;
  shopAddress?: string;
  createdAt?: string;
  completedAt?: string;
  paymentMethod: CheckoutMethod;
  status: string;
  paymentStatus: string;
  totalAmount: number;
  paidAmount?: number;
  outstandingAmount?: number;
  depositAmount: number;
  requiredOnlineAmount: number;
  remainingAtShop: number;
  softDeadlineAt?: string;
  hardDeadlineAt?: string;
  paymentExpiresAt?: string;
  paymentReviewRequired: boolean;
  canCancel: boolean;
  cancellationUnavailableReason?: string;
  cancellationPolicy?: string;
  checkoutUrl?: string;
  policy?: CheckoutPolicy;
  items: {
    orderItemId: string;
    flashDealVariantId: string;
    flashDealId?: string;
    imageUrl?: string;
    productName: string;
    variantName?: string;
    unitPrice: number;
    quantity: number;
    subtotal: number;
  }[];
  paymentMessage?: string;
}

export interface OrderHistoryPage {
  page: { items: CheckoutOrder[]; nextCursor: string | null; hasMore: boolean; pageSize: number };
  statusCounts: Record<string, number>;
}
