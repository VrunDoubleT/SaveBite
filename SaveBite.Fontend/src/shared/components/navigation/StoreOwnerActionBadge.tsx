import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { useAuthStore } from "@/shared/stores/authStore";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { warningApi } from "@/features/warnings/api/warningApi";
import { ownerShopApi } from "@/features/shops/api/ownerShopApi";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";

export function StoreOwnerActionBadge({ kind }: { kind: "fees" | "warnings" }) {
  const userId = useAuthStore((state) => state.user?.id);
  const realtime = useRealtimeSubscriptions();
  const { key } = useLocation();
  const [result, setResult] = useState<{ userId: string; count: number } | null>(null);

  useEffect(() => {
    if (!userId) return;
    const controller = new AbortController();
    let reading = false;
    let dirty = false;
    let shopId: string | undefined;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        while (dirty && !controller.signal.aborted) {
          dirty = false;
          let count: number;
          if (kind === "warnings") {
            count = (await warningApi.history(1, 1, controller.signal, "new")).totalItems;
          } else {
            shopId ??= (await ownerShopApi.getMyShopProfile()).id;
            const pages = await Promise.all([
              platformFeeApi.getShopStatements(shopId, { status: "Issued", pageNumber: 1, pageSize: 1 }, controller.signal),
              platformFeeApi.getShopStatements(shopId, { status: "Overdue", pageNumber: 1, pageSize: 1 }, controller.signal),
            ]);
            count = pages.reduce((sum, page) => sum + (page.totalItems ?? page.totalCount ?? 0), 0);
          }
          if (!controller.signal.aborted) setResult({ userId: userId!, count });
        }
      } catch {
        // Keep the last known count; retry on navigation, focus or the next event.
      } finally {
        reading = false;
      }
    }
    const update = () => { void refresh(); };
    const off = [
      realtime.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, update),
      realtime.on(REALTIME_EVENTS.SHOP_STATUS_CHANGED, update),
      realtime.on(REALTIME_EVENTS.SHOP_ORDERS_CHANGED, update),
      realtime.onConnected((again) => { if (again) update(); }),
    ];
    window.addEventListener("focus", update);
    window.addEventListener("store-owner-actions-changed", update);
    update();
    return () => {
      controller.abort();
      off.forEach((unsubscribe) => unsubscribe());
      window.removeEventListener("focus", update);
      window.removeEventListener("store-owner-actions-changed", update);
    };
  }, [userId, realtime, kind, key]);

  const count = result?.userId === userId ? result?.count ?? 0 : 0;
  if (count <= 0) return null;
  const label = kind === "fees" ? "unpaid platform fee statements" : "warnings requiring correction";
  return (
    <span aria-live="polite" aria-label={`${count} ${label}`} title={`${count} ${label}`}
      className="inline-flex min-w-5 items-center justify-center rounded-full bg-emerald-600 px-1.5 py-0.5 text-xs font-bold leading-4 text-white">
      {count}
    </span>
  );
}
