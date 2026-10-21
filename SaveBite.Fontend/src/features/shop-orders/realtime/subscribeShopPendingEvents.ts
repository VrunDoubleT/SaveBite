import type { RealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";

// Outside Orders, only invalidate the current shop's lightweight Pending count.
export function subscribeShopPendingEvents(client: RealtimeSubscriptions, shopId: string, refresh: () => void) {
  const offEvent = client.on(REALTIME_EVENTS.SHOP_PENDING_ORDERS_CHANGED, (event) => {
    const data = event.data as { shopId?: unknown } | null;
    if (data?.shopId === shopId) refresh();
  });
  const offConnected = client.onConnected(() => refresh());
  return () => { offEvent(); offConnected(); };
}
