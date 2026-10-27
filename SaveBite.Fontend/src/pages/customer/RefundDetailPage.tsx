import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { getApiErrorMessage } from "@/shared/api";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { refundApi } from "@/features/refunds/api/refundApi";
import { CustomerRefundDetailContent } from "@/features/refunds/components/CustomerRefundDetailContent";
import { RefundStatusBadge } from "@/features/refunds/components/RefundStatusBadge";
import type { Refund } from "@/features/refunds/types/refund.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";

export function CustomerRefundDetailPage() {
  const { refundId = "" } = useParams();
  return <RefundDetail key={refundId} refundId={refundId} />;
}
function RefundDetail({ refundId }: { refundId: string }) {
  const [refund, setRefund] = useState<Refund | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [revision, setRevision] = useState(0);
  const operation = useRef(false);
  const alive = useRef(true);
  const realtime = useRealtimeSubscriptions();
  useEffect(() => {
    alive.current = true;
    return () => {
      alive.current = false;
    };
  }, []);
  useEffect(() => {
    if (busy) return;
    const abort = new AbortController();
    void refundApi
      .detail(refundId, abort.signal)
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
  }, [refundId, revision, busy]);
  useEffect(() => {
    const off = realtime.on(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, (event) => {
      const data = event.data as { orderId?: string; reason?: string } | null;
      if (data?.orderId === refund?.orderId && data?.reason === "refund-changed")
        setRevision((old) => old + 1);
    });
    const connected = realtime.onConnected((again) => {
      if (again) setRevision((old) => old + 1);
    });
    return () => {
      off();
      connected();
    };
  }, [realtime, refund?.orderId]);
  async function mutate(action: () => Promise<Refund>) {
    if (operation.current) return;
    operation.current = true;
    setBusy(true);
    setError("");
    try {
      const changed = await action();
      if (alive.current) setRefund(changed);
    } catch (e) {
      if (alive.current) setError(getApiErrorMessage(e));
    } finally {
      operation.current = false;
      if (alive.current) setBusy(false);
    }
  }
  return (
    <div className="w-full min-w-0 space-y-4">
      <Link
        to={APP_PATHS.ACCOUNT_REFUNDS}
        className="inline-flex items-center gap-2 text-sm font-medium text-neutral-500 hover:text-emerald-700"
      >
        <ArrowLeft size={16} />
        My refunds
      </Link>
      <header>
        <h1 className="text-xl font-bold text-neutral-900">Refund details</h1>
        <p className="mt-1 text-sm text-neutral-500">
          Follow your refund, confirmed destination and complete decision history.
        </p>
      </header>
      {error && (
        <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {error}
          <button
            type="button"
            disabled={busy}
            onClick={() => setRevision((old) => old + 1)}
            className="ml-3 underline"
          >
            Retry
          </button>
        </p>
      )}
      {!refund && !error && (
        <p role="status" className="py-6 text-sm text-neutral-500">
          Loading refund…
        </p>
      )}
      {refund && (
        <>
          <section className="rounded-xl border border-neutral-200 bg-white p-4 sm:p-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex min-w-0 items-center gap-3">
                <span className="relative flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-neutral-200 bg-neutral-50 text-neutral-400">
                  <Store size={24} />
                  {refund.shopLogoUrl && (
                    <img
                      src={refund.shopLogoUrl}
                      alt={`${refund.shopName} logo`}
                      onError={(event) => {
                        event.currentTarget.style.display = "none";
                      }}
                      className="absolute inset-0 size-full object-cover"
                    />
                  )}
                </span>
                <div className="min-w-0">
                  <h2 className="break-words text-sm font-semibold">{refund.shopName}</h2>
                  <p className="mt-1 text-xs text-neutral-500">
                    Order {refund.orderCode} · Requested {checkoutTime(refund.createdAt)} · GMT+7
                  </p>
                </div>
              </div>
              <RefundStatusBadge status={refund.status} overdue={refund.isOverdue} />
            </div>
          </section>
          <CustomerRefundDetailContent refund={refund} busy={busy} mutate={mutate} />
        </>
      )}
    </div>
  );
}
