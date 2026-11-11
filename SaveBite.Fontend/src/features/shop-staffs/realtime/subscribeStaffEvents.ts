import type { RealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";

type List = "invitations" | "staff";

// Both lists use the session's existing socket. Serialize invalidations so an
// event arriving during a fetch always results in a subsequent fresh fetch.
export function subscribeStaffEvents(
  client: RealtimeSubscriptions,
  list: List,
  refresh: () => Promise<void>,
) {
  let active = true;
  let running = false;
  let pending = false;
  let timer: ReturnType<typeof setTimeout> | undefined;
  function queue() {
    if (!active) return;
    pending = true;
    if (running || timer) return;
    timer = setTimeout(() => {
      timer = undefined;
      void flush();
    }, 50);
  }
  async function flush() {
    if (!active) return;
    pending = false;
    running = true;
    try {
      await refresh();
    } catch {
      /* Page loaders display fetch failures. */
    } finally {
      running = false;
      if (pending) queue();
    }
  }
  const offChanged = client.on(
    list === "staff"
      ? REALTIME_EVENTS.SHOP_STAFF_CHANGED
      : REALTIME_EVENTS.STAFF_INVITATIONS_CHANGED,
    queue,
  );
  const offNotification = client.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, (event) => {
    const data = event.data as { notification?: { type?: number | string } } | null;
    const type = data?.notification?.type;
    const types: (number | string)[] =
      list === "staff"
        ? [13, 37, 38, "StaffInvitationAccepted", "StaffStatusChanged", "StaffLeftShop"]
        : [
            12,
            13,
            22,
            23,
            24,
            "StaffInvitationReceived",
            "StaffInvitationAccepted",
            "StaffInvitationDeclined",
            "StaffInvitationExpired",
            "StaffJoinedShop",
          ];
    if (type !== undefined && types.includes(type)) queue();
  });
  const offConnected = client.onConnected(queue);
  return () => {
    active = false;
    clearTimeout(timer);
    offChanged();
    offNotification();
    offConnected();
  };
}
