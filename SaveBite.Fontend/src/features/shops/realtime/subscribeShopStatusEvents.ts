import type { RealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";

export interface ShopStatusChangedPayload {
  shopId?: string;
  previousStatus?: string;
  status?: string;
  reason?: string | null;
}

// Re-read the shop whenever an admin changes its status, and after a reconnect
// (events sent while the connection was down are not replayed).
export function subscribeShopStatusEvents(client: RealtimeSubscriptions, refresh: () => void) {
  const offChanged = client.on(REALTIME_EVENTS.SHOP_STATUS_CHANGED, () => refresh());
  const offConnected = client.onConnected((reconnected) => {
    if (reconnected) refresh();
  });
  return () => {
    offChanged();
    offConnected();
  };
}
