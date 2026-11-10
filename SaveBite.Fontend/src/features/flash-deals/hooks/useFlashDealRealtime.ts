import { useEffect, useRef, useState } from "react";
import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";
import { API_URL } from "@/shared/api/httpClient";
import type { RealtimeEvent } from "@/shared/realtime/realtime.types";
import { emitFeedbackSync } from "@/features/feedbacks/utils/feedbackSync";

export interface FlashDealRealtimePayload {
  dealId?: string;
  shopId?: string;
  action?: string;
  status?: string;
  visibilityVersion?: number;
}

// Public pages keep their subscription even when the deal becomes unavailable.
export function useFlashDealRealtime(
  kind: "shop" | "deal" | "all",
  id: string | undefined,
  refresh: (eventData?: FlashDealRealtimePayload) => void,
) {
  const [connectionStatus, setConnectionStatus] = useState<
    "connecting" | "connected" | "disconnected"
  >("connecting");
  const callback = useRef(refresh);
  useEffect(() => {
    callback.current = refresh;
  }, [refresh]);
  useEffect(() => {
    if (kind !== "all" && !id) return;
    let stopped = false;
    let retry: ReturnType<typeof setTimeout> | undefined;
    let pending: ReturnType<typeof setTimeout> | undefined;
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL.replace(/\/$/, "")}/flash-deals-realtime`, {
        transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Error)
      .build();
    const refreshSoon = (payload?: FlashDealRealtimePayload) => {
      clearTimeout(pending);
      pending = setTimeout(() => {
        if (!stopped) callback.current(payload);
      }, 100);
    };
    let hasJoinedOnce = false;
    const join = async () => {
      if (kind === "shop" && id) {
        await connection.invoke("WatchShop", id);
      } else if (kind === "deal" && id) {
        await connection.invoke("WatchDeal", id);
      } else if (kind === "all") {
        await connection.invoke("WatchFeed");
      }
      if (!stopped) setConnectionStatus("connected");
      // Only refresh on reconnect (not on initial mount when component already fetches data)
      if (hasJoinedOnce) {
        refreshSoon();
      } else {
        hasJoinedOnce = true;
      }
    };
    connection.on("ReceiveEvent", (event: RealtimeEvent<any>) => {
      if (event.version !== 1) return;
      if (event.type === "flashdeal.changed") {
        const payload = event.data as FlashDealRealtimePayload | undefined;
        window.dispatchEvent(new CustomEvent("flashdeal:changed", { detail: payload }));
        if (kind === "all") {
          const act = payload?.action?.toLowerCase();
          const st = payload?.status?.toLowerCase();
          // Hidden / Closed / Cancelled deals must be dropped from active feed immediately without delay!
          if (act === "hidden" || st === "hidden" || act === "closed" || st === "closed" || act === "cancelled" || st === "cancelled") {
            if (!stopped) callback.current(payload);
          } else {
            refreshSoon(payload);
          }
          return;
        }
        const receivedId = kind === "shop" ? payload?.shopId : payload?.dealId;
        if (
          (id && receivedId?.toLowerCase() === id.toLowerCase()) ||
          (kind === "deal" && id && payload?.dealId?.toLowerCase() === id.toLowerCase())
        ) {
          refreshSoon(payload);
        }
      } else if (event.type === "shop.status.changed") {
        const changedShopId = event.data?.shopId;
        window.dispatchEvent(new CustomEvent("shop:status-changed", { detail: event.data }));
        const isInactive = !event.data?.status || event.data?.status.toLowerCase() !== "active" || Boolean(event.data?.isBlocked);
        if (kind === "all") {
          refreshSoon();
        } else if (kind === "shop" && id && changedShopId?.toLowerCase() === id.toLowerCase()) {
          if (!isInactive) {
            refreshSoon();
          }
        } else if (kind === "deal") {
          refreshSoon();
        }
      } else if (event.type === "feedback.changed") {
        emitFeedbackSync(event.data?.action ?? "hide", event.data?.feedbackId, event.data);
        if (kind === "all") {
          refreshSoon();
        } else if (kind === "shop" && id && event.data?.shopId?.toLowerCase() === id.toLowerCase()) {
          refreshSoon();
        }
      }
    });
    async function start() {
      if (stopped || connection.state !== HubConnectionState.Disconnected) return;
      try {
        await connection.start();
        if (!stopped) await join();
      } catch (error) {
        if (!stopped) {
          setConnectionStatus("disconnected");
          console.warn("Flash deal realtime connection/subscription failed", error);
          await connection.stop();
          clearTimeout(retry);
          retry = setTimeout(() => void start(), 3000);
        }
      }
    }
    connection.onreconnecting(() => {
      if (!stopped) setConnectionStatus("disconnected");
    });
    connection.onreconnected(() => {
      void join().catch(() => {
        void connection.stop();
      });
    });
    connection.onclose(() => {
      if (!stopped) {
        setConnectionStatus("disconnected");
        retry = setTimeout(() => void start(), 3000);
      }
    });
    void start();
    const onVisible = () => {
      if (document.visibilityState === "visible") refreshSoon();
    };
    document.addEventListener("visibilitychange", onVisible);
    return () => {
      stopped = true;
      clearTimeout(retry);
      clearTimeout(pending);
      document.removeEventListener("visibilitychange", onVisible);
      void connection.stop();
    };
  }, [kind, id]);
  return connectionStatus;
}
