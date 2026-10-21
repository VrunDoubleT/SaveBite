import { RealtimeContext } from "@/shared/realtime/realtimeContext";
import { createRealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { useShopOrderEventsStore } from "@/features/shop-orders/stores/shopOrderEventsStore";
import { useEffect, useState, type PropsWithChildren } from "react";
import { useAuthStore } from "@/shared/stores/authStore";
import { createRealtimeClient } from "@/shared/realtime/realtimeClient";
import { subscribeCartEvents } from "@/features/cart/realtime/subscribeCartEvents";

// Own the connection at session level instead of starting a socket in each page or header.
export function RealtimeProvider({ children }: PropsWithChildren) {
  const [subscriptions] = useState(createRealtimeSubscriptions);
  const userId = useAuthStore((state) => state.user?.id);
  const authenticated = useAuthStore((state) => state.isAuthenticated);
  const customerActive = useAuthStore((state) => state.user?.customerStatus === "active");
  useEffect(() => {
    if (!authenticated || !userId) return;
    const client = createRealtimeClient();
    const detach = subscriptions.attach(client);
    const unsubscribe = customerActive ? subscribeCartEvents(client) : () => {};
    void client.start();
    return () => {
      unsubscribe();
      detach();
      useShopOrderEventsStore.getState().reset();
      void client.stop().catch(() => {});
    };
  }, [authenticated, userId, customerActive, subscriptions]);
  return <RealtimeContext.Provider value={subscriptions}>{children}</RealtimeContext.Provider>;
}
