import { useEffect, useRef, useState } from "react";
import {
  Bell,
  Check,
  CheckCheck,
  ChevronDown,
  ChevronUp,
  CreditCard,
  Inbox,
  Layers,
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
  const [expandStack, setExpandStack] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  const items = useNotificationStore((state) => state.items);
  const unreadCount = useNotificationStore((state) => state.unreadCount);
  const isLoading = useNotificationStore((state) => state.isLoading);
  const filter = useNotificationStore((state) => state.filter);
  const livePreviews = useNotificationStore((state) => state.livePreviews);
  const setFilter = useNotificationStore((state) => state.setFilter);
  const fetchNotifications = useNotificationStore((state) => state.fetchNotifications);
  const markAsRead = useNotificationStore((state) => state.markAsRead);
  const markAllAsRead = useNotificationStore((state) => state.markAllAsRead);
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
    dismissLivePreview(item.id);
    setOpen(false);
    if (item.actionUrl) {
      navigate(item.actionUrl);
    }
  }

  const hasLivePreviews = !open && livePreviews.length > 0;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        aria-label={`Notifications${unreadCount > 0 ? `, ${unreadCount} unread` : ""}`}
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={handleToggleDropdown}
        className={`relative flex size-10 items-center justify-center rounded-full transition-colors focus-visible:outline-none focus-visible:shadow-focus ${
          open || hasLivePreviews
            ? "bg-primary-50 text-primary-600"
            : "text-text-secondary hover:bg-primary-50 hover:text-primary-600"
        }`}
      >
        <Bell
          className={`size-5 transition-transform duration-300 ${
            hasLivePreviews ? "scale-110 animate-bounce" : ""
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

      {/* Realtime Notification Toasts (Stacked Layer Design when multiple arrive) */}
      {hasLivePreviews && (
        <div
          role="status"
          aria-live="polite"
          className="fixed left-3 right-3 top-14 z-50 transition-all duration-300 sm:absolute sm:left-auto sm:right-0 sm:top-[42px] sm:w-92"
        >
          {/* Header indicator when 2 or more notifications arrive simultaneously */}
          {livePreviews.length > 1 && (
            <div className="mb-1.5 flex items-center justify-between rounded-lg bg-neutral-900/90 px-3 py-1.5 text-white shadow-md backdrop-blur-xs">
              <div className="flex items-center gap-1.5 text-xs font-semibold">
                <Layers className="size-3.5 text-primary-400" />
                <span>{livePreviews.length} new notifications</span>
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setExpandStack((prev) => !prev)}
                  className="flex items-center gap-0.5 text-[11px] text-neutral-300 transition-colors hover:text-white"
                >
                  {expandStack ? "Stack" : "Expand"}
                  {expandStack ? <ChevronUp className="size-3" /> : <ChevronDown className="size-3" />}
                </button>
                <button
                  type="button"
                  aria-label="Dismiss all previews"
                  onClick={() => dismissLivePreview()}
                  className="rounded-full p-0.5 text-neutral-400 hover:bg-neutral-800 hover:text-white"
                >
                  <X className="size-3" />
                </button>
              </div>
            </div>
          )}

          {/* Stacked or expanded display */}
          {expandStack ? (
            /* Expanded view */
            <div className="flex flex-col gap-2">
              {livePreviews.map((preview) => {
                const visual = getNotificationVisual(preview.type);
                return (
                  <div
                    key={preview.id}
                    className="overflow-hidden rounded-xl border border-primary-200 bg-white shadow-lg transition-all"
                  >
                    <div className="flex items-start gap-3 p-3">
                      <div
                        className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full ${visual.bg}`}
                      >
                        <visual.Icon className="size-4" aria-hidden="true" />
                      </div>

                      <button
                        type="button"
                        onClick={() => void handleSelectNotification(preview)}
                        className="min-w-0 flex-1 text-left focus:outline-none"
                      >
                        <div className="flex items-center justify-between gap-1">
                          <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-emerald-700">
                            <Sparkles className="size-2.5" /> New
                          </span>
                          <span className="text-[11px] text-neutral-400">Just now</span>
                        </div>
                        <p className="mt-1 truncate text-xs font-semibold text-neutral-900">
                          {preview.title}
                        </p>
                        <p className="mt-0.5 line-clamp-2 text-xs text-neutral-600">
                          {preview.body}
                        </p>
                      </button>

                      <button
                        type="button"
                        aria-label="Dismiss notification"
                        onClick={(e) => {
                          e.stopPropagation();
                          dismissLivePreview(preview.id);
                        }}
                        className="rounded-full p-1 text-neutral-400 transition-colors hover:bg-neutral-100 hover:text-neutral-600"
                      >
                        <X className="size-4" />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            /* Stacked view (Layered Cards) */
            <div className="relative pb-2">
              {livePreviews.map((preview, index) => {
                const visual = getNotificationVisual(preview.type);
                const isTop = index === 0;
                // Layering styles: top item is prominent, lower items are offset and scaled down
                const stackTransform =
                  index === 0
                    ? "relative z-30 translate-y-0 scale-100 opacity-100 shadow-xl"
                    : index === 1
                      ? "absolute inset-x-0 top-0 z-20 translate-y-2.5 scale-[0.96] opacity-90 shadow-md pointer-events-none"
                      : "absolute inset-x-0 top-0 z-10 translate-y-5 scale-[0.92] opacity-75 shadow-sm pointer-events-none";

                return (
                  <div
                    key={preview.id}
                    className={`overflow-hidden rounded-xl border border-primary-200 bg-white transition-all duration-300 ${stackTransform}`}
                  >
                    <div className="flex items-start gap-3 p-3">
                      <div
                        className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full ${visual.bg}`}
                      >
                        <visual.Icon className="size-4" aria-hidden="true" />
                      </div>

                      <button
                        type="button"
                        onClick={() => void handleSelectNotification(preview)}
                        className="min-w-0 flex-1 text-left focus:outline-none"
                      >
                        <div className="flex items-center justify-between gap-1">
                          <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-emerald-700">
                            <Sparkles className="size-2.5" /> New
                          </span>
                          <span className="text-[11px] text-neutral-400">Just now</span>
                        </div>
                        <p className="mt-1 truncate text-xs font-semibold text-neutral-900">
                          {preview.title}
                        </p>
                        <p className="mt-0.5 line-clamp-2 text-xs text-neutral-600">
                          {preview.body}
                        </p>
                      </button>

                      {isTop && (
                        <button
                          type="button"
                          aria-label="Dismiss notification"
                          onClick={(e) => {
                            e.stopPropagation();
                            dismissLivePreview(preview.id);
                          }}
                          className="rounded-full p-1 text-neutral-400 transition-colors hover:bg-neutral-100 hover:text-neutral-600"
                        >
                          <X className="size-4" />
                        </button>
                      )}
                    </div>
                    {isTop && (
                      <div className="h-1 w-full bg-emerald-500/20">
                        <div className="h-full w-full animate-pulse bg-emerald-500" />
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* Full notification dropdown list (UC-22, UC-23, UC-24) */}
      {open && (
        <div
          role="dialog"
          aria-label="Notifications"
          className="fixed left-3 right-3 top-14 z-50 flex max-h-[30rem] flex-col overflow-hidden rounded-2xl border border-border-default bg-bg-surface shadow-2xl transition-all duration-200 sm:absolute sm:left-auto sm:right-0 sm:top-[42px] sm:w-[26rem]"
        >
          {/* Subtle pointer indicator aligning to bell icon */}
          <div
            className="pointer-events-none absolute -top-1.5 right-3.5 hidden size-3 rotate-45 border-l border-t border-border-default bg-bg-surface sm:block"
            aria-hidden="true"
          />

          {/* Header & Mark All As Read */}
          <div className="relative z-10 border-b border-border-default bg-white px-4 py-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <h3 className="text-sm font-bold text-text-primary">Notifications</h3>
                {unreadCount > 0 && (
                  <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-800">
                    {unreadCount} new
                  </span>
                )}
              </div>

              {/* Mark all as read button */}
              <button
                type="button"
                onClick={() => void markAllAsRead()}
                disabled={unreadCount === 0}
                title="Mark all as read"
                className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-semibold transition-all ${
                  unreadCount > 0
                    ? "bg-emerald-50 text-emerald-700 hover:bg-emerald-100 active:scale-95 cursor-pointer"
                    : "text-neutral-400 opacity-60 cursor-not-allowed"
                }`}
              >
                <CheckCheck className="size-3.5" />
                <span>Mark all read</span>
              </button>
            </div>

            {/* Filter Tabs */}
            <div className="mt-2.5 flex items-center gap-1 rounded-lg bg-neutral-100 p-1">
              <button
                type="button"
                onClick={() => void setFilter("all")}
                className={`flex-1 rounded-md py-1 text-center text-xs font-semibold transition-all ${
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
                className={`flex-1 rounded-md py-1 text-center text-xs font-semibold transition-all ${
                  filter === "unread"
                    ? "bg-white text-neutral-900 shadow-2xs"
                    : "text-neutral-600 hover:text-neutral-900"
                }`}
              >
                Unread {unreadCount > 0 ? `(${unreadCount})` : ""}
              </button>
            </div>
          </div>

          {/* Notification Items List - Uniform height, balanced card layout */}
          <div className="flex-1 divide-y divide-neutral-100 overflow-y-auto bg-neutral-50/50">
            {isLoading && items.length === 0 ? (
              <div className="flex min-h-[140px] items-center justify-center p-6 text-center text-xs text-text-secondary">
                Loading notifications...
              </div>
            ) : items.length === 0 ? (
              <div className="flex min-h-[180px] flex-col items-center justify-center p-6 text-center">
                <Inbox className="size-9 text-neutral-300" aria-hidden="true" />
                <p className="mt-3 text-sm font-semibold text-text-primary">
                  {filter === "unread" ? "No unread notifications" : "No notifications yet"}
                </p>
                <p className="mt-1 text-xs text-text-secondary">
                  Updates about orders, deals, and account activity will appear here.
                </p>
              </div>
            ) : (
              items.map((item) => {
                const { Icon, bg } = getNotificationVisual(item.type);
                return (
                  <div
                    key={item.id}
                    className={`group relative flex min-h-[86px] items-start gap-3 border-l-4 p-3.5 transition-colors hover:bg-neutral-100/70 ${
                      !item.isRead
                        ? "border-emerald-500 bg-emerald-50/30"
                        : "border-transparent bg-white"
                    }`}
                  >
                    {/* Visual Icon (Fixed size) */}
                    <div
                      className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full ${bg}`}
                    >
                      <Icon className="size-4" aria-hidden="true" />
                    </div>

                    {/* Content Box (Fixed line heights, preventing jumping/uneven cards) */}
                    <button
                      type="button"
                      onClick={() => void handleSelectNotification(item)}
                      className="min-w-0 flex-1 text-left focus:outline-none"
                    >
                      {/* Fixed 1-line title */}
                      <div className="flex items-center gap-1.5">
                        <p
                          className={`truncate text-xs leading-4 ${
                            !item.isRead
                              ? "font-bold text-neutral-900"
                              : "font-semibold text-neutral-700"
                          }`}
                        >
                          {item.title}
                        </p>
                        {!item.isRead && (
                          <span
                            className="size-1.5 shrink-0 rounded-full bg-emerald-600"
                            title="Unread"
                          />
                        )}
                      </div>

                      {/* Fixed 2-line body */}
                      <p className="mt-1 line-clamp-2 min-h-[2.25rem] text-xs leading-4.5 text-neutral-600">
                        {item.body || "—"}
                      </p>

                      {/* Footer relative time */}
                      <p className="mt-1 text-[11px] font-medium text-neutral-400">
                        {formatRelativeTime(item.createdAt)}
                      </p>
                    </button>

                    {/* Action buttons (Fixed width column to prevent layout shifting) */}
                    <div className="flex w-7 shrink-0 flex-col items-center justify-start gap-1 pt-0.5">
                      {!item.isRead ? (
                        <button
                          type="button"
                          title="Mark as read"
                          aria-label="Mark as read"
                          onClick={(e) => {
                            e.stopPropagation();
                            void markAsRead(item.id);
                          }}
                          className="flex size-7 items-center justify-center rounded-md text-emerald-600 transition-colors hover:bg-emerald-100 active:scale-95"
                        >
                          <Check className="size-3.5" />
                        </button>
                      ) : (
                        <div className="size-7" />
                      )}

                      <button
                        type="button"
                        title="Delete notification"
                        aria-label="Delete notification"
                        onClick={(e) => {
                          e.stopPropagation();
                          void deleteNotification(item.id);
                        }}
                        className="flex size-7 items-center justify-center rounded-md text-neutral-400 opacity-60 transition-all hover:bg-rose-50 hover:text-rose-600 hover:opacity-100 active:scale-95"
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
