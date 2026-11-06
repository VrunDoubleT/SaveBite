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

// Public pages keep their subscription even when the deal becomes unavailable.
export function useFlashDealRealtime(
  kind: "shop" | "deal" | "all",
  id: string | undefined,
  refresh: () => void,
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
    const refreshSoon = () => {
      clearTimeout(pending);
      pending = setTimeout(() => {
        if (!stopped) callback.current();
      }, 100);
    };
    const join = async () => {
      if (kind === "shop" && id) {
        await connection.invoke("WatchShop", id);
      } else if (kind === "deal" && id) {
        await connection.invoke("WatchDeal", id);
      }
      if (!stopped) setConnectionStatus("connected");
      // Close the race between initial HTTP read and group registration, including reconnect.
      refreshSoon();
    };
    connection.on("ReceiveEvent", (event: RealtimeEvent<any>) => {
      if (event.version !== 1) return;
      if (event.type === "flashdeal.changed") {
        if (kind === "all") {
          refreshSoon();
          return;
        }
        const receivedId = kind === "shop" ? event.data?.shopId : event.data?.dealId;
        if (
          (id && receivedId?.toLowerCase() === id.toLowerCase()) ||
          (kind === "deal" && id && event.data?.dealId?.toLowerCase() === id.toLowerCase())
        ) {
          refreshSoon();
        }
      } else if (event.type === "shop.status.changed") {
        const changedShopId = event.data?.shopId;
        window.dispatchEvent(new CustomEvent("shop:status-changed", { detail: event.data }));
        if (kind === "all") {
          refreshSoon();
        } else if (kind === "shop" && id && changedShopId?.toLowerCase() === id.toLowerCase()) {
          refreshSoon();
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
