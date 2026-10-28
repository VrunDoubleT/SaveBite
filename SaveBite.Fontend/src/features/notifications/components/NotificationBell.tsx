import { useEffect, useRef, useState } from "react";
import {
  Bell,
  Check,
  CreditCard,
  Inbox,
  ShieldAlert,
  ShoppingBag,
  Sparkles,
  Store,
  Trash2,
  Users,
  X,
  Zap,
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useNotificationStore } from "../stores/notificationStore";
import type { NotificationItem } from "../types/notification.types";

function formatRelativeTime(isoDate: string): string {
  const date = new Date(isoDate);
  if (Number.isNaN(date.getTime())) return "";
  const diffSeconds = Math.max(0, Math.floor((Date.now() - date.getTime()) / 1000));

  if (diffSeconds < 60) return "Just now";
  const minutes = Math.floor(diffSeconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  return date.toLocaleDateString("en-US");
}

function getNotificationVisual(type: number | string) {
  const key = String(type).toLowerCase();
  // Flash Deal (17)
  if (key === "17" || key.includes("flashdeal")) {
    return {
      Icon: Zap,
      bg: "bg-amber-100 text-amber-600",
    };
  }
  // Payment / Refund (6..11)
  if (
    ["6", "7", "8", "9", "10", "11"].includes(key) ||
    key.includes("payment") ||
    key.includes("refund")
  ) {
    return {
      Icon: CreditCard,
      bg: "bg-blue-100 text-blue-600",
    };
  }
  // Staff Invitation (12, 13)
  if (["12", "13"].includes(key) || key.includes("staff")) {
    return {
      Icon: Users,
      bg: "bg-purple-100 text-purple-600",
    };
  }
  // Shop Application / Product Revision (14, 15, 16, 20)
  if (
    ["14", "15", "16", "20"].includes(key) ||
    key.includes("shop") ||
    key.includes("product")
  ) {
    return {
      Icon: Store,
      bg: "bg-indigo-100 text-indigo-600",
    };
  }
  // Trust Score / Penalty (18, 19)
  if (["18", "19"].includes(key) || key.includes("trust") || key.includes("penalty")) {
    return {
      Icon: ShieldAlert,
      bg: "bg-rose-100 text-rose-600",
    };
  }
  // Default: Order events (1..5)
  return {
    Icon: ShoppingBag,
    bg: "bg-emerald-100 text-emerald-600",
  };
}

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  const items = useNotificationStore((state) => state.items);
  const unreadCount = useNotificationStore((state) => state.unreadCount);
  const isLoading = useNotificationStore((state) => state.isLoading);
  const filter = useNotificationStore((state) => state.filter);
  const livePreview = useNotificationStore((state) => state.livePreview);
  const setFilter = useNotificationStore((state) => state.setFilter);
  const fetchNotifications = useNotificationStore((state) => state.fetchNotifications);
  const markAsRead = useNotificationStore((state) => state.markAsRead);
  const deleteNotification = useNotificationStore((state) => state.deleteNotification);
  const dismissLivePreview = useNotificationStore((state) => state.dismissLivePreview);

  useEffect(() => {
    void fetchNotifications();
  }, [fetchNotifications]);

  useEffect(() => {
    if (!open) return;

    const handlePointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  function handleToggleDropdown() {
    dismissLivePreview();
    setOpen((prev) => {
      const next = !prev;
      if (next) {
        void fetchNotifications();
      }
      return next;
    });
  }

  async function handleSelectNotification(item: NotificationItem) {
    if (!item.isRead) {
      await markAsRead(item.id);
    }
    dismissLivePreview();
    setOpen(false);
    if (item.actionUrl) {
      navigate(item.actionUrl);
    }
  }

  const previewVisual = livePreview ? getNotificationVisual(livePreview.type) : null;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        aria-label={`Notifications${unreadCount > 0 ? `, ${unreadCount} unread` : ""}`}
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={handleToggleDropdown}
        className={`relative flex size-10 items-center justify-center rounded-full transition-colors focus-visible:outline-none focus-visible:shadow-focus ${
          open || livePreview
            ? "bg-primary-50 text-primary-600"
            : "text-text-secondary hover:bg-primary-50 hover:text-primary-600"
        }`}
      >
        <Bell
          className={`size-5 transition-transform duration-300 ${
            livePreview ? "scale-110 animate-bounce" : ""
          }`}
          aria-hidden="true"
        />
        {unreadCount > 0 && (
          <span
            className="absolute -right-1 -top-1 min-w-5 rounded-full bg-rose-600 px-1 text-center text-[10px] font-bold leading-5 text-white shadow-xs"
            aria-hidden="true"
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {/* Auto-hiding 3-second drop-down preview when a new realtime notification arrives */}
      {!open && livePreview && previewVisual && (
        <div
          role="status"
          aria-live="polite"
          className="fixed left-4 right-4 top-18 z-50 overflow-hidden rounded-xl border border-primary-200 bg-white shadow-xl transition-all duration-300 sm:absolute sm:left-auto sm:right-0 sm:top-12 sm:w-88"
        >
          <div className="flex items-start gap-3 p-3.5">
            <div
              className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full ${previewVisual.bg}`}
            >
              <previewVisual.Icon className="size-4" aria-hidden="true" />
            </div>

            <button
              type="button"
              onClick={() => void handleSelectNotification(livePreview)}
              className="min-w-0 flex-1 text-left focus:outline-none"
            >
              <div className="flex items-center gap-1.5">
                <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-emerald-700">
                  <Sparkles className="size-2.5" /> New
                </span>
                <span className="text-[11px] text-neutral-400">Just now</span>
              </div>
              <p className="mt-1 truncate text-sm font-semibold text-neutral-900">
                {livePreview.title}
              </p>
              <p className="mt-0.5 line-clamp-2 text-xs text-neutral-600">
                {livePreview.body}
              </p>
            </button>

            <button
              type="button"
              aria-label="Dismiss notification preview"
              onClick={(e) => {
                e.stopPropagation();
                dismissLivePreview();
              }}
              className="rounded-full p-1 text-neutral-400 transition-colors hover:bg-neutral-100 hover:text-neutral-600"
            >
              <X className="size-4" />
            </button>
          </div>
          <div className="h-1 w-full bg-emerald-500/20">
            <div className="h-full w-full animate-pulse bg-emerald-500" />
          </div>
        </div>
      )}

      {/* Full notification dropdown list (UC-22, UC-23, UC-24) */}
      {open && (
        <div
          role="dialog"
          aria-label="Notifications"
          className="fixed left-4 right-4 top-18 z-50 flex max-h-[28rem] flex-col overflow-hidden rounded-xl border border-border-default bg-bg-surface shadow-xl sm:absolute sm:left-auto sm:right-0 sm:top-12 sm:w-96"
        >
          {/* Header & Filter Tabs */}
          <div className="flex items-center justify-between border-b border-border-default px-4 py-3">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-bold text-text-primary">Notifications</h3>
              {unreadCount > 0 && (
                <span className="rounded-full bg-primary-100 px-2 py-0.5 text-xs font-semibold text-primary-700">
                  {unreadCount} unread
                </span>
              )}
            </div>

            <div className="flex items-center gap-1 rounded-lg bg-neutral-100 p-0.5">
              <button
                type="button"
                onClick={() => void setFilter("all")}
                className={`rounded-md px-2.5 py-1 text-xs font-semibold transition-colors ${
                  filter === "all"
                    ? "bg-white text-neutral-900 shadow-2xs"
                    : "text-neutral-600 hover:text-neutral-900"
                }`}
              >
                All
              </button>
              <button
                type="button"
                onClick={() => void setFilter("unread")}
                className={`rounded-md px-2.5 py-1 text-xs font-semibold transition-colors ${
                  filter === "unread"
                    ? "bg-white text-neutral-900 shadow-2xs"
                    : "text-neutral-600 hover:text-neutral-900"
                }`}
              >
                Unread
              </button>
            </div>
          </div>

          {/* Notification Items List */}
          <div className="flex-1 divide-y divide-neutral-100 overflow-y-auto">
            {isLoading && items.length === 0 ? (
              <div className="p-6 text-center text-xs text-text-secondary">
                Loading notifications...
              </div>
            ) : items.length === 0 ? (
              <div className="p-8 text-center">
                <Inbox className="mx-auto size-9 text-neutral-300" aria-hidden="true" />
                <p className="mt-3 text-sm font-semibold text-text-primary">
                  {filter === "unread" ? "No unread notifications" : "No notifications yet"}
                </p>
                <p className="mt-1 text-xs text-text-secondary">
                  New updates about orders, deals, and account events will appear here.
                </p>
              </div>
            ) : (
              items.map((item) => {
                const { Icon, bg } = getNotificationVisual(item.type);
                return (
                  <div
                    key={item.id}
                    className={`group flex items-start gap-3 px-4 py-3 transition-colors hover:bg-neutral-50 ${
                      !item.isRead ? "bg-emerald-50/40" : "bg-white"
                    }`}
                  >
                    <div
                      className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full ${bg}`}
                    >
                      <Icon className="size-4" aria-hidden="true" />
                    </div>

                    <button
                      type="button"
                      onClick={() => void handleSelectNotification(item)}
                      className="min-w-0 flex-1 text-left focus:outline-none"
                    >
                      <div className="flex items-start justify-between gap-2">
                        <p
                          className={`text-xs leading-4 ${
                            !item.isRead
                              ? "font-bold text-neutral-900"
                              : "font-medium text-neutral-700"
                          }`}
                        >
                          {item.title}
                        </p>
                        {!item.isRead && (
                          <span
                            className="mt-1 size-2 shrink-0 rounded-full bg-emerald-600"
                            title="Unread"
                          />
                        )}
                      </div>
                      <p className="mt-1 line-clamp-2 text-xs text-neutral-600">
                        {item.body}
                      </p>
                      <p className="mt-1 text-[11px] font-medium text-neutral-400">
                        {formatRelativeTime(item.createdAt)}
                      </p>
                    </button>

                    {/* Action buttons: Mark as Read (UC-23) & Delete (UC-24) */}
                    <div className="flex shrink-0 flex-col items-center gap-1 opacity-85 transition-opacity group-hover:opacity-100">
                      {!item.isRead && (
                        <button
                          type="button"
                          title="Mark as read"
                          aria-label="Mark as read"
                          onClick={(e) => {
                            e.stopPropagation();
                            void markAsRead(item.id);
                          }}
                          className="rounded-md p-1 text-emerald-600 transition-colors hover:bg-emerald-100"
                        >
                          <Check className="size-3.5" />
                        </button>
                      )}
                      <button
                        type="button"
                        title="Delete notification"
                        aria-label="Delete notification"
                        onClick={(e) => {
                          e.stopPropagation();
                          void deleteNotification(item.id);
                        }}
                        className="rounded-md p-1 text-neutral-400 transition-colors hover:bg-rose-50 hover:text-rose-600"
                      >
                        <Trash2 className="size-3.5" />
                      </button>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      )}
    </div>
  );
}
