import { create } from "zustand";
import { notificationApi } from "../api/notificationApi";
import type {
  NotificationItem,
  NotificationRealtimePayload,
  NotificationStateSyncPayload,
} from "../types/notification.types";

interface NotificationState {
  items: NotificationItem[];
  unreadCount: number;
  isLoading: boolean;
  filter: "all" | "unread";
  livePreview: NotificationItem | null;
  setFilter: (filter: "all" | "unread") => Promise<void>;
  fetchNotifications: (filter?: "all" | "unread") => Promise<void>;
  markAsRead: (notificationId: string) => Promise<void>;
  deleteNotification: (notificationId: string) => Promise<void>;
  dismissLivePreview: () => void;
  onNotificationReceived: (payload: NotificationRealtimePayload) => void;
  onNotificationRead: (payload: NotificationStateSyncPayload) => void;
  onNotificationDeleted: (payload: NotificationStateSyncPayload) => void;
  reset: () => void;
}

let previewTimeout: ReturnType<typeof setTimeout> | null = null;

function clearPreviewTimer() {
  if (previewTimeout) {
    clearTimeout(previewTimeout);
    previewTimeout = null;
  }
}

export const useNotificationStore = create<NotificationState>((set, get) => ({
  items: [],
  unreadCount: 0,
  isLoading: false,
  filter: "all",
  livePreview: null,

  async setFilter(filter) {
    set({ filter });
    await get().fetchNotifications(filter);
  },

  async fetchNotifications(filterOverride) {
    const activeFilter = filterOverride ?? get().filter;
    set({ isLoading: true });
    try {
      const result = await notificationApi.getNotifications({
        page: 1,
        pageSize: 20,
        isUnreadOnly: activeFilter === "unread" ? true : undefined,
      });
      set({
        items: result.page.items,
        unreadCount: result.unreadCount,
        isLoading: false,
      });
    } catch {
      set({ isLoading: false });
    }
  },

  async markAsRead(notificationId) {
    const target = get().items.find((n) => n.id === notificationId);
    const wasUnread = target && !target.isRead;
    const nowIso = new Date().toISOString();

    if (wasUnread) {
      set((state) => ({
        items:
          state.filter === "unread"
            ? state.items.filter((n) => n.id !== notificationId)
            : state.items.map((n) =>
                n.id === notificationId ? { ...n, isRead: true, readAt: nowIso } : n,
              ),
        unreadCount: Math.max(0, state.unreadCount - 1),
      }));
    }

    try {
      await notificationApi.markAsRead(notificationId);
    } catch {
      await get().fetchNotifications();
    }
  },

  async deleteNotification(notificationId) {
    const target = get().items.find((n) => n.id === notificationId);
    const wasUnread = target && !target.isRead;

    set((state) => ({
      items: state.items.filter((n) => n.id !== notificationId),
      unreadCount: wasUnread ? Math.max(0, state.unreadCount - 1) : state.unreadCount,
      livePreview: state.livePreview?.id === notificationId ? null : state.livePreview,
    }));

    try {
      await notificationApi.deleteNotification(notificationId);
    } catch {
      await get().fetchNotifications();
    }
  },

  dismissLivePreview() {
    clearPreviewTimer();
    set({ livePreview: null });
  },

  onNotificationReceived(payload) {
    if (!payload?.notification) return;
    clearPreviewTimer();

    set((state) => {
      const exists = state.items.some((n) => n.id === payload.notification.id);
      const nextItems = exists
        ? state.items.map((n) => (n.id === payload.notification.id ? payload.notification : n))
        : [payload.notification, ...state.items].slice(0, 30);

      return {
        items: nextItems,
        unreadCount:
          typeof payload.unreadCount === "number"
            ? payload.unreadCount
            : state.unreadCount + (exists ? 0 : 1),
        livePreview: payload.notification,
      };
    });

    // Automatically hide the drop-down notification preview after 3 seconds.
    previewTimeout = setTimeout(() => {
      set({ livePreview: null });
      previewTimeout = null;
    }, 3000);
  },

  onNotificationRead(payload) {
    if (!payload?.notificationId) return;
    set((state) => ({
      items:
        state.filter === "unread"
          ? state.items.filter((n) => n.id !== payload.notificationId)
          : state.items.map((n) =>
              n.id === payload.notificationId
                ? { ...n, isRead: true, readAt: payload.readAt ?? new Date().toISOString() }
                : n,
            ),
      unreadCount:
        typeof payload.unreadCount === "number" ? payload.unreadCount : state.unreadCount,
    }));
  },

  onNotificationDeleted(payload) {
    if (!payload?.notificationId) return;
    set((state) => ({
      items: state.items.filter((n) => n.id !== payload.notificationId),
      unreadCount:
        typeof payload.unreadCount === "number" ? payload.unreadCount : state.unreadCount,
      livePreview:
        state.livePreview?.id === payload.notificationId ? null : state.livePreview,
    }));
  },

  reset() {
    clearPreviewTimer();
    set({
      items: [],
      unreadCount: 0,
      isLoading: false,
      filter: "all",
      livePreview: null,
    });
  },
}));
