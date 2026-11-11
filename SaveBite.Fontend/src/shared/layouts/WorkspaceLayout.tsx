import { useEffect, useState, type ReactNode } from "react";
import { Outlet } from "react-router-dom";
import { WorkspaceHeader } from "@/shared/components/header/WorkspaceHeader";
import { ShieldAlert } from "lucide-react";
import {
  WorkspaceSidebar,
  type WorkspaceNavigationItem,
} from "@/shared/components/navigation/WorkspaceSidebar";
import { useAuthStore } from "@/shared/stores/authStore";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";

interface WorkspaceLayoutProps {
  children?: ReactNode;
  navigation: readonly WorkspaceNavigationItem[];
  navigationLabel: string;
  shopId?: string;
  subtitle: string;
  title: string;
}

export function WorkspaceLayout({
  children,
  navigation,
  navigationLabel,
  shopId,
  subtitle,
  title,
}: WorkspaceLayoutProps) {
  const [collapsed, setCollapsed] = useState(false);
  const user = useAuthStore((state) => state.user);
  const refreshUser = useAuthStore((state) => state.refreshUser);
  const realtime = useRealtimeSubscriptions();
  const currentShop =
    user?.shop && (!shopId || user.shop.shopId === shopId) ? user.shop : null;

  useEffect(() => {
    async function refreshShopStatus() {
      try {
        await refreshUser();
      } catch (error) {
        toast.error(`Unable to refresh shop status: ${getApiErrorMessage(error)}`);
      }
    }

    const off = realtime.on(REALTIME_EVENTS.SHOP_STATUS_CHANGED, (event) => {
      const data = event.data as { shopId?: unknown } | null;
      if (data?.shopId === currentShop?.shopId) void refreshShopStatus();
    });
    const offConnected = realtime.onConnected((reconnected) => {
      if (reconnected) void refreshShopStatus();
    });
    return () => {
      off();
      offConnected();
    };
  }, [currentShop?.shopId, realtime, refreshUser]);

  return (
    <div className="min-h-screen bg-bg-body">
      <WorkspaceSidebar ariaLabel={navigationLabel} navigation={navigation} collapsed={collapsed} />
      <div className={`min-h-screen transition-[padding] ${collapsed ? "lg:pl-20" : "lg:pl-64"}`}>
        <WorkspaceHeader
          title={title}
          subtitle={subtitle}
          collapsed={collapsed}
          onToggleCollapsed={() => setCollapsed((value) => !value)}
        />
        <main className="mx-auto w-full min-w-0 px-4 py-4 md:p-6">
          {currentShop?.status === "Suspended" && (
            <div
              role="alert"
              className="mb-4 rounded-xl border border-rose-200 bg-rose-50/80 p-4 text-rose-900 shadow-2xs"
            >
              <div className="flex items-start gap-3">
                <span
                  aria-hidden="true"
                  className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-rose-100 text-rose-700"
                >
                  <ShieldAlert className="size-5" />
                </span>
                <div className="min-w-0 text-sm">
                  <h2 className="font-bold">
                    {currentShop.suspendedForPlatformFees
                      ? "Your store is suspended due to overdue platform fees"
                      : "Your store has been suspended by an administrator"}
                  </h2>
                  {currentShop.suspendedForPlatformFees ? (
                    <p className="mt-1 leading-normal text-rose-800">
                      Settle all overdue platform fee statements to restore your store operations.
                    </p>
                  ) : currentShop.suspensionReason ? (
                    <p className="mt-1 break-words whitespace-pre-wrap leading-normal text-rose-800">
                      <span className="font-semibold">Reason:</span> {currentShop.suspensionReason}
                    </p>
                  ) : null}
                </div>
              </div>
            </div>
          )}
          {children ?? <Outlet />}
        </main>
      </div>
    </div>
  );
}
