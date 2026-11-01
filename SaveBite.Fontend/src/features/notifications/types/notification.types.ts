export interface NotificationItem {
  id: string;
  type: number | string;
  title: string;
  body: string;
  actionUrl?: string | null;
  isRead: boolean;
  readAt?: string | null;
  createdAt: string;
}

export interface NotificationPagedResult {
  items: NotificationItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface NotificationListResponse {
  page: NotificationPagedResult;
  unreadCount: number;
}

export interface NotificationRealtimePayload {
  notification: NotificationItem;
  unreadCount: number;
}

export interface NotificationStateSyncPayload {
  notificationId: string;
  unreadCount: number;
  readAt?: string | null;
}
