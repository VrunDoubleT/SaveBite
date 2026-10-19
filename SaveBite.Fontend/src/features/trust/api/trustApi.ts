import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse, PaginatedResponse } from "@/shared/types";
import type {
  LevelProgress,
  PenaltyItem,
  PenaltyRecovery,
  TrustHistoryParams,
  TrustLevelInfo,
  TrustPenaltyParams,
  TrustScoreHistoryItem,
  TrustScoreOverview,
} from "../types/trust.types";

// The backend paged result uses `totalItems`; the shared type calls it `total`.
interface BackendPaged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

function toPaginated<T>(data: BackendPaged<T>): PaginatedResponse<T> {
  return {
    items: data.items,
    page: data.page,
    pageSize: data.pageSize,
    total: data.totalItems,
    totalPages: data.totalPages,
  };
}

export const trustApi = {
  // UC-44
  async getOverview(): Promise<TrustScoreOverview> {
    const res = await httpClient.get<ApiResponse<TrustScoreOverview>>(API_ENDPOINTS.TRUST_SCORE.ME);
    return res.data.data;
  },

  // UC-45
  async getLevelProgress(): Promise<LevelProgress> {
    const res = await httpClient.get<ApiResponse<LevelProgress>>(
      API_ENDPOINTS.TRUST_SCORE.LEVEL_PROGRESS,
    );
    return res.data.data;
  },

  // UC-46
  async getHistory(params: TrustHistoryParams): Promise<PaginatedResponse<TrustScoreHistoryItem>> {
    const res = await httpClient.get<ApiResponse<BackendPaged<TrustScoreHistoryItem>>>(
      API_ENDPOINTS.TRUST_SCORE.HISTORY,
      { params },
    );
    return toPaginated(res.data.data);
  },

  // UC-47
  async getLevels(): Promise<TrustLevelInfo[]> {
    const res = await httpClient.get<ApiResponse<TrustLevelInfo[]>>(
      API_ENDPOINTS.TRUST_SCORE.LEVELS,
    );
    return res.data.data;
  },

  // UC-48
  async getPenalties(params: TrustPenaltyParams): Promise<PaginatedResponse<PenaltyItem>> {
    const res = await httpClient.get<ApiResponse<BackendPaged<PenaltyItem>>>(
      API_ENDPOINTS.TRUST_SCORE.PENALTIES,
      { params },
    );
    return toPaginated(res.data.data);
  },

  // UC-49
  async getPenaltyRecovery(penaltyId: string): Promise<PenaltyRecovery> {
    const res = await httpClient.get<ApiResponse<PenaltyRecovery>>(
      API_ENDPOINTS.TRUST_SCORE.PENALTY_RECOVERY(penaltyId),
    );
    return res.data.data;
  },
};
