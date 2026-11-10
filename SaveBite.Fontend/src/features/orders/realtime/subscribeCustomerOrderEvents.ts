import type { RealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";

// Pages opt in while mounted; detail pages refresh only for their own order.
export function subscribeCustomerOrderEvents(client: RealtimeSubscriptions, refresh: () => void, orderId?: string) {
  const offEvent = client.on(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, (event) => {
    const data = event.data as { orderId?: unknown } | null;
    if (typeof data?.orderId === "string" && (!orderId || data.orderId === orderId)) refresh();
  });
  const offConnected = client.onConnected(() => refresh());
  // Durable order notifications can arrive even if the best-effort order refresh was missed.
  const offNotification = client.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, (event) => {
    const data = event.data as { notification?: { actionUrl?: unknown } } | null;
    const actionUrl = data?.notification?.actionUrl;
    if (typeof actionUrl !== "string") return;
    const match = /^\/orders\/([^/?#]+)(?:[/?#]|$)/.exec(actionUrl);
    if (match && (!orderId || match[1] === orderId)) refresh();
  });
  return () => { offEvent(); offConnected(); offNotification(); };
}
