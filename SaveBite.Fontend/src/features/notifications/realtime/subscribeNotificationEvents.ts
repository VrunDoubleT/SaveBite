import type { createRealtimeClient } from "@/shared/realtime/realtimeClient";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { useNotificationStore } from "../stores/notificationStore";
import { useAuthStore } from "@/shared/stores/authStore";
import { useAccountSuspensionStore } from "@/shared/stores/accountSuspensionStore";
import type {
  NotificationRealtimePayload,
  NotificationStateSyncPayload,
} from "../types/notification.types";

export function subscribeNotificationEvents(
  client: ReturnType<typeof createRealtimeClient>,
) {
  const offReceived = client.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, (event) => {
    const payload = event.data as NotificationRealtimePayload;
    useNotificationStore
      .getState()
      .onNotificationReceived(payload);
    // Realtime invalidate feedback when feedback notification arrives (e.g. FeedbackRemovedByAdmin = 31)
    const notiType = payload?.notification?.type;
    if (notiType === 31 || notiType === 25 || notiType === 26 || notiType === "FeedbackRemovedByAdmin") {
      window.dispatchEvent(new CustomEvent("feedback:invalidated"));
    }
    // Realtime role and shop application changes: 12, 13, 14, 15, 16, 24, 29, 30, 37, 38, 42
    const roleAppTypes = [12, 13, 14, 15, 16, 24, 29, 30, 37, 38, 42, "ShopApplicationApproved", "ShopApplicationRejected", "ShopApplicationNeedsRevision", "RoleChanged", "CustomerStatusChanged"];
    if (roleAppTypes.includes(notiType as any)) {
      void useAuthStore.getState().refreshUser();
      window.dispatchEvent(new CustomEvent("shop-application:updated"));
      window.dispatchEvent(new CustomEvent("user:role-changed"));
    }
  });

  const offRead = client.on(REALTIME_EVENTS.NOTIFICATION_READ, (event) => {
    useNotificationStore
      .getState()
      .onNotificationRead(event.data as NotificationStateSyncPayload);
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
