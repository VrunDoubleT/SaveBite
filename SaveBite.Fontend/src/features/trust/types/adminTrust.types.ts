import type { TrustLevelDepositPolicy, TrustLevelRequirement } from "./trust.types";

export interface AdminTrustLevel {
  id: string;
  name: string;
  description?: string | null;
  rank: number;
  isActive: boolean;
  depositPolicy?: TrustLevelDepositPolicy | null;
  requirement?: TrustLevelRequirement | null;
  userCount: number;
  updatedAt: string;
}

export type TrustLevelChangeType =
  | "Created"
  | "InfoUpdated"
  | "RequirementUpdated"
  | "BenefitsUpdated"
  | "Deactivated"
  | "Reactivated";

export interface TrustLevelDepositPolicySnapshot {
  requireFullOnlinePayment: boolean;
  depositFreeOrderAmount?: number | null;
  maxDepositOrderAmount?: number | null;
  ranges: TrustLevelDepositRangeInput[];
}

export interface TrustLevelHistoryItem {
  id: string;
  trustLevelId: string;
  versionNo: number;
  changeType: TrustLevelChangeType;
  name: string;
  description?: string | null;
  rank: number;
  isActive: boolean;
  minScore: number;
  minCompletedOrders: number;
  minTotalOrderValue: number;
  depositPolicy?: TrustLevelDepositPolicySnapshot | null;
  affectedUserCount: number;
  changedByUserId?: string | null;
  changedByName?: string | null;
  changedAt: string;
}

export interface PenaltyRule {
  id: string;
  minimumOccurrence: number;
  scoreDeduction: number;
  requiredOnlineOrders: number;
  requiredOnlineOrderValue: number;
  permanentOfflineLock: boolean;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isCurrent: boolean;
  createdByName?: string | null;
  createdAt: string;
}

export type TrustScoreRuleKind = "Earning" | "Penalty";

export interface TrustScoreRule {
  id: string;
  ruleType: "OnlineOrderCompleted" | "PayAtShopOrderCompleted" | "NoShow";
  scoreDelta: number;
  kind: TrustScoreRuleKind;
  isActive: boolean;
  createdByName?: string | null;
  createdAt: string;
}

export interface TrustLevelDepositRangeInput {
  fromOrderAmount: number;
  toOrderAmount: number;
  depositAmount: number;
}

export interface UpdateLevelBenefitsPayload {
  description: string | null;
  requireFullOnlinePayment: boolean;
  depositFreeOrderAmount: number | null;
  maxDepositOrderAmount: number | null;
  ranges: TrustLevelDepositRangeInput[];
}

export interface UpdateLevelRequirementPayload {
  minScore: number;
  minCompletedOrders: number;
  minTotalOrderValue: number;
}

export interface CreateTrustLevelPayload {
  name: string;
  requirement: UpdateLevelRequirementPayload;
  benefits: UpdateLevelBenefitsPayload;
}

export interface UpdateLevelInfoPayload {
  name: string;
}

export interface UpdateLevelStatusPayload {
  isActive: boolean;
}

export interface CreatePenaltyRulePayload {
  minimumOccurrence: number;
  scoreDeduction: number;
  requiredOnlineOrders: number;
  requiredOnlineOrderValue: number;
  permanentOfflineLock: boolean;
  isActive: boolean;
  /** ISO string. Omit to start immediately. */
  effectiveFrom?: string;
  /** ISO string or null for open-ended. */
  effectiveTo?: string | null;
}

export interface UpdatePenaltyRulePayload {
  minimumOccurrence: number;
  scoreDeduction: number;
  requiredOnlineOrders: number;
  requiredOnlineOrderValue: number;
  permanentOfflineLock: boolean;
  isActive: boolean;
  /** ISO string. */
  effectiveFrom?: string;
  /** ISO string or null for open-ended. */
  effectiveTo?: string | null;
}

export interface UpdateScoreRulePayload {
  scoreDelta: number;
  isActive: boolean;
}
