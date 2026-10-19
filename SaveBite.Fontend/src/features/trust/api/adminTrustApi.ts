import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  AdminTrustLevel,
  PenaltyRule,
  TrustScoreRule,
  UpdateLevelBenefitsPayload,
  UpdateLevelRequirementPayload,
  UpdatePenaltyRulePayload,
  UpdateScoreRulePayload,
} from "../types/adminTrust.types";

export const adminTrustApi = {
  // UC-129 / UC-131
  async getLevels(): Promise<AdminTrustLevel[]> {
    const res = await httpClient.get<ApiResponse<AdminTrustLevel[]>>(
      API_ENDPOINTS.ADMIN_TRUST.LEVELS,
    );
    return res.data.data;
  },

  // UC-130
  async updateLevelBenefits(levelId: string, payload: UpdateLevelBenefitsPayload) {
    const res = await httpClient.put<ApiResponse<AdminTrustLevel>>(
      API_ENDPOINTS.ADMIN_TRUST.LEVEL_BENEFITS(levelId),
      payload,
    );
    return res.data.data;
  },

  // UC-132
  async updateLevelRequirement(levelId: string, payload: UpdateLevelRequirementPayload) {
    const res = await httpClient.put<ApiResponse<AdminTrustLevel>>(
      API_ENDPOINTS.ADMIN_TRUST.LEVEL_REQUIREMENTS(levelId),
      payload,
    );
    return res.data.data;
  },

  // UC-133
  async getPenaltyRules(): Promise<PenaltyRule[]> {
    const res = await httpClient.get<ApiResponse<PenaltyRule[]>>(
      API_ENDPOINTS.ADMIN_TRUST.PENALTY_RULES,
    );
    return res.data.data;
  },

  // UC-134
  async updatePenaltyRule(ruleId: string, payload: UpdatePenaltyRulePayload) {
    const res = await httpClient.put<ApiResponse<PenaltyRule>>(
      API_ENDPOINTS.ADMIN_TRUST.PENALTY_RULE(ruleId),
      payload,
    );
    return res.data.data;
  },

  // UC-135
  async getScoreRules(): Promise<TrustScoreRule[]> {
    const res = await httpClient.get<ApiResponse<TrustScoreRule[]>>(
      API_ENDPOINTS.ADMIN_TRUST.SCORE_RULES,
    );
    return res.data.data;
  },

  // UC-136
  async updateScoreRule(ruleId: string, payload: UpdateScoreRulePayload) {
    const res = await httpClient.put<ApiResponse<TrustScoreRule>>(
      API_ENDPOINTS.ADMIN_TRUST.SCORE_RULE(ruleId),
      payload,
    );
    return res.data.data;
  },

  // UC-137
  async getEarningRules(): Promise<TrustScoreRule[]> {
    const res = await httpClient.get<ApiResponse<TrustScoreRule[]>>(
      API_ENDPOINTS.ADMIN_TRUST.EARNING_RULES,
    );
    return res.data.data;
  },

  // UC-138
  async updateEarningRule(ruleId: string, payload: UpdateScoreRulePayload) {
    const res = await httpClient.put<ApiResponse<TrustScoreRule>>(
      API_ENDPOINTS.ADMIN_TRUST.EARNING_RULE(ruleId),
      payload,
    );
    return res.data.data;
  },
};
