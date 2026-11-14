import { PaymentPanel } from "@/features/payments/components/PaymentPanel";
import { usePaymentUpdates } from "@/features/payments/hooks/usePaymentUpdates";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { paymentTypeLabel, paymentMethodLabel } from "@/features/payments/utils/paymentLabels";
import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { CreditCard, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { paymentApi } from "@/features/payments/api/paymentApi";
import type { CustomerPaymentDetail } from "@/features/payments/types/paymentHistory.types";
import { PaymentStatusBadge } from "@/features/payments/components/PaymentStatusBadge";
import { OrderPickupDeadlines } from "@/features/checkout/components/OrderPickupDeadlines";
import { OrderPickupCode } from "@/features/checkout/components/OrderPickupCode";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { CancelOrderAction } from "@/features/checkout/components/CancelOrderAction";
import { orderApi } from "@/features/orders/api/orderApi";
import { useCartStore } from "@/features/cart/stores/cartStore";
import { AmountSummary, ButtonLink, Card, ErrorState, MediaImage } from "@/shared/components/ui";
import { OrderStatusPanel } from "@/features/checkout/components/OrderStatusPanel";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

export function PaymentDetailPage() {
  const { paymentId = "" } = useParams();
  const [params] = useSearchParams();
  const checkoutId = params.get("checkoutId");
  const userId = useAuthStore((state) => state.user?.id);
  return (
    <PaymentDetails key={`${userId}:${paymentId}`} paymentId={paymentId} checkoutId={checkoutId} />
  );
}
function PaymentDetails({
  paymentId,
  checkoutId,
}: {
  paymentId: string;
  checkoutId: string | null;
}) {
  const navigate = useNavigate();
  const wasPending = useRef(false);
  const [data, setData] = useState<CustomerPaymentDetail | null>(null);
  const [error, setError] = useState("");
  const [retry, setRetry] = useState(0);
  const [now, setNow] = useState(Date.now);
  const [cancelling, setCancelling] = useState(false);
  const mounted = useRef(true);
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);
  usePaymentUpdates(
    REALTIME_EVENTS.PAYMENT_CHANGED,
    async (signal) => {
      try {
        const result = await paymentApi.detail(paymentId, signal);
        if (mounted.current && !signal.aborted) {
          setData(result);
          setError("");
        }
      } catch (cause) {
        if (mounted.current && !signal.aborted) setError(getApiErrorMessage(cause));
      }
    },
    (event) => event.paymentId === paymentId,
  );
  useEffect(() => {
    if (!retry) return;
    const abort = new AbortController();
    void paymentApi
      .detail(paymentId, abort.signal)
      .then((result) => {
        if (!abort.signal.aborted) {
          setData(result);
          setError("");
        }
      })
      .catch((cause) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(cause));
      });
    return () => abort.abort();
  }, [retry, paymentId]);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  const pending = data?.payment.status === "Pending";
  const payment = data?.payment,
    order = data?.order;
  const paymentDeadline =
    payment?.type === "Remaining" ? order?.hardDeadlineAt : order?.paymentExpiresAt;
  const processing =
    pending &&
    payment?.method === "OnlinePayment" &&
    !!paymentDeadline &&
    Date.parse(paymentDeadline) <= now;
  const hasOrder =
    Boolean(payment?.orderId) &&
    !order?.isCheckoutSession &&
    !(["Cancelled", "Expired"].includes(payment?.status ?? "") && payment?.collectedAmount === 0);
  useEffect(() => {
    if (payment?.status === "Pending") wasPending.current = true;
    // Historical successful payments remain readable; only a newly completed payment redirects.
    if (
      (checkoutId || wasPending.current) &&
      !cancelling &&
      payment?.status === "Succeeded" &&
      payment.orderId &&
      hasOrder &&
      !order?.paymentReviewRequired
    ) {
      navigate(APP_PATHS.ORDER_DETAIL.replace(":orderId", encodeURIComponent(payment.orderId)), {
        replace: true,
      });
    }
  }, [
    checkoutId,
    cancelling,
    payment?.status,
    payment?.orderId,
    hasOrder,
    order?.paymentReviewRequired,
    navigate,
  ]);
  async function cancelPayment() {
    if (!order || cancelling) return;
    setCancelling(true);
    wasPending.current = false;
    try {
      await orderApi.cancel(order.id);
      setData(await paymentApi.detail(paymentId));
      setError("");
      void useCartStore.getState().refresh();
    } catch (cause) {
      setError(getApiErrorMessage(cause));
    } finally {
      setCancelling(false);
    }
  }
  return (
    <div className="w-full min-w-0 space-y-5">
      {error && (
        <ErrorState
          title="Couldn't load payment details"
          description={error}
          onRetry={() => setRetry((value) => value + 1)}
        />
      )}
      {!data && !error && <LoadingSkeleton variant="detail" label="Loading payment details" />}
      {payment && order && (
        <>
          <Card padding="none" aria-label="Payment details" className="overflow-hidden">
            {!processing && (
              <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border-default bg-primary-50/50 p-4 sm:p-5">
                <h2 className="flex items-center gap-2 text-section font-semibold text-text-primary">
                  <CreditCard size={20} aria-hidden="true" className="text-primary-700" />
                  {paymentTypeLabel(payment.type)}
                </h2>
                <PaymentStatusBadge status={payment.status} />
              </header>
            )}
            {pending && payment.method === "OnlinePayment" && (
              <PaymentPanel
                amount={payment.amount}
                qrCode={payment.qrCode}
                reference={payment.gatewayOrderCode}
                expiresAt={
                  payment.type === "Remaining" ? order.hardDeadlineAt : order.paymentExpiresAt
                }
                isSimulated={payment.isSimulated}
              >
                {payment.isSimulated && (
                  <button
                    type="button"
                    className="min-h-11 rounded-lg bg-amber-600 px-4 py-2 text-sm font-semibold text-white"
                    onClick={() => {
                      void paymentApi
                        .simulate(order.id, "paid", payment.id)
                        .then(() => paymentApi.detail(paymentId))
                        .then(setData)
                        .catch((cause) => setError(getApiErrorMessage(cause)));
                    }}
                  >
                    Confirm test payment
                  </button>
                )}
              </PaymentPanel>
            )}
            {!processing && (
              <div className="space-y-3 p-4 sm:p-5">
                {pending && order.isCheckoutSession && (
                  <div className="flex flex-wrap items-center gap-3">
                    <CancelOrderAction
                      order={order}
                      now={now}
                      busy={cancelling}
                      onCancel={cancelPayment}
                    />
                  </div>
                )}
                <AmountSummary
                  amounts={[
                    { label: "Payment amount", amount: payment.amount },
                    { label: "Amount collected", amount: payment.collectedAmount, tone: "primary" },
                  ]}
                />
                <dl className="grid gap-4 text-body sm:grid-cols-3">
                  {[
                    ["Method", paymentMethodLabel(payment.method)],
                    ["Created", checkoutTime(payment.createdAt)],
                    ["Paid at", payment.paidAt ? checkoutTime(payment.paidAt) : "Not recorded"],
                  ].map(([label, value]) => (
                    <div key={label}>
                      <dt className="text-caption text-text-secondary">{label}</dt>
                      <dd className="mt-1 font-medium text-text-primary [overflow-wrap:anywhere]">
                        {value}
                      </dd>
                    </div>
                  ))}
                </dl>
                <p className="break-all border-t border-border-default pt-3 text-caption text-text-muted">
                  Payment ID: {payment.id}
                </p>
              </div>
            )}
          </Card>
          <Card padding="none" aria-label="Related order" className="overflow-hidden">
            <header className="flex items-center gap-3 border-b border-border-default bg-primary-50/40 p-4 sm:p-5">
              <MediaImage
                src={order.shopLogoUrl}
                alt=""
                fallbackIcon={Store}
                showFallbackLabel={false}
                className="size-10 shrink-0 rounded-md"
              />
              <div className="min-w-0">
                <p className="text-caption text-text-secondary">
                  {hasOrder ? "Related order · Pickup shop" : "Checkout payment · No order placed"}
                </p>
                <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {order.shopName}
                </h2>
                {order.shopAddress && (
                  <p className="mt-1 text-caption text-text-secondary [overflow-wrap:anywhere]">
                    {order.shopAddress}
                  </p>
                )}
              </div>
            </header>
            <div className="space-y-3 p-4 sm:p-5">
              <div className="flex flex-wrap items-start justify-between gap-3">
                {hasOrder && <OrderPickupCode code={order.orderCode} compact showHint={false} />}
                <ButtonLink
                  to={
                    hasOrder
                      ? APP_PATHS.ORDER_DETAIL.replace(":orderId", encodeURIComponent(order.id))
                      : APP_PATHS.CART
                  }
                  variant="primary"
                  className="min-h-11"
                >
                  {hasOrder ? "View order details" : "Back to cart"}
                </ButtonLink>
              </div>
              {hasOrder && <OrderStatusPanel order={order} compact />}
              <ul aria-label="Ordered products" className="divide-y divide-border-default">
                {order.items.map((item) => (
                  <li
                    key={item.flashDealVariantId}
                    className="grid grid-cols-[48px_minmax(0,1fr)] gap-3 py-4 sm:grid-cols-[48px_minmax(0,1fr)_auto]"
                  >
                    <MediaImage
                      src={item.imageUrl}
                      alt={item.productName}
                      showFallbackLabel={false}
                      className="size-12 rounded-md"
                    />
                    <div className="min-w-0">
                      <p className="text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
                        {item.productName}
                      </p>
                      {item.variantName && (
                        <p className="mt-1 text-caption text-text-secondary [overflow-wrap:anywhere]">
                          {item.variantName}
                        </p>
                      )}
                      <p className="mt-1 text-caption text-text-secondary">
                        Qty {item.quantity} × {money(item.unitPrice)}
                      </p>
                    </div>
                    <p className="col-start-2 text-body font-semibold tabular-nums text-text-primary sm:col-start-auto">
                      {money(item.subtotal)}
                    </p>
                  </li>
                ))}
              </ul>
              <AmountSummary
                amounts={[
                  { label: hasOrder ? "Order total" : "Checkout total", amount: order.totalAmount },
                  {
                    label: hasOrder ? "Paid for order" : "Amount paid",
                    amount: order.paidAmount ?? 0,
                    tone: "primary",
                  },
                  {
                    label: "Still due",
                    amount: order.outstandingAmount ?? 0,
                    tone: (order.outstandingAmount ?? 0) > 0 ? "warning" : undefined,
                  },
                ]}
              />
              {hasOrder && <OrderPickupDeadlines order={order} now={now} compact />}
            </div>
          </Card>
        </>
      )}
    </div>
  );
}
