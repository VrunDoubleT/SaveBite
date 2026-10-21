import type { RealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { useShopOrderEventsStore } from "../stores/shopOrderEventsStore";

export function subscribeShopOrderEvents(client: RealtimeSubscriptions) {
  const offEvent = client.on(REALTIME_EVENTS.SHOP_ORDERS_CHANGED, (event) => {
    const data = event.data as { shopId?: unknown; orderId?: unknown } | null;
    if (data && typeof data.shopId === "string") useShopOrderEventsStore.getState().changed(data.shopId, typeof data.orderId === "string" ? data.orderId : undefined);
  });
  const offConnected = client.onConnected((reconnected) => {
    if (reconnected) useShopOrderEventsStore.getState().reconnected();
  });
  return () => { offEvent(); offConnected(); };
}
