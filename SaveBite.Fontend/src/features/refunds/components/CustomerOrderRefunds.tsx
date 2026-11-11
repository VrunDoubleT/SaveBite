import { isUploadCancelled } from "@/shared/uploads/uploadErrors";
import { toast } from "@/shared/stores/toastStore";
import { CustomerActionLink } from "@/shared/components/CustomerMutationControls";
import { Badge, Card } from "@/shared/components/ui";
import { RefundCard } from "./RefundCard";
import { refundEligibility } from "../utils/refundEligibility";
import { APP_PATHS } from "@/app/router/paths";
import { useEffect, useRef, useState } from "react";
import { ChevronDown, RotateCcw } from "lucide-react";
import { getApiErrorMessage } from "@/shared/api";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { refundApi } from "../api/refundApi";
import type { Refund } from "../types/refund.types";

export function CustomerOrderRefunds({ order }: { order: CheckoutOrder }) {
  const [refunds, setRefunds] = useState<Refund[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
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
    } catch (e) {
      if (isUploadCancelled(e))
        toast.info(
          "Upload stopped. Your refund draft is saved; edit it or submit again when ready.",
        );
      else if (alive.current) setError(getApiErrorMessage(e));
    } finally {
      operation.current = false;
      if (alive.current) setBusy(false);
    }
  }
  const {
    completed,
    deadline,
    hasActive,
    eligible: canRequest,
    reason,
  } = refundEligibility(order, refunds, now);
  const eligible = loaded && canRequest;
  if (!completed && !refunds.length && !error) return null;

  return (
    <Card aria-label="Order refunds" className="!p-4">
      <details className="group">
        <summary className="flex min-h-11 cursor-pointer list-none items-center gap-2 text-body font-semibold text-text-primary">
          <RotateCcw size={16} className="text-secondary-700" aria-hidden="true" />
          Refunds
          {hasActive && <Badge tone="warning">Action in progress</Badge>}
          {loaded && refunds.length > 0 && <Badge tone="neutral">{refunds.length}</Badge>}
          <ChevronDown
            size={16}
            className="ml-auto shrink-0 transition-transform group-open:rotate-180"
            aria-hidden="true"
          />
        </summary>
        <div className="border-t border-border-default pt-3">
          {completed && (
            <p className="text-caption text-text-secondary">
              Request a refund for affected items within 6 hours of order completion.
            </p>
          )}
          {eligible && (
            <CustomerActionLink
              to={APP_PATHS.ORDER_REFUND_REQUEST.replace(":orderId", encodeURIComponent(order.id))}
              className="mt-3 min-h-11"
            >
              Request refund
            </CustomerActionLink>
          )}
          {!loaded && !error && (
            <p className="mt-3 text-caption text-text-secondary">Loading refund eligibility…</p>
          )}
          {completed && deadline !== null && (
            <p className="mt-2 text-caption text-text-secondary">
              Request deadline: {checkoutTime(new Date(deadline).toISOString())} · GMT+7
            </p>
          )}
          {loaded && completed && !hasActive && !eligible && (
            <p className="mt-2 text-caption text-text-secondary">{reason}</p>
          )}
          <div className="mt-3 space-y-3">
            {refunds.map((refund) => (
              <RefundCard key={refund.id} refund={refund} busy={busy} mutate={mutate} />
            ))}
          </div>
        </div>
      </details>
      {error && (
        <p
          role="alert"
          className="mt-3 rounded-lg bg-danger-soft p-3 text-caption text-danger-strong"
        >
          {error}
        </p>
      )}
    </Card>
  );
}