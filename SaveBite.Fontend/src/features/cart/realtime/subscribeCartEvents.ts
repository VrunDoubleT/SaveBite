import type { createRealtimeClient } from "@/shared/realtime/realtimeClient";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { useCartStore } from "../stores/cartStore";

// Fetch authoritative state after a change and after reconnecting to recover missed events.
export function subscribeCartEvents(client: ReturnType<typeof createRealtimeClient>) {
  const offEvent = client.on(REALTIME_EVENTS.CART_CHANGED, () => useCartStore.getState().queueRealtimeRefresh());
  const offConnected = client.onConnected((reconnected) => useCartStore.getState().onRealtimeConnected(reconnected));
  return () => { offEvent(); offConnected(); };
}
