import { useAuthStore } from "@/shared/stores/authStore";
import type { RealtimeSubscriptions } from "@/shared/realtime/realtimeSubscriptions";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";

// Events invalidate account data; /me remains authoritative for roles and shop access.
export function subscribeAccountEvents(client: RealtimeSubscriptions, userId: string) {
  let active = true;
  let running = false;
  let pending = false;
  let failures = 0;
  let timer: ReturnType<typeof setTimeout> | undefined;
  const reviewed = new Set<string>();
  const isCurrentSession = () => {
    const state = useAuthStore.getState();
    return active && state.isAuthenticated && state.user?.id === userId;
  };

  function queueRefresh(delay = 50) {
    if (!isCurrentSession()) return;
    pending = true;
    if (running || timer) return;
    timer = setTimeout(() => {
      timer = undefined;
      void refresh();
    }, delay);
  }

  async function refresh() {
    if (!isCurrentSession()) return;
    pending = false;
    running = true;
    let retryDelay = 50;
    try {
      await useAuthStore.getState().refreshUser();
      failures = 0;
      if (isCurrentSession()) window.dispatchEvent(new CustomEvent("user:role-changed"));
    } catch {
      // Preserve the current session and retry transient failures without unhandled rejections.
      pending = true;
      retryDelay = Math.min(30_000, 1000 * 2 ** Math.min(failures++, 5));
    } finally {
      running = false;
      if (pending) queueRefresh(retryDelay);
    }
  }

  const offRole = client.on(REALTIME_EVENTS.USER_ROLE_CHANGED, (event) => {
    const data = event.data as { userId?: string } | null;
    if (data?.userId === userId) queueRefresh();
  });
  const offCustomerStatus = client.on(REALTIME_EVENTS.CUSTOMER_STATUS_CHANGED, (event) => {
    const data = event.data as { userId?: string } | null;
    if (data?.userId === userId) queueRefresh();
  });
  // Membership notifications are private and persisted. They also invalidate /me
  // if the separate role event was missed or the notification arrives first.
  const offMembership = client.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, (event) => {
    const data = event.data as { notification?: { type?: number | string } } | null;
    const type = data?.notification?.type;
    if (type === 37 || type === "StaffStatusChanged" || type === 24 || type === "StaffJoinedShop")
      queueRefresh();
  });
  const offApplication = client.on(REALTIME_EVENTS.SHOP_APPLICATION_REVIEWED, (event) => {
    const data = event.data as {
      applicantUserId?: string;
      applicationId?: string;
      revisionNumber?: number;
      status?: string;
    } | null;
    if (!data?.applicationId || !data.status || !isCurrentSession()) return;
    const key = `${data.applicationId}:${data.revisionNumber}:${data.status}`;
    if (reviewed.has(key)) return;
    reviewed.add(key);
    if (reviewed.size > 128) reviewed.delete(reviewed.values().next().value!);
    // Admin application lists also consume this existing local invalidation event.
    window.dispatchEvent(new CustomEvent("shop-application:updated"));
    if (data.applicantUserId === userId) queueRefresh();
  });
  // Reconcile on first connect too: a role may change between initial /me and hub startup.
  const offConnected = client.onConnected(() => queueRefresh());
  const onLocalRefresh = () => queueRefresh();
  window.addEventListener("account:refresh-requested", onLocalRefresh);
  // Recover changes missed while this account tab was in the background.
  window.addEventListener("focus", onLocalRefresh);

  return () => {
    active = false;
    clearTimeout(timer);
    offRole();
    offCustomerStatus();
    offMembership();
    offApplication();
    offConnected();
    window.removeEventListener("account:refresh-requested", onLocalRefresh);
    window.removeEventListener("focus", onLocalRefresh);
  };
}
