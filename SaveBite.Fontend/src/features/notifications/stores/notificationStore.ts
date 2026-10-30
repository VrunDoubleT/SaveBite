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
  livePreviews: NotificationItem[];
  livePreview: NotificationItem | null;
  setFilter: (filter: "all" | "unread") => Promise<void>;
  fetchNotifications: (filter?: "all" | "unread") => Promise<void>;
  markAsRead: (notificationId: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  deleteNotification: (notificationId: string) => Promise<void>;
  dismissLivePreview: (notificationId?: string) => void;
  onNotificationReceived: (payload: NotificationRealtimePayload) => void;
  onNotificationRead: (payload: NotificationStateSyncPayload) => void;
  onNotificationDeleted: (payload: NotificationStateSyncPayload) => void;
  reset: () => void;
}

const previewTimeouts = new Map<string, ReturnType<typeof setTimeout>>();

function clearAllPreviewTimers() {
  previewTimeouts.forEach((timeout) => clearTimeout(timeout));
  previewTimeouts.clear();
}

function clearPreviewTimer(id: string) {
  const timeout = previewTimeouts.get(id);
  if (timeout) {
    clearTimeout(timeout);
    previewTimeouts.delete(id);
  }
}

export const useNotificationStore = create<NotificationState>((set, get) => ({
  items: [],
  unreadCount: 0,
  isLoading: false,
  filter: "all",
  livePreviews: [],
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

  async markAllAsRead() {
    const nowIso = new Date().toISOString();
    set((state) => ({
      items:
        state.filter === "unread"
          ? []
          : state.items.map((n) => ({ ...n, isRead: true, readAt: nowIso })),
      unreadCount: 0,
    }));

    try {
      await notificationApi.markAllAsRead();
    } catch {
      await get().fetchNotifications();
    }
  },

  async deleteNotification(notificationId) {
    const target = get().items.find((n) => n.id === notificationId);
    const wasUnread = target && !target.isRead;

    clearPreviewTimer(notificationId);

    set((state) => {
      const nextPreviews = state.livePreviews.filter((n) => n.id !== notificationId);
      return {
        items: state.items.filter((n) => n.id !== notificationId),
        unreadCount: wasUnread ? Math.max(0, state.unreadCount - 1) : state.unreadCount,
        livePreviews: nextPreviews,
        livePreview: nextPreviews[0] ?? null,
      };
    });

    try {
      await notificationApi.deleteNotification(notificationId);
    } catch {
      await get().fetchNotifications();
    }
  },

  dismissLivePreview(notificationId) {
    if (notificationId) {
      clearPreviewTimer(notificationId);
      set((state) => {
        const nextPreviews = state.livePreviews.filter((n) => n.id !== notificationId);
        return {
          livePreviews: nextPreviews,
          livePreview: nextPreviews[0] ?? null,
        };
      });
    } else {
      clearAllPreviewTimers();
      set({
        livePreviews: [],
        livePreview: null,
      });
    }
  },

  onNotificationReceived(payload) {
    if (!payload?.notification) return;
    const notification = payload.notification;

    clearPreviewTimer(notification.id);

    set((state) => {
      const exists = state.items.some((n) => n.id === notification.id);
      const nextItems = exists
        ? state.items.map((n) => (n.id === notification.id ? notification : n))
        : [notification, ...state.items].slice(0, 30);

      const existingPreviews = state.livePreviews.filter((n) => n.id !== notification.id);
      const nextPreviews = [notification, ...existingPreviews].slice(0, 3);

      return {
        items: nextItems,
        unreadCount:
          typeof payload.unreadCount === "number"
            ? payload.unreadCount
            : state.unreadCount + (exists ? 0 : 1),
        livePreviews: nextPreviews,
        livePreview: nextPreviews[0] ?? null,
      };
    });

    // Auto dismiss each preview after 5 seconds
    const timer = setTimeout(() => {
      get().dismissLivePreview(notification.id);
    }, 5000);
    previewTimeouts.set(notification.id, timer);
  },

  onNotificationRead(payload) {
    if (!payload) return;
    const isReadAll =
      !payload.notificationId ||
      payload.notificationId === "00000000-0000-0000-0000-000000000000" ||
      payload.notificationId === "all";

    const nowIso = payload.readAt ?? new Date().toISOString();

    set((state) => {
      if (isReadAll) {
        return {
          items:
            state.filter === "unread"
              ? []
              : state.items.map((n) => ({ ...n, isRead: true, readAt: nowIso })),
          unreadCount:
            typeof payload.unreadCount === "number" ? payload.unreadCount : 0,
        };
      }

      return {
        items:
          state.filter === "unread"
            ? state.items.filter((n) => n.id !== payload.notificationId)
            : state.items.map((n) =>
                n.id === payload.notificationId
                  ? { ...n, isRead: true, readAt: nowIso }
                  : n,
              ),
        unreadCount:
          typeof payload.unreadCount === "number" ? payload.unreadCount : state.unreadCount,
      };
    });
  },

  onNotificationDeleted(payload) {
    if (!payload?.notificationId) return;
    clearPreviewTimer(payload.notificationId);

    set((state) => {
      const nextPreviews = state.livePreviews.filter((n) => n.id !== payload.notificationId);
      return {
        items: state.items.filter((n) => n.id !== payload.notificationId),
        unreadCount:
          typeof payload.unreadCount === "number" ? payload.unreadCount : state.unreadCount,
        livePreviews: nextPreviews,
        livePreview: nextPreviews[0] ?? null,
      };
    });
  },

  reset() {
    clearAllPreviewTimers();
    set({
      items: [],
      unreadCount: 0,
      isLoading: false,
      filter: "all",
      livePreviews: [],
      livePreview: null,
    });
  },
}));

if (import.meta.env.DEV && typeof window !== "undefined") {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).__notificationStore = useNotificationStore;
}

