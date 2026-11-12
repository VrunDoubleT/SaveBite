// ---------------------------------------------------------------------
// Shapes returned by the Trust Score API (customer side).
// ---------------------------------------------------------------------

export interface TrustLevelDepositRange {
  id: string;
  fromOrderAmount: number;
  toOrderAmount: number;
  depositAmount: number;
}

export interface TrustLevelDepositPolicy {
  id: string;
  requireFullOnlinePayment: boolean;
  depositFreeOrderAmount?: number | null;
  maxDepositOrderAmount?: number | null;
  ranges: TrustLevelDepositRange[];
  updatedAt: string;
}

export interface TrustLevelSummary {
  id: string;
  name: string;
  description?: string | null;
  rank: number;
  depositPolicy?: TrustLevelDepositPolicy | null;
}

export interface TrustLevelRequirement {
  minScore: number;
  minCompletedOrders: number;
  minTotalOrderValue: number;
}

export type TrustStanding = "GoodStanding" | "Restricted" | "OfflineLocked";

export interface TrustScoreOverview {
  currentScore: number;
  level: TrustLevelSummary;
  requirement?: TrustLevelRequirement | null;
  isHighestLevel: boolean;
  isOfflineLockedPermanently: boolean;
  completedOrdersCount: number;
  accumulatedOrderValue: number;
  activePenaltyCount: number;
  standing: TrustStanding;
  updatedAt: string;
}

export type ProgressMetricKey =
  "score" | "completedOrders" | "orderValue" | "onlineOrders" | "onlineOrderValue";

export interface ProgressMetric {
  key: ProgressMetricKey;
  current: number;
  required: number;
  remaining: number;
  percent: number;
}

export interface LevelProgress {
  currentLevel: TrustLevelSummary;
  nextLevel?: TrustLevelSummary | null;
  isHighestLevel: boolean;
  requirementsMet: boolean;
  overallPercent: number;
  metrics: ProgressMetric[];
  scoreDeltaPerCompletedOrder?: number | null;
  onlineOrderCompletedPoints?: number | null;
  payAtShopOrderCompletedPoints?: number | null;
}

export interface TrustLevelInfo {
  id: string;
  name: string;
  description?: string | null;
  rank: number;
  requirement?: TrustLevelRequirement | null;
  isCurrent: boolean;
  isUnlocked: boolean;
  depositPolicy?: TrustLevelDepositPolicy | null;
}

// RefundReclaimed: a refund took accumulated order value back (score unchanged).
export type TrustScoreEventType = "OrderCompleted" | "NoShow" | "RefundReclaimed";

export interface TrustScoreHistoryItem {
  id: string;
  eventType: TrustScoreEventType;
  scoreBefore: number;
  scoreDelta: number;
  scoreAfter: number;
  /** Accumulated order value (VND) before / change / after this event. */
  valueBefore: number;
  valueDelta: number;
  valueAfter: number;
  reason?: string | null;
  orderId?: string | null;
  orderCode?: string | null;
  createdAt: string;
  /** Set only when this event also moved the customer to another level. */
  previousLevelName?: string | null;
  newLevelName?: string | null;
}

export type PenaltyStatus = "Recovering" | "Recovered" | "Superseded";

export interface PenaltyItem {
  id: string;
  orderId: string;
  orderCode?: string | null;
  occurrenceNumber: number;
  scoreDeducted: number;
  permanentOfflineLock: boolean;
  lockEndsAt?: string | null;
  status: PenaltyStatus;
  requiredOnlineOrders: number;
  requiredOnlineOrderValue: number;
  completedOnlineOrders: number;
  completedOnlineOrderValue: number;
  createdAt: string;
  recoveredAt?: string | null;
  reason?: string | null;
}

export interface PenaltyRecoveryOrder {
  orderId: string;
  orderCode?: string | null;
  orderValue: number;
  createdAt: string;
}

export interface PenaltyRecovery {
  penalty: PenaltyItem;
  isRecovered: boolean;
  overallPercent: number;
  metrics: ProgressMetric[];
  recoveryOrders: PenaltyRecoveryOrder[];
}

export interface TrustListParams {
  page?: number;
  pageSize?: number;
}

export interface TrustHistoryParams extends TrustListParams {
  eventType?: TrustScoreEventType;
}

export interface TrustPenaltyParams extends TrustListParams {
  status?: PenaltyStatus;
}
