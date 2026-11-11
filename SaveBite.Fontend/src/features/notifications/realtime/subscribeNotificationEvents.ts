import type { createRealtimeClient } from "@/shared/realtime/realtimeClient";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { useNotificationStore } from "../stores/notificationStore";
import { useAuthStore } from "@/shared/stores/authStore";
import { useAccountSuspensionStore } from "@/shared/stores/accountSuspensionStore";
import type {
  NotificationRealtimePayload,
  NotificationStateSyncPayload,
} from "../types/notification.types";

export function subscribeNotificationEvents(client: ReturnType<typeof createRealtimeClient>) {
  const offReceived = client.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, (event) => {
    const payload = event.data as NotificationRealtimePayload;
    const notiType = payload?.notification?.type;

    const isFlashDealNotification =
      notiType === 17 ||
      notiType === "FlashDealAvailable" ||
      String(notiType).toUpperCase() === "FLASHDEALAVAILABLE";

    const isCustomerOnHomePage =
      typeof window !== "undefined" &&
      (window.location.pathname === "/" ||
        window.location.pathname === "" ||
        window.location.pathname === "/home");

    // On home page, useNearbyFlashDeals validates the active category filter
    // and handles the bell notification directly with exact filter matching.
    if (isFlashDealNotification && isCustomerOnHomePage) {
      return;
    }

    useNotificationStore.getState().onNotificationReceived(payload);

    // Realtime invalidate feedback when feedback notification arrives
    if (
      notiType === 31 ||
      notiType === 25 ||
      notiType === 26 ||
      notiType === "FeedbackRemovedByAdmin"
    ) {
      window.dispatchEvent(new CustomEvent("feedback:invalidated"));
    }
        // Shop/account status changes may also affect /me. Role/membership changes have their own event.
    if (
      [29, 30, "ShopStatusChanged", "AccountStatusChanged", "CustomerStatusChanged"].includes(
        notiType,
      )
    ) {
      window.dispatchEvent(new CustomEvent("account:refresh-requested"));
    }

    // Realtime invalidate trust score & levels
    if (
      [40, "40", "TrustLevelChanged", 18, "18", "TrustScoreChanged"].includes(
        notiType as string | number,
      )
    ) {
      window.dispatchEvent(new CustomEvent("trust:invalidated"));
    }
  });

  const offRead = client.on(REALTIME_EVENTS.NOTIFICATION_READ, (event) => {
    useNotificationStore.getState().onNotificationRead(event.data as NotificationStateSyncPayload);
  });

  const offDeleted = client.on(REALTIME_EVENTS.NOTIFICATION_DELETED, (event) => {
    useNotificationStore
      .getState()
      .onNotificationDeleted(event.data as NotificationStateSyncPayload);
  });

  const offLocked = client.on("account.locked", (event) => {
    const data = event.data as { reason?: string; isCustomerProfile?: boolean } | undefined;
    useAuthStore.getState().logout();
    window.dispatchEvent(new Event("auth:unauthorized"));
    useAccountSuspensionStore.getState().showSuspensionModal(data);
  });

  const offConnected = client.onConnected(() => {
    void useNotificationStore.getState().fetchNotifications();
  });

  return () => {
    offReceived();
    offRead();
    offDeleted();
    offLocked();
    offConnected();
  };
}