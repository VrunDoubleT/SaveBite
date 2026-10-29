import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useShopOrderEventsStore } from "@/features/shop-orders/stores/shopOrderEventsStore";
import { getApiErrorMessage } from "@/shared/api";
import { refundApi } from "../api/refundApi";
import type { Refund } from "../types/refund.types";
import { ShopRefundCard } from "./ShopRefundCard";
export function ShopOrderRefunds({ shopId, orderId }: { shopId: string; orderId: string }) {
  const [refunds, setRefunds] = useState<Refund[]>([]);
  const [error, setError] = useState("");
  const revision = useShopOrderEventsStore(
    (state) => state.orderRevisions[`${shopId}:${orderId}`] ?? 0,
  );
  const reconnected = useShopOrderEventsStore((state) => state.reconnectRevision);
  useEffect(() => {
    const abort = new AbortController();
    void refundApi
      .shopOrder(orderId, abort.signal)
      .then((items) => {
        if (!abort.signal.aborted) setRefunds(items);
      })
      .catch((e) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(e));
      });
    return () => abort.abort();
  }, [shopId, orderId, revision, reconnected]);
  if (!refunds.length && !error) return null;
  return (
    <section className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="text-sm font-semibold">Refunds for this order</h2>
        <Link to="/store-owner/refunds" className="text-xs text-emerald-700 hover:underline">
          All refunds
        </Link>
      </div>
      {error && (
        <p role="alert" className="text-xs text-red-700">
          {error}
        </p>
      )}
      {refunds.map((refund) => (
        <ShopRefundCard
          key={`${refund.id}:${refund.status}:${refund.amount}`}
          refund={refund}
          onChanged={(changed) =>
            setRefunds((old) => old.map((item) => (item.id === changed.id ? changed : item)))
          }
        />
      ))}
    </section>
  );
}
