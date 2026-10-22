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
  effectiveFrom: string;
  effectiveTo?: string | null;
  isCurrent: boolean;
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

export interface UpdatePenaltyRulePayload {
  minimumOccurrence: number;
  scoreDeduction: number;
  requiredOnlineOrders: number;
  requiredOnlineOrderValue: number;
  permanentOfflineLock: boolean;
  isActive: boolean;
  /** ISO string. Omit to start the new version immediately. */
  effectiveFrom?: string;
}

export interface UpdateScoreRulePayload {
  scoreDelta: number;
  /** ISO string. Omit to start the new version immediately. */
  effectiveFrom?: string;
}