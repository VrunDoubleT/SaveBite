import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { refundApi } from "@/features/refunds/api/refundApi";
import { ShopRefundCard } from "@/features/refunds/components/ShopRefundCard";
import type { Refund } from "@/features/refunds/types/refund.types";
import { getApiErrorMessage } from "@/shared/api";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
export function ShopRefundDetailPage() {
  const { refundId = "" } = useParams();
  return <ShopRefundDetail key={refundId} refundId={refundId} />;
}
function ShopRefundDetail({ refundId }: { refundId: string }) {
  const [refund, setRefund] = useState<Refund | null>(null);
  const [error, setError] = useState("");
  const [revision, setRevision] = useState(0);
  const realtime = useRealtimeSubscriptions();
  useEffect(() => {
    const abort = new AbortController();
    void refundApi
      .shopDetail(refundId, abort.signal)
      .then((data) => {
        if (!abort.signal.aborted) {
          setRefund(data);
          setError("");
        }
      })
      .catch((e) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(e));
      });
    return () => abort.abort();
  }, [refundId, revision]);
  useEffect(() => {
    const off = realtime.on(REALTIME_EVENTS.SHOP_ORDERS_CHANGED, (event) => {
      const change = event.data as { shopId?: string; orderId?: string } | null;
      if (change?.shopId === refund?.shopId && change?.orderId === refund?.orderId)
        setRevision((value) => value + 1);
    });
    const connected = realtime.onConnected((again) => {
      if (again) setRevision((value) => value + 1);
    });
    return () => {
      off();
      connected();
    };
  }, [realtime, refund?.shopId, refund?.orderId]);
  return (
    <div className="w-full min-w-0 space-y-4">
      <Link
        to="/store-owner/refunds"
        className="inline-flex items-center gap-1 text-sm text-neutral-500 hover:text-emerald-700"
      >
        <ArrowLeft size={16} />
        All refunds
      </Link>
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold">Refund details</h1>
          {refund && (
            <p className="mt-1 text-sm text-neutral-500">
              Order {refund.orderCode} · {refund.customerName || "Customer"}
            </p>
          )}
        </div>
        {refund && (
          <Link
            to={`/store-owner/orders/${refund.orderId}`}
            className="rounded-lg border border-neutral-200 px-3 py-2 text-xs font-medium"
          >
            View order
          </Link>
        )}
      </header>
      {error && (
        <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {error}
          <button className="ml-2 underline" onClick={() => setRevision((value) => value + 1)}>
            Retry
          </button>
        </p>
      )}
      {!refund && !error && <p className="py-6 text-sm text-neutral-500">Loading refund…</p>}
      {refund && (
        <ShopRefundCard
          key={`${refund.id}:${refund.status}:${refund.amount}`}
          refund={refund}
          onChanged={setRefund}
        />
      )}
    </div>
  );
}
