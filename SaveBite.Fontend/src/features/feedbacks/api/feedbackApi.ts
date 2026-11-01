import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  CreateFeedbackInput,
  CreateFeedbackReplyInput,
  FeedbackHistoryResponse,
  FeedbackItem,
  FeedbackReply,
  ShopFeedbacksQueryParams,
  UpdateFeedbackInput,
  UpdateFeedbackReplyInput,
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

  // UC-41: View Feedback History (Customer)
  async getHistory(
    page = 1,
    pageSize = 10,
    feedbackId?: string,
  ): Promise<FeedbackHistoryResponse> {
    const response = await httpClient.get<ApiResponse<FeedbackHistoryResponse>>(
      API_ENDPOINTS.FEEDBACKS.HISTORY,
      {
        params: { page, pageSize, feedbackId },
      },
    );
    return response.data.data;
  },

  // View Single Feedback
  async getFeedback(feedbackId: string): Promise<FeedbackItem> {
    const response = await httpClient.get<ApiResponse<FeedbackItem>>(
      API_ENDPOINTS.FEEDBACKS.DETAIL(feedbackId),
    );
    return response.data.data;
  },

  // UC-42: Edit Feedback
  async editFeedback(
    feedbackId: string,
    input: UpdateFeedbackInput,
  ): Promise<FeedbackItem> {
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

  // Upload Feedback Image
  async uploadImage(file: File): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);
    const response = await httpClient.post<ApiResponse<string>>(
      API_ENDPOINTS.FEEDBACKS.IMAGES,
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
      },
    );
    return response.data.data;
  },

  // View Feedbacks for Shop (Store Owner & Staff)
  async getShopFeedbacks(
    params: ShopFeedbacksQueryParams = {},
  ): Promise<FeedbackHistoryResponse> {
    const response = await httpClient.get<ApiResponse<FeedbackHistoryResponse>>(
      API_ENDPOINTS.FEEDBACKS.SHOP,
      { params },
    );
    return response.data.data;
  },

  // View Feedbacks for Admin
  async getAdminFeedbacks(
    params: ShopFeedbacksQueryParams = {},
  ): Promise<FeedbackHistoryResponse> {
    const response = await httpClient.get<ApiResponse<FeedbackHistoryResponse>>(
      API_ENDPOINTS.FEEDBACKS.ADMIN,
      { params },
    );
    return response.data.data;
  },

  // Reply Feedback (Shop & Admin)
  async replyFeedback(
    feedbackId: string,
    input: CreateFeedbackReplyInput,
  ): Promise<FeedbackReply> {
    const response = await httpClient.post<ApiResponse<FeedbackReply>>(
      API_ENDPOINTS.FEEDBACKS.REPLY(feedbackId),
      input,
    );
    return response.data.data;
  },

  // Edit Reply (Shop & Admin)
  async editReply(
    feedbackId: string,
    input: UpdateFeedbackReplyInput,
  ): Promise<FeedbackReply> {
    const response = await httpClient.put<ApiResponse<FeedbackReply>>(
      API_ENDPOINTS.FEEDBACKS.REPLY(feedbackId),
      input,
    );
    return response.data.data;
  },

  // Delete Reply (Shop & Admin)
  async deleteReply(feedbackId: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.FEEDBACKS.REPLY(feedbackId));
  },
};
