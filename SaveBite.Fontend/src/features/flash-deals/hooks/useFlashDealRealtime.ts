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
  DealId?: string;
  ShopId?: string;
  Action?: string;
  Status?: string;
  VisibilityVersion?: number;
}

interface ShopStatusEventData {
  shopId?: string;
  ShopId?: string;
  status?: string;
  Status?: string;
  isBlocked?: boolean;
  IsBlocked?: boolean;
}

interface FeedbackEventData {
  action?: string;
  Action?: string;
  feedbackId?: string;
  FeedbackId?: string;
  shopId?: string;
  ShopId?: string;
  productId?: string;
  ProductId?: string;
  rating?: number;
  Rating?: number;
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
    connection.on("ReceiveEvent", (event: RealtimeEvent<unknown>) => {
      if (event.version !== 1) return;
      if (event.type === "flashdeal.changed") {
        const payload = event.data as FlashDealRealtimePayload | undefined;
        window.dispatchEvent(new CustomEvent("flashdeal:changed", { detail: payload }));
        if (kind === "all") {
          const rawAction = payload?.action ?? payload?.Action;
          const act = rawAction ? String(rawAction).toLowerCase() : undefined;
          const rawStatus = payload?.status ?? payload?.Status;
          const st = rawStatus ? String(rawStatus).toLowerCase() : undefined;

          // Hidden / Closed / Cancelled hoặc Opened / Reopened -> gọi trực tiếp callback không bị lag
          if (
            act === "hidden" ||
            st === "hidden" ||
            act === "closed" ||
            st === "closed" ||
            act === "cancelled" ||
            st === "cancelled" ||
            act === "opened" ||
            act === "published" ||
            act === "reopened" ||
            st === "onsale"
          ) {
            if (!stopped) callback.current(payload);
          } else {
            refreshSoon(payload);
          }
          return;
        }
        const rawDealId = payload?.dealId ?? payload?.DealId;
        const rawShopId = payload?.shopId ?? payload?.ShopId;
        const receivedId = kind === "shop" ? rawShopId : rawDealId;
        if (
          (id && receivedId?.toLowerCase() === id.toLowerCase()) ||
          (kind === "deal" && id && rawDealId?.toLowerCase() === id.toLowerCase())
        ) {
          refreshSoon(payload);
        }
      } else if (event.type === "shop.status.changed") {
        const shopData = event.data as ShopStatusEventData | undefined;
        const changedShopId = shopData?.shopId ?? shopData?.ShopId;
        window.dispatchEvent(new CustomEvent("shop:status-changed", { detail: event.data }));
        const isInactive = !shopData?.status || shopData.status.toLowerCase() !== "active" || Boolean(shopData.isBlocked ?? shopData.IsBlocked);
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
        const fbData = event.data as FeedbackEventData | undefined;
        const fbShopId = fbData?.shopId ?? fbData?.ShopId;
        emitFeedbackSync(
          fbData?.action ?? fbData?.Action ?? "hide",
          fbData?.feedbackId ?? fbData?.FeedbackId,
          {
            shopId: fbShopId,
            productId: fbData?.productId ?? fbData?.ProductId,
            rating: fbData?.rating ?? fbData?.Rating,
          },
        );
        if (kind === "all") {
          refreshSoon();
        } else if (kind === "shop" && id && fbShopId?.toLowerCase() === id.toLowerCase()) {
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