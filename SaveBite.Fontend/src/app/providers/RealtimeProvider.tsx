import { RealtimeContext } from "@/shared/realtime/realtimeContext";
import { createRealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { useShopOrderEventsStore } from "@/features/shop-orders/stores/shopOrderEventsStore";
import { useEffect, useState, type PropsWithChildren } from "react";
import { useAuthStore } from "@/shared/stores/authStore";
import { createRealtimeClient } from "@/shared/realtime/realtimeClient";
import { subscribeCartEvents } from "@/features/cart/realtime/subscribeCartEvents";
import { subscribeNotificationEvents } from "@/features/notifications/realtime/subscribeNotificationEvents";
import { useNotificationStore } from "@/features/notifications/stores/notificationStore";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { emitFeedbackSync } from "@/features/feedbacks/utils/feedbackSync";

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
    const unsubscribeCart = customerActive ? subscribeCartEvents(client) : () => {};
    const unsubscribeNotifications = subscribeNotificationEvents(client);
    const unsubscribeFeedback = client.on(REALTIME_EVENTS.FEEDBACK_CHANGED, (event) => {
      const data = event.data as { feedbackId?: string; action?: string; shopId?: string; rating?: number };
      if (data) {
        emitFeedbackSync(data.action ?? "hide", data.feedbackId, data);
      }
    });
    const unsubscribeShopStatus = client.on(REALTIME_EVENTS.SHOP_STATUS_CHANGED, (event) => {
      window.dispatchEvent(new CustomEvent("shop:status-changed", { detail: event.data }));
    });
    const unsubscribeShopApp = client.on("shop.application.reviewed", () => {
      void useAuthStore.getState().refreshUser();
      window.dispatchEvent(new CustomEvent("shop-application:updated"));
      window.dispatchEvent(new CustomEvent("user:role-changed"));
    });
    const unsubscribeRole = client.on("user.role.changed", () => {
      void useAuthStore.getState().refreshUser();
      window.dispatchEvent(new CustomEvent("user:role-changed"));
    });
    void client.start();
    return () => {
      unsubscribeRole();
      unsubscribeShopApp();
      unsubscribeShopStatus();
      unsubscribeFeedback();
      unsubscribeNotifications();
      unsubscribeCart();
      detach();
      useShopOrderEventsStore.getState().reset();
      useNotificationStore.getState().reset();
      void client.stop().catch(() => {});
    };
  }, [authenticated, userId, customerActive, subscriptions]);
  return <RealtimeContext.Provider value={subscriptions}>{children}</RealtimeContext.Provider>;
}
