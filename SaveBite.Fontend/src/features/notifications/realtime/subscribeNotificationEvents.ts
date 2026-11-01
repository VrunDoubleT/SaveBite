import type { createRealtimeClient } from "@/shared/realtime/realtimeClient";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { useNotificationStore } from "../stores/notificationStore";
import type {
  NotificationRealtimePayload,
  NotificationStateSyncPayload,
} from "../types/notification.types";

export function subscribeNotificationEvents(
  client: ReturnType<typeof createRealtimeClient>,
) {
  const offReceived = client.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, (event) => {
    useNotificationStore
      .getState()
      .onNotificationReceived(event.data as NotificationRealtimePayload);
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

  const offConnected = client.onConnected(() => {
    void useNotificationStore.getState().fetchNotifications();
  });

  return () => {
    offReceived();
    offRead();
    offDeleted();
    offConnected();
  };
}
