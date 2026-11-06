import { useCallback, useEffect, useState, type ReactNode } from "react";
import { getApiErrorMessage } from "@/shared/api";
import { shopOrderApi } from "@/features/shop-orders/api/shopOrderApi";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { useAuthStore } from "@/shared/stores/authStore";
import { RestockContext } from "./restockContext";
import { restockApi, type RestockSummary } from "./restockApi";

export function RestockWorkspaceProvider({
  shopId,
  children,
}: {
  shopId?: string;
  children: ReactNode;
}) {
  const userId = useAuthStore((state) => state.user?.id);
  return (
    <Workspace key={`${userId}:${shopId}`} selectedShopId={shopId}>
      {children}
    </Workspace>
  );
}
function Workspace({ selectedShopId, children }: { selectedShopId?: string; children: ReactNode }) {
  const [shopId, setShopId] = useState(selectedShopId ?? "");
  const [summary, setSummary] = useState<RestockSummary | null>(null);
  const [revision, setRevision] = useState(0);
  const [error, setError] = useState("");
  const refresh = useCallback(() => setRevision((value) => value + 1), []);
  const realtime = useRealtimeSubscriptions();
  useEffect(() => {
    if (selectedShopId || shopId) return;
    const abort = new AbortController();
    void shopOrderApi
      .ownerShop(abort.signal)
      .then((id) => {
        if (!abort.signal.aborted) setShopId(id);
      })
      .catch((cause) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(cause));
      });
    return () => abort.abort();
  }, [selectedShopId, shopId, revision]);
  useEffect(() => {
    if (!shopId) return;
    const abort = new AbortController();
    void restockApi
      .summary(shopId, abort.signal)
      .then((next) => {
        if (!abort.signal.aborted) {
          setSummary(next);
          setError("");
        }
      })
      .catch((cause) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(cause));
      });
    return () => abort.abort();
  }, [shopId, revision]);
  useEffect(() => {
    const off = realtime.on(REALTIME_EVENTS.SHOP_ORDERS_CHANGED, (event) => {
      if ((event.data as { shopId?: string } | null)?.shopId === shopId) refresh();
    });
    const connected = realtime.onConnected(() => refresh());
    return () => {
      off();
      connected();
    };
  }, [shopId, realtime, refresh]);
  useEffect(() => {
    if (!summary?.nextDeadlineAt) return;
    const timer = window.setTimeout(
      refresh,
      Math.max(100, Date.parse(summary.nextDeadlineAt) - Date.now() + 150),
    );
    return () => clearTimeout(timer);
  }, [summary, refresh]);
  return (
    <RestockContext.Provider
      value={{ shopId, pendingCount: summary?.pendingCount ?? null, revision, error, refresh }}
    >
      {summary && (
        <span role="status" aria-live="polite" aria-atomic="true" className="sr-only">
          {summary.pendingCount} orders need restock decisions.
        </span>
      )}
      {children}
    </RestockContext.Provider>
  );
}
