import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { CustomerActionLink } from "@/shared/components/CustomerMutationControls";
import { Badge, Card } from "@/shared/components/ui";
import { Link } from "react-router-dom";
import { RefundStatusBadge } from "./RefundStatusBadge";
import { refundEligibility } from "../utils/refundEligibility";
import { APP_PATHS } from "@/app/router/paths";
import { useEffect, useState } from "react";
import { RotateCcw } from "lucide-react";
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
  const [now, setNow] = useState(Date.now);
  const realtime = useRealtimeSubscriptions();
  useEffect(() => {
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
          const items = await refundApi.forOrder(order.id, abort.signal);
          if (!abort.signal.aborted) {
            setRefunds(items);
            setLoaded(true);
            setError("");
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
    const timer = window.setInterval(() => setNow(Date.now()), 1_000);
    return () => {
      abort.abort();
      off();
      window.clearInterval(timer);
    };
  }, [order.id, realtime]);

  const {
    completed,
    deadline,
    hasActive,
    eligible: canRequest,
    reason,
  } = refundEligibility(order, refunds, now);
  const eligible = loaded && !error && refunds.length === 0 && canRequest;
  if (!completed && !refunds.length && !error) return null;

  return (
    <Card aria-label="Order refunds" className="!p-4">
      <section aria-label="Refund options">
        <h2 className="flex min-h-11 flex-wrap items-center gap-2 text-body font-semibold text-text-primary">
          <RotateCcw size={16} className="text-secondary-700" aria-hidden="true" />
          Refunds
          {hasActive && <Badge tone="warning">Action in progress</Badge>}
          {loaded && refunds.length > 0 && <Badge tone="neutral">{refunds.length}</Badge>}
        </h2>
        <div className="border-t border-border-default pt-3">
          {eligible && (
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
            <LoadingSkeleton variant="list" label="Loading refund eligibility…" />
          )}
          {eligible && deadline !== null && (
            <p className="mt-2 text-caption text-text-secondary">
              Request deadline: {checkoutTime(new Date(deadline).toISOString())}
            </p>
          )}
          {loaded && !error && completed && refunds.length === 0 && !eligible && (
            <p role="status" className="mt-2 text-body text-text-secondary">
              {deadline !== null && now > deadline
                ? "The refund request window has closed. You can no longer request a refund for this order."
                : reason}
            </p>
          )}
          <div className="mt-3 space-y-3">
            {refunds.map((refund) => (
              <div
                key={refund.id}
                className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border-default p-3"
              >
                <RefundStatusBadge status={refund.status} overdue={refund.isOverdue} />
                <Link
                  to={APP_PATHS.REFUND_DETAIL.replace(":refundId", encodeURIComponent(refund.id))}
                  className="inline-flex min-h-11 items-center rounded-md px-3 text-body font-semibold text-primary-700 underline underline-offset-4 hover:text-primary-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
                >
                  View refund details
                </Link>
              </div>
            ))}
          </div>
        </div>
      </section>
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
