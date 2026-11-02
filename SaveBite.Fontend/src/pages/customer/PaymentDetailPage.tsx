import { paymentTypeLabel, paymentMethodLabel } from "@/features/payments/utils/paymentLabels";
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { CreditCard, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { paymentApi } from "@/features/payments/api/paymentApi";
import type { CustomerPaymentDetail } from "@/features/payments/types/paymentHistory.types";
import { PaymentStatusBadge } from "@/features/payments/components/PaymentStatusBadge";
import { OrderPickupDeadlines } from "@/features/checkout/components/OrderPickupDeadlines";
import { OrderPickupCode } from "@/features/checkout/components/OrderPickupCode";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import {
  AmountSummary,
  ButtonLink,
  Card,
  ErrorState,
  MediaImage,
  Skeleton,
} from "@/shared/components/ui";
import { OrderStatusPanel } from "@/features/checkout/components/OrderStatusPanel";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

export function PaymentDetailPage() {
  const { paymentId = "" } = useParams();
  const userId = useAuthStore((state) => state.user?.id);
  return <PaymentDetails key={`${userId}:${paymentId}`} paymentId={paymentId} />;
}
function PaymentDetails({ paymentId }: { paymentId: string }) {
  const [data, setData] = useState<CustomerPaymentDetail | null>(null);
  const [error, setError] = useState("");
  const [retry, setRetry] = useState(0);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    const abort = new AbortController();
    void paymentApi
      .detail(paymentId, abort.signal)
      .then((result) => {
        if (!abort.signal.aborted) {
          setData(result);
          setError("");
        }
      })
      .catch((e) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(e));
      });
    return () => abort.abort();
  }, [paymentId, retry]);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  const payment = data?.payment,
    order = data?.order;
  return (
    <div className="min-w-0 space-y-4">
      {error && (
        <ErrorState
          title="Couldn't load payment details"
          description={error}
          onRetry={() => setRetry((value) => value + 1)}
        />
      )}
      {!data && !error && (
        <div role="status" aria-label="Loading payment details" className="space-y-4">
          <Skeleton className="h-48" />
          <Skeleton className="h-80" />
        </div>
      )}
      {payment && order && (
        <>
          <Card padding="none" aria-label="Payment details" className="overflow-hidden">
            <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border-default bg-primary-50/50 p-4 sm:p-5">
              <h2 className="flex items-center gap-2 text-section font-semibold text-text-primary">
                <CreditCard size={20} aria-hidden="true" className="text-primary-700" />
                {paymentTypeLabel(payment.type)}
              </h2>
              <PaymentStatusBadge status={payment.status} />
            </header>
            <div className="space-y-3 p-4 sm:p-5">
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
                Payment ID: {payment.id} · GMT+7
              </p>
            </div>
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
                <p className="text-caption text-text-secondary">Related order · Pickup shop</p>
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
                <OrderPickupCode code={order.orderCode} compact showHint={false} />
                <ButtonLink
                  to={APP_PATHS.ORDER_DETAIL.replace(":orderId", encodeURIComponent(order.id))}
                  variant="secondary"
                  className="min-h-11"
                >
                  View order details
                </ButtonLink>
              </div>
              <OrderStatusPanel order={order} compact />
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
                  { label: "Order total", amount: order.totalAmount },
                  { label: "Paid for order", amount: order.paidAmount ?? 0, tone: "primary" },
                  {
                    label: "Still due",
                    amount: order.outstandingAmount ?? 0,
                    tone: (order.outstandingAmount ?? 0) > 0 ? "warning" : undefined,
                  },
                ]}
              />
              <OrderPickupDeadlines order={order} now={now} compact />
            </div>
          </Card>
        </>
      )}
    </div>
  );
}
