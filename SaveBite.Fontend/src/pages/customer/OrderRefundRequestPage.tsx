import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import {
  ButtonLink,
  Card,
  EmptyState,
  ErrorState,
  PageHeader,
  Skeleton,
} from "@/shared/components/ui";
import { orderApi } from "@/features/orders/api/orderApi";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { RefundRequestForm } from "@/features/refunds/components/RefundRequestForm";
import { refundApi } from "@/features/refunds/api/refundApi";
import type { Refund } from "@/features/refunds/types/refund.types";
import { refundEligibility } from "@/features/refunds/utils/refundEligibility";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";
import { toast } from "@/shared/stores/toastStore";

export function OrderRefundRequestPage() {
  const { orderId = "" } = useParams();
  const userId = useAuthStore((state) => state.user?.id);
  return <RefundRequestPage key={`${userId}:${orderId}`} orderId={orderId} />;
}

function RefundRequestPage({ orderId }: { orderId: string }) {
  const navigate = useNavigate();
  const detailUrl = APP_PATHS.ORDER_DETAIL.replace(":orderId", encodeURIComponent(orderId));
  const [retry, setRetry] = useState(0);
  const [result, setResult] = useState<{ order: CheckoutOrder; refunds: Refund[] } | null>(null);
  const [error, setError] = useState("");
  const [submitError, setSubmitError] = useState("");
  const [busy, setBusy] = useState(false);
  const [now, setNow] = useState(Date.now);
  const operation = useRef(false);
  const alive = useRef(true);
  useEffect(() => {
    alive.current = true;
    const controller = new AbortController();
    void Promise.all([
      orderApi.order(orderId, controller.signal),
      refundApi.forOrder(orderId, controller.signal),
    ])
      .then(([order, refunds]) => {
        if (!controller.signal.aborted) {
          setResult({ order, refunds });
          setError("");
        }
      })
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(getApiErrorMessage(e));
      });
    const clock = window.setInterval(() => setNow(Date.now()), 1000);
    return () => {
      alive.current = false;
      controller.abort();
      window.clearInterval(clock);
    };
  }, [orderId, retry]);
  async function mutate(action: () => Promise<Refund>) {
    if (
      operation.current ||
      !result ||
      !refundEligibility(result.order, result.refunds, Date.now()).eligible
    )
      return;
    operation.current = true;
    setBusy(true);
    setSubmitError("");
    try {
      // Recheck authoritative state before any upload or request after a long editing session.
      const [order, refunds] = await Promise.all([
        orderApi.order(orderId),
        refundApi.forOrder(orderId),
      ]);
      if (!alive.current) return;
      if (!refundEligibility(order, refunds, Date.now()).eligible) {
        setResult({ order, refunds });
        return;
      }
      const refund = await action();
      if (!alive.current) return;
      toast.success("Refund request submitted.");
      navigate(APP_PATHS.REFUND_DETAIL.replace(":refundId", encodeURIComponent(refund.id)), {
        replace: true,
      });
    } catch (e) {
      if (alive.current) setSubmitError(getApiErrorMessage(e));
    } finally {
      operation.current = false;
      if (alive.current) setBusy(false);
    }
  }
  const eligibility = result && refundEligibility(result.order, result.refunds, now);
  return (
    <div className="min-w-0 w-full space-y-6 py-6 sm:py-8">
      <PageHeader
        title="Request a refund"
        description="Select the affected items and provide details for the shop to review."
        breadcrumb={
          <ol className="flex flex-wrap items-center gap-2">
            <li>
              <Link
                to={APP_PATHS.ACCOUNT_ORDERS}
                className="inline-flex min-h-8 items-center hover:text-primary-700"
              >
                My orders
              </Link>
            </li>
            <li className="flex items-center gap-2">
              <span aria-hidden="true">›</span>
              <Link
                to={detailUrl}
                className="inline-flex min-h-8 items-center hover:text-primary-700"
              >
                Order details
              </Link>
            </li>
            <li className="flex items-center gap-2">
              <span aria-hidden="true">›</span>
              <span aria-current="page" className="font-medium text-text-primary">
                Request a refund
              </span>
            </li>
          </ol>
        }
      />
      {error ? (
        <ErrorState
          title="Couldn't check refund eligibility"
          description={error}
          onRetry={() => {
            setError("");
            setRetry((n) => n + 1);
          }}
        />
      ) : !result ? (
        <div role="status" aria-label="Checking refund eligibility" className="space-y-4">
          <Skeleton className="h-20" />
          <Skeleton className="h-96" />
        </div>
      ) : eligibility && !eligibility.eligible && !busy ? (
        <EmptyState
          title="Refund request unavailable"
          description={eligibility.reason}
          action={
            <ButtonLink to={detailUrl} variant="secondary">
              Back to order details
            </ButtonLink>
          }
        />
      ) : (
        <>
          <Card className="flex flex-wrap items-center justify-between gap-3 !py-4">
            <div className="min-w-0 [overflow-wrap:anywhere]">
              <p className="font-semibold text-text-primary">{result.order.shopName}</p>
              <p className="mt-1 text-caption text-text-secondary">
                Order {result.order.orderCode}
              </p>
            </div>
            {eligibility?.deadline && (
              <p className="text-caption text-text-secondary">
                Request by {checkoutTime(new Date(eligibility.deadline).toISOString())} · GMT+7
              </p>
            )}
          </Card>
          <Card className="min-w-0">
            {submitError && (
              <p
                role="alert"
                className="mb-4 rounded-md bg-danger-soft p-3 text-body text-danger-strong"
              >
                {submitError}
              </p>
            )}
            <RefundRequestForm
              order={result.order}
              busy={busy}
              mutate={mutate}
              onClose={() => navigate(detailUrl)}
            />
          </Card>
        </>
      )}
    </div>
  );
}
