import { useEffect, useRef } from "react";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";

// Re-read recorded state on events, reconnect and return from a banking app. Never reconcile PayOS here.
export function usePaymentUpdates(
  type: string,
  refresh: (signal: AbortSignal) => Promise<unknown>,
  matches: (data: Record<string, unknown>) => boolean,
) {
  const realtime = useRealtimeSubscriptions();
  const latest = useRef({ refresh, matches });
  useEffect(() => {
    latest.current = { refresh, matches };
  }, [refresh, matches]);
  useEffect(() => {
    const abort = new AbortController();
    let active = true;
    let reading = false;
    let dirty = false;
    async function update() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        while (active && dirty) {
          dirty = false;
          await latest.current.refresh(abort.signal);
        }
      } finally {
        reading = false;
      }
    }
    const request = () => {
      void update().catch(() => {
        /* The owning view displays read errors. */
      });
    };
    const off = realtime.on(type, (event) => {
      if (
        event.data &&
        typeof event.data === "object" &&
        latest.current.matches(event.data as Record<string, unknown>)
      )
        request();
    });
    const offConnected = realtime.onConnected(request);
    const visible = () => {
      if (document.visibilityState === "visible") request();
    };
    document.addEventListener("visibilitychange", visible);
    window.addEventListener("focus", request);
    // Subscribe before reading to cover completion while this view is opening.
    request();
    return () => {
      active = false;
      abort.abort();
      off();
      offConnected();
      document.removeEventListener("visibilitychange", visible);
      window.removeEventListener("focus", request);
    };
  }, [realtime, type]);
}
