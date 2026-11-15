import { useEffect, useState } from "react";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { adminShopApplicationApi } from "../api/adminShopApplicationApi";

export function AdminShopApplicationsBadge() {
  const realtime = useRealtimeSubscriptions();
  const [count, setCount] = useState(0);

  useEffect(() => {
    let disposed = false;
    let reading = false;
    let dirty = false;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        while (dirty && !disposed) {
          dirty = false;
          const items = await adminShopApplicationApi.getAll();
          if (!disposed) setCount(items.filter((item) => item.status === "Pending").length);
        }
      } catch {
        // Retry on the next update, focus or reconnect.
      } finally {
        reading = false;
      }
    }
    const update = () => {
      void refresh();
    };
    const offNotification = realtime.on(REALTIME_EVENTS.NOTIFICATION_RECEIVED, update);
    const offReview = realtime.on(REALTIME_EVENTS.SHOP_APPLICATION_REVIEWED, update);
    const offConnected = realtime.onConnected((again) => {
      if (again) update();
    });
    window.addEventListener("shop-application:updated", update);
    window.addEventListener("focus", update);
    update();
    return () => {
      disposed = true;
      offNotification();
      offReview();
      offConnected();
      window.removeEventListener("shop-application:updated", update);
      window.removeEventListener("focus", update);
    };
  }, [realtime]);

  if (count <= 0) return null;
  return (
    <span
      aria-live="polite"
      aria-label={`${count} store applications awaiting review`}
      title="Store applications awaiting review"
      className="inline-flex min-w-5 items-center justify-center rounded-full bg-emerald-600 px-1.5 py-0.5 text-xs font-bold leading-4 text-white"
    >
      {count}
    </span>
  );
}
