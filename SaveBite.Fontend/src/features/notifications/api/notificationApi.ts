import { httpClient, API_ENDPOINTS } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  NotificationItem,
  NotificationListResponse,
} from "../types/notification.types";

export const notificationApi = {
  async getNotifications(params?: {
    page?: number;
    pageSize?: number;
    isUnreadOnly?: boolean;
  }): Promise<NotificationListResponse> {
    const response = await httpClient.get<ApiResponse<NotificationListResponse>>(
      API_ENDPOINTS.NOTIFICATIONS.BASE,
      { params },
    );
    return response.data.data;
  },

  async markAsRead(notificationId: string): Promise<NotificationItem> {
    const response = await httpClient.patch<ApiResponse<NotificationItem>>(
      API_ENDPOINTS.NOTIFICATIONS.MARK_READ(notificationId),
    );
    return response.data.data;
  },

  async deleteNotification(notificationId: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.NOTIFICATIONS.DELETE(notificationId));
  },
};
