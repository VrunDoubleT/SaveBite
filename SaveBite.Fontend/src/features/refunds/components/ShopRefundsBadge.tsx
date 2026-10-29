import { useEffect, useState } from "react";
import { useAuthStore } from "@/shared/stores/authStore";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { shopOrderApi } from "@/features/shop-orders/api/shopOrderApi";
import { refundApi } from "../api/refundApi";

// Keep the owner's unresolved refund count current throughout the workspace without polling.
export function ShopRefundsBadge() {
  const userId = useAuthStore((state) => state.user?.id);
  const realtime = useRealtimeSubscriptions();
  const [result, setResult] = useState<{ userId: string; count: number } | null>(null);
  useEffect(() => {
    if (!userId) return;
    const abort = new AbortController();
    let shopId = ""; let dirty = false; let reading = false;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        if (!shopId) shopId = await shopOrderApi.ownerShop(abort.signal);
        while (dirty && !abort.signal.aborted) {
          dirty = false;
          const count = await refundApi.shopPendingCount(shopId, abort.signal);
          if (!abort.signal.aborted) setResult({ userId: userId!, count });
        }
      } catch { /* Recover through the next relevant event or reconnect without inventing a badge count. */ }
      finally { reading = false; }
    }
    const off = realtime.on(REALTIME_EVENTS.SHOP_ORDERS_CHANGED, (event) => {
      const data = event.data as { shopId?: string; reason?: string } | null;
      if ((!shopId || data?.shopId === shopId) && ["reject", "confirmation-expired", "refund-changed"].includes(data?.reason ?? "")) void refresh();
    });
    const connected = realtime.onConnected((again) => { if (again) void refresh(); });
    void refresh();
    return () => { abort.abort(); off(); connected(); };
  }, [userId, realtime]);
  const count = result && result.userId === userId ? result.count : 0;
  if (count <= 0) return null;
  return <span aria-live="polite" aria-label={`${count} unresolved refunds`} title="Required refunds and requests still needing resolution" className="inline-flex min-w-5 items-center justify-center rounded-full bg-emerald-600 px-1.5 py-0.5 text-xs font-bold leading-4 text-white">{count}</span>;
}
