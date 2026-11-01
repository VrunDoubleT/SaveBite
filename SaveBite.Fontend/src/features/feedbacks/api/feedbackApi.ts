import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  CreateFeedbackInput,
  FeedbackHistoryResponse,
  FeedbackItem,
  UpdateFeedbackInput,
} from "../types/feedback.types";

export const feedbackApi = {
  // UC-40: Add Feedback
  async addFeedback(input: CreateFeedbackInput): Promise<FeedbackItem> {
    const response = await httpClient.post<ApiResponse<FeedbackItem>>(
      API_ENDPOINTS.FEEDBACKS.BASE,
      input,
    );
    return response.data.data;
  },

  // UC-41: View Feedback History
  async getHistory(page = 1, pageSize = 10): Promise<FeedbackHistoryResponse> {
    const response = await httpClient.get<ApiResponse<FeedbackHistoryResponse>>(
      API_ENDPOINTS.FEEDBACKS.HISTORY,
      {
        params: { page, pageSize },
      },
    );
    return response.data.data;
  },

  // UC-42: Edit Feedback
  async editFeedback(feedbackId: string, input: UpdateFeedbackInput): Promise<FeedbackItem> {
    const response = await httpClient.put<ApiResponse<FeedbackItem>>(
      API_ENDPOINTS.FEEDBACKS.DETAIL(feedbackId),
      input,
    );
    return response.data.data;
  },

  // UC-43: Delete Feedback (Soft Delete)
  async deleteFeedback(feedbackId: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.FEEDBACKS.DETAIL(feedbackId));
  },
};
