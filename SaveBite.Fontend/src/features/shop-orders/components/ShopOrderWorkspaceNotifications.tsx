import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { getApiErrorMessage } from "@/shared/api";
import { getApiErrorStatus } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import { shopOrderApi } from "../api/shopOrderApi";
import { subscribeShopOrderEvents } from "../realtime/subscribeShopOrderEvents";
import { subscribeShopPendingEvents } from "../realtime/subscribeShopPendingEvents";

// The workspace switches subscriptions without opening another SignalR connection.
export function ShopOrderWorkspaceNotifications({ shopId, ordersPath }: { shopId?: string; ordersPath: string }) {
  const { pathname } = useLocation();
  const viewingOrders = pathname === ordersPath || pathname.startsWith(`${ordersPath}/`);
  return viewingOrders ? <ShopOrderLiveSubscription /> : <PendingOrdersBadge key={shopId ?? "owner"} shopId={shopId} />;
}

function ShopOrderLiveSubscription() {
  const realtime = useRealtimeSubscriptions();
  useEffect(() => subscribeShopOrderEvents(realtime), [realtime]);
  return null;
}

function PendingOrdersBadge({ shopId: selectedShopId }: { shopId?: string }) {
  const realtime = useRealtimeSubscriptions();
  const [shopId, setShopId] = useState(selectedShopId ?? "");
  const [count, setCount] = useState<number | null>(null);
  const [error, setError] = useState("");
  useEffect(() => {
    if (selectedShopId) return;
    const abort = new AbortController();
    void shopOrderApi.ownerShop(abort.signal).then((id) => {
      if (!abort.signal.aborted) setShopId(id);
    }).catch((e) => { if (!abort.signal.aborted) setError(getApiErrorMessage(e)); });
    return () => abort.abort();
  }, [selectedShopId]);
  useEffect(() => {
    if (!shopId) return;
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
          const next = await shopOrderApi.pendingCount(shopId, abort.signal);
          if (!abort.signal.aborted) { setCount(next); setError(""); }
        }
      } catch (e) {
        if (!abort.signal.aborted) {
          const message = getApiErrorStatus(e) === 404
            ? "Pending order count is unavailable. Restart the backend to load the latest Orders API, then retry."
            : `Unable to load pending orders: ${getApiErrorMessage(e)}`;
          setError(message);
          toast.error(message);
        }
      } finally { reading = false; }
    }
    const off = subscribeShopPendingEvents(realtime, shopId, () => { void refresh(); });
    void refresh();
    return () => { off(); abort.abort(); };
  }, [shopId, realtime]);
  // Show only a real count; keep loading and request errors out of the numeric badge.
  if (count === null || count <= 0) return null;
  return (
    <span title={error || "Pending orders"}
      aria-label={`${count} pending orders`}
      aria-live="polite"
      className="inline-flex min-w-5 items-center justify-center rounded-full bg-emerald-600 px-1.5 py-0.5 text-xs font-bold leading-4 text-white">
      {count}
    </span>
  );
}
