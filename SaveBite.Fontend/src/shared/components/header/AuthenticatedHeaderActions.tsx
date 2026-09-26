import { useEffect, useRef, useState } from "react";
import { Bell, Inbox, ShoppingCart } from "lucide-react";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { useAuthStore } from "@/shared/stores/authStore";

export function AuthenticatedHeaderActions() {
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const notificationsRef = useRef<HTMLDivElement>(null);
  const user = useAuthStore((state) => state.user);

  useEffect(() => {
    if (!notificationsOpen) return;

    const handlePointerDown = (event: PointerEvent) => {
      if (!notificationsRef.current?.contains(event.target as Node)) {
        setNotificationsOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setNotificationsOpen(false);
    };

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [notificationsOpen]);

  if (!user) return null;

  return (
    <div className="flex items-center gap-1 sm:gap-2">
      <div ref={notificationsRef} className="relative">
        <button
          type="button"
          aria-label="Notifications"
          aria-expanded={notificationsOpen}
          aria-haspopup="dialog"
          onClick={() => setNotificationsOpen((open) => !open)}
          className="flex size-10 items-center justify-center rounded-full text-text-secondary transition-colors hover:bg-primary-50 hover:text-primary-600 focus-visible:outline-none focus-visible:shadow-focus"
        >
          <Bell className="size-5" aria-hidden="true" />
        </button>

        {notificationsOpen && (
          <div
            role="dialog"
            aria-label="Notifications"
            className="fixed left-4 right-4 top-18 z-50 rounded-md border border-border-default bg-bg-surface p-6 text-center shadow-md sm:absolute sm:left-auto sm:right-0 sm:top-12 sm:w-80"
          >
            <Inbox className="mx-auto size-9 text-neutral-300" aria-hidden="true" />
            <p className="mt-3 text-sm font-semibold text-text-primary">No notifications yet</p>
            <p className="mt-1 text-xs text-text-secondary">
              New updates will appear here when they are available.
            </p>
          </div>
        )}
      </div>

      <Link
        to={APP_PATHS.CART}
        aria-label="Cart"
        className="flex size-10 items-center justify-center rounded-full text-text-secondary transition-colors hover:bg-primary-50 hover:text-primary-600 focus-visible:outline-none focus-visible:shadow-focus"
      >
        <ShoppingCart className="size-5" aria-hidden="true" />
      </Link>

      <Link
        to={APP_PATHS.ACCOUNT}
        aria-label="Account"
        className="ml-1 flex items-center rounded-full font-bold focus-visible:outline-none focus-visible:shadow-focus"
      >
        <span className="flex size-9 items-center justify-center rounded-full border border-border-default bg-primary-100 text-sm font-semibold text-primary-700">
          {user.fullName.charAt(0).toUpperCase()}
        </span>
        <span className="ml-2 hidden max-w-36 truncate text-sm font-medium text-text-primary sm:inline">
          {user.fullName}
        </span>
      </Link>
    </div>
  );
}
