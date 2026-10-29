import { useEffect, useState } from "react";
import { useAuthStore } from "@/shared/stores/authStore";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { refundApi } from "../api/refundApi";

// Keep only a count in account navigation; events and reconnects refresh it without polling.
export function RefundsBadge() {
  const userId = useAuthStore((state) => state.user?.id);
  const realtime = useRealtimeSubscriptions();
  const [result, setResult] = useState<{ userId: string; count: number } | null>(null);
  useEffect(() => {
    if (!userId) return;
    const abort = new AbortController();
    let reading = false;
    let dirty = false;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        while (dirty && !abort.signal.aborted) {
          dirty = false;
          const count = await refundApi.pendingDetailsCount(abort.signal);
          if (!abort.signal.aborted) setResult({ userId: userId!, count });
        }
      } catch {
        if (!abort.signal.aborted) setResult({ userId: userId!, count: 0 });
      } finally {
        reading = false;
      }
    }
    const off = realtime.on(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, (event) => {
      const data = event.data as { reason?: string } | null;
      if (["reject", "confirmation-expired", "refund-changed"].includes(data?.reason ?? ""))
        void refresh();
    });
    const connected = realtime.onConnected((reconnected) => {
      if (reconnected) void refresh();
    });
    void refresh();
    return () => {
      abort.abort();
      off();
      connected();
    };
  }, [userId, realtime]);
  const count = result && result.userId === userId ? result.count : 0;
  if (count <= 0) return null;
  return (
    <span
      aria-live="polite"
      aria-label={`${count} refunds need your bank details`}
      title="Shop refunds waiting for your bank details or email confirmation"
      className="ml-auto inline-flex min-w-5 items-center justify-center rounded-full bg-emerald-600 px-1.5 py-0.5 text-xs font-bold leading-4 text-white"
    >
      {count}
    </span>
  );
}
