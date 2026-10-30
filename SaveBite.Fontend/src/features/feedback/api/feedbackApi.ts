import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { FeedbackPage } from "../types/feedback.types";

export interface FeedbackListParams {
  page: number;
  pageSize: number;
  search?: string;
  status?: number;
}

export const feedbackApi = {
  // Admin views customer feedback.
  async list(params: FeedbackListParams): Promise<FeedbackPage> {
    const response = await httpClient.get<ApiResponse<FeedbackPage>>(API_ENDPOINTS.FEEDBACK.BASE, {
      params,
    });

    return response.data.data;
  },

  // Admin hides violating feedback.
  async remove(id: string, reason: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.FEEDBACK.REMOVE(id), {
      data: {
        reason: reason.trim(),
      },
    });
  },

  // Admin restores feedback hidden by admin.
  async restore(id: string, reason?: string): Promise<void> {
    await httpClient.post(API_ENDPOINTS.FEEDBACK.RESTORE(id), {
      reason: reason?.trim() || null,
    });
  },
};
