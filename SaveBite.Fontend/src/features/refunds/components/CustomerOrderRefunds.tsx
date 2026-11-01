import { RefundCard } from "./RefundCard";
import { isRefundActive as active } from "../utils/refundStatus";
import { RefundRequestForm } from "./RefundRequestForm";
import { useEffect, useRef, useState } from "react";
import { RotateCcw } from "lucide-react";
import { getApiErrorMessage } from "@/shared/api";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { refundApi } from "../api/refundApi";
import type { Refund } from "../types/refund.types";

const button =
  "rounded-lg bg-emerald-700 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-800 disabled:opacity-50";

export function CustomerOrderRefunds({ order }: { order: CheckoutOrder }) {
  const [refunds, setRefunds] = useState<Refund[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [open, setOpen] = useState(false);
  const [now, setNow] = useState(Date.now);
  const operation = useRef(false);
  const revision = useRef(0);
  const alive = useRef(true);
  const realtime = useRealtimeSubscriptions();
  useEffect(() => {
    alive.current = true;
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
          const current = revision.current;
          const items = await refundApi.forOrder(order.id, abort.signal);
          if (!abort.signal.aborted && !operation.current && current === revision.current) {
            setRefunds(items);
            setLoaded(true);
          }
        }
      } catch (e) {
        if (!abort.signal.aborted) setError(getApiErrorMessage(e));
      } finally {
        reading = false;
      }
    }
    void refresh();
    const off = subscribeCustomerOrderEvents(
      realtime,
      () => {
        void refresh();
      },
      order.id,
    );
    const timer = window.setInterval(() => setNow(Date.now()), 30_000);
    return () => {
      alive.current = false;
      abort.abort();
      off();
      window.clearInterval(timer);
    };
  }, [order.id, realtime]);

  async function mutate(action: () => Promise<Refund>) {
    if (operation.current) return;
    operation.current = true;
    revision.current++;
    setBusy(true);
    setError("");
    try {
      const changed = await action();
      if (!alive.current) return;
      setRefunds((items) => [changed, ...items.filter((item) => item.id !== changed.id)]);
      setLoaded(true);
      setOpen(false);
    } catch (e) {
      if (alive.current) setError(getApiErrorMessage(e));
    } finally {
      operation.current = false;
      if (alive.current) setBusy(false);
    }
  }
  const completed = order.status === "Completed";
  const deadline = order.completedAt ? Date.parse(order.completedAt) + 6 * 60 * 60 * 1000 : null;
  const remaining = Math.max(
    0,
    (order.paidAmount ?? 0) -
      refunds.reduce((total, refund) => total + refund.transferredAmount, 0),
  );
  const hasActive = refunds.some(active);
  const hasRequested = refunds.some((refund) => refund.type === "CustomerRequest");
  const eligible =
    loaded &&
    completed &&
    deadline !== null &&
    now <= deadline &&
    remaining > 0 &&
    !hasActive &&
    !hasRequested;
  if (!completed && !refunds.length && !error) return null;

  return (
    <section
      aria-label="Order refunds"
      className="rounded-xl border border-neutral-200 bg-white p-4 sm:p-5"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="flex items-center gap-2 text-sm font-semibold text-neutral-900">
            <RotateCcw size={16} />
            Refunds
          </h2>
          {completed && (
            <p className="mt-1 text-xs text-neutral-500">
              Request a refund for affected items within 6 hours of order completion.
            </p>
          )}
        </div>
        {eligible && (
          <button
            type="button"
            className={button}
            disabled={busy}
            onClick={() => setOpen((value) => !value)}
          >
            Request refund
          </button>
        )}
      </div>
      {error && (
        <p role="alert" className="mt-3 rounded-lg bg-red-50 p-3 text-xs text-red-700">
          {error}
        </p>
      )}
      {!loaded && !error && (
        <p className="mt-3 text-xs text-neutral-500">Loading refund eligibility…</p>
      )}
      {completed && deadline !== null && (
        <p className="mt-2 text-xs text-neutral-500">
          Request deadline: {checkoutTime(new Date(deadline).toISOString())} · GMT+7
        </p>
      )}
      {loaded && completed && !hasActive && !eligible && (
        <p className="mt-2 text-xs text-neutral-600">
          {hasRequested
            ? "This order already has a refund request. A second request cannot be submitted."
            : remaining <= 0
              ? "There is no paid balance left to refund."
              : deadline === null
                ? "The completion time is unavailable. Contact support to check refund eligibility."
                : "The 6-hour refund request window has closed."}
        </p>
      )}
      {eligible && (
        <div hidden={!open}>
          <RefundRequestForm
            order={order}
            busy={busy}
            mutate={mutate}
            onClose={() => setOpen(false)}
          />
        </div>
      )}
      <div className="mt-3 space-y-3">
        {refunds.map((refund) => (
          <RefundCard key={refund.id} refund={refund} busy={busy} mutate={mutate} />
        ))}
      </div>
    </section>
  );
}
