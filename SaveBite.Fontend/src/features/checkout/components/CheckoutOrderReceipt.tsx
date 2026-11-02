import {
  Badge,
  Button,
  Card,
  ErrorState,
  MediaImage,
  PageHeader,
  Skeleton,
} from "@/shared/components/ui";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import { CustomerOrderRefunds } from "@/features/refunds/components/CustomerOrderRefunds";
import { OrderPickupDeadlines } from "./OrderPickupDeadlines";
import { OrderPickupCode } from "./OrderPickupCode";
import { OrderStatusPanel } from "./OrderStatusPanel";
import { CancelOrderAction } from "./CancelOrderAction";
import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import {
  CheckCircle2,
  CreditCard,
  Store,
  Package,
  MapPin,
  ShoppingBag,
  Star,
} from "lucide-react";
import { RateOrderModal } from "@/features/feedbacks/components/RateOrderModal";
import { EditReviewModal } from "@/features/feedbacks/components/EditReviewModal";
import { APP_PATHS } from "@/app/router/paths";
import { orderApi } from "@/features/orders/api/orderApi";
import { paymentApi } from "@/features/payments/api/paymentApi";
import type { CheckoutOrder } from "../types/checkout.types";
import { checkoutTime, safePaymentUrl } from "../utils/checkoutPolicy";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { getApiErrorMessage } from "@/shared/api";
import { useCartStore } from "@/features/cart/stores/cartStore";

export function CheckoutOrderReceipt({ orderId }: { orderId: string }) {
  const realtime = useRealtimeSubscriptions();
  const [order, setOrder] = useState<CheckoutOrder | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [ratingItemId, setRatingItemId] = useState<string | null>(null);
  const [editingFeedbackTarget, setEditingFeedbackTarget] = useState<{
    feedbackId: string;
    productName?: string;
    variantName?: string;
    imageUrl?: string | null;
  } | null>(null);
  const [now, setNow] = useState(Date.now);
  const alive = useRef(true);
  const revision = useRef(0);
  const operation = useRef(false);
  const pending = order?.paymentStatus === "Pending" && order.status === "AwaitingPayment";
  const expired = !!order?.paymentExpiresAt && Date.parse(order.paymentExpiresAt) <= now;

  useEffect(() => {
    alive.current = true;
    const clock = window.setInterval(() => setNow(Date.now()), 1000);
    return () => {
      alive.current = false;
      window.clearInterval(clock);
    };
  }, []);
  useEffect(() => {
    if (busy) return;
    const controller = new AbortController();
    let reading = false;
    let dirty = false;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        while (dirty && !controller.signal.aborted) {
          dirty = false;
          const current = ++revision.current;
          const next = await orderApi.order(orderId, controller.signal);
          if (!controller.signal.aborted && alive.current && current === revision.current)
            setOrder(next);
        }
      } catch (error) {
        if (!controller.signal.aborted && alive.current) setError(getApiErrorMessage(error));
      } finally {
        reading = false;
      }
    }
    const off = subscribeCustomerOrderEvents(
      realtime,
      () => {
        void refresh();
      },
      orderId,
    );
    void refresh();
    return () => {
      off();
      controller.abort();
    };
  }, [orderId, realtime, busy]);
  useEffect(() => {
    if (!pending) return;
    const controller = new AbortController();
    let reading = false;
    const timer = window.setInterval(async () => {
      if (operation.current || reading || document.visibilityState !== "visible") return;
      reading = true;
      const current = ++revision.current;
      try {
        const next = await orderApi.order(orderId, controller.signal);
        if (alive.current && current === revision.current) {
          setOrder(next);
          setError(null);
        }
      } catch (error) {
        if (!controller.signal.aborted && alive.current && current === revision.current)
          setError(getApiErrorMessage(error));
      } finally {
        reading = false;
      }
    }, 5000);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [pending, orderId]);
  async function update(action: () => Promise<CheckoutOrder>, redirect = false) {
    if (operation.current) return;
    operation.current = true;
    setBusy(true);
    setError(null);
    const current = ++revision.current;
    try {
      const next = await action();
      if (!alive.current || current !== revision.current) return;
      setOrder(next);
      void useCartStore.getState().refresh();
      const url = safePaymentUrl(next.checkoutUrl);
      if (redirect && next.paymentStatus === "Pending" && url) window.location.assign(url);
    } catch (error) {
      if (alive.current && current === revision.current) setError(getApiErrorMessage(error));
    } finally {
      operation.current = false;
      if (alive.current) setBusy(false);
    }
  }
  return (
    <div className="min-w-0 py-6 sm:py-8">
      <RateOrderModal
        isOpen={ratingItemId !== null}
        onClose={() => setRatingItemId(null)}
        order={order}
        focusOrderItemId={ratingItemId || undefined}
        onSubmitted={() => void update(() => orderApi.order(orderId))}
      />
      <EditReviewModal
        isOpen={Boolean(editingFeedbackTarget)}
        onClose={() => setEditingFeedbackTarget(null)}
        feedbackId={editingFeedbackTarget?.feedbackId ?? null}
        initialProductName={editingFeedbackTarget?.productName}
        initialVariantName={editingFeedbackTarget?.variantName}
        initialImageUrl={editingFeedbackTarget?.imageUrl}
        onUpdated={() => void update(() => orderApi.order(orderId))}
      />
      <div className="mb-6">
        <PageHeader
          title="Order details"
          description="Your order, payment and pickup information in one place."
          breadcrumb={
            <Link
              to={APP_PATHS.ACCOUNT_ORDERS}
              className="inline-flex min-h-8 items-center gap-2 font-medium text-text-secondary hover:text-primary-700"
            >
              My orders
            </Link>
          }
          actions={
            order?.createdAt ? (
              <Badge tone="info">Placed {checkoutTime(order.createdAt)} · GMT+7</Badge>
            ) : undefined
          }
        />
      </div>
      {error && (
        <div className="mb-5">
          <ErrorState
            title="Couldn't verify your order"
            description={error}
            onRetry={() => void update(() => orderApi.order(orderId))}
          />
        </div>
      )}
      {!order && !error && (
        <div role="status" aria-label="Verifying your order status" className="space-y-4">
          <span className="sr-only">Verifying your order status…</span>
          <Skeleton className="h-24" />
          <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_22rem]">
            <Skeleton className="h-96" />
            <Skeleton className="h-72" />
          </div>
        </div>
      )}
      {order && (
        <div className="space-y-4">
          <Card
            aria-label="Order overview"
            className="flex flex-wrap items-center justify-between gap-x-6 gap-y-3 rounded-md border border-border-default bg-bg-surface px-4 py-3"
          >
            <OrderPickupCode code={order.orderCode} compact />
            <OrderStatusPanel order={order} compact />
          </Card>
          {order.paymentReviewRequired && (
            <p
              role="alert"
              className="rounded-md border border-accent-100 bg-accent-50 px-4 py-3 text-body text-accent-700"
            >
              Your payment needs review. Contact the shop to check the payment; your pickup deadline
              has not been extended.
            </p>
          )}
          {order.paymentMessage && (
            <p
              role="alert"
              className="rounded-md border border-accent-100 bg-accent-50 p-4 text-body text-accent-700"
            >
              {order.paymentMessage}
            </p>
          )}
          <div className="grid min-w-0 items-start gap-4 lg:grid-cols-[minmax(0,1fr)_22rem]">
            <div className="min-w-0 space-y-4">
              <Card aria-label="Shop and ordered products" className="!p-0 overflow-hidden">
                <header className="flex items-center gap-3 border-b border-border-default bg-primary-50/50 p-4 sm:p-5">
                  <OrderImage src={order.shopLogoUrl} alt={order.shopName} shop />
                  <div className="min-w-0">
                    <p className="text-caption font-medium text-text-secondary">Pickup shop</p>
                    <h2 className="mt-0.5 break-words text-section font-bold text-text-primary">
                      {order.shopName}
                    </h2>
                    {order.shopAddress && (
                      <p className="mt-1 flex items-start gap-1.5 text-caption text-text-secondary">
                        <MapPin aria-hidden="true" size={15} className="mt-0.5 shrink-0" />
                        <span className="break-words">{order.shopAddress}</span>
                      </p>
                    )}
                  </div>
                </header>
                <div className="px-4 sm:px-5">
                  <div className="flex items-center justify-between gap-3 pt-4">
                    <h3 className="flex items-center gap-2 text-body font-semibold text-text-primary">
                      <ShoppingBag aria-hidden="true" size={17} className="text-primary-600" />
                      Ordered products
                    </h3>
                    <span className="text-caption text-text-secondary">
                      {order.items.reduce((sum, item) => sum + item.quantity, 0)} items
                    </span>
                  </div>
                  <ul className="divide-y divide-border-default" aria-label="Ordered products">
                    {order.items.map((item) => (
                      <li
                        key={item.flashDealVariantId}
                        className="grid grid-cols-[64px_minmax(0,1fr)] gap-x-3 gap-y-2 py-4 sm:grid-cols-[64px_minmax(0,1fr)_auto] sm:gap-x-4"
                      >
                        <div className="row-span-2 sm:row-span-1">
                          <OrderImage src={item.imageUrl} alt={item.productName} />
                        </div>
                        <div className="min-w-0">
                          <p className="break-words text-body font-semibold text-text-primary">
                            {item.productName}
                          </p>
                          {item.variantName && (
                            <p className="mt-1 text-caption text-text-secondary">
                              {item.variantName}
                            </p>
                          )}
                          <p className="mt-1.5 text-caption text-text-secondary">
                            Qty{" "}
                            <span className="font-semibold text-text-primary">{item.quantity}</span>{" "}
                            × {money(item.unitPrice)}
                          </p>
                          {order.status === "Completed" && (
                            <div className="mt-2">
                              {item.canReview && item.orderItemId ? (
                                <Button
                                  variant="secondary"
                                  type="button"
                                  onClick={() => setRatingItemId(item.orderItemId ?? "")}
                                  className="inline-flex items-center gap-1.5 rounded-md border border-accent-300 bg-accent-50 px-2.5 py-1 text-caption font-semibold text-accent-700 hover:bg-accent-100"
                                >
                                  <Star
                                    aria-hidden="true"
                                    size={13}
                                    className="fill-amber-400 text-accent-500"
                                  />
                                  Rate this item
                                </Button>
                              ) : item.rating ? (
                                <Button
                                  variant="secondary"
                                  type="button"
                                  onClick={() => {
                                    if (item.feedbackId) {
                                      setEditingFeedbackTarget({
                                        feedbackId: item.feedbackId,
                                        productName: item.productName,
                                        variantName: item.variantName,
                                        imageUrl: item.imageUrl,
                                      });
                                    }
                                  }}
                                  className="inline-flex items-center gap-1 rounded-full bg-primary-50 px-2.5 py-0.5 text-caption font-semibold text-primary-700 hover:bg-primary-100 transition-colors cursor-pointer text-left"
                                >
                                  <CheckCircle2 aria-hidden="true" size={13} />
                                  Reviewed
                                  <Star
                                    aria-hidden="true"
                                    size={12}
                                    className="fill-amber-400 text-accent-300"
                                  />
                                  {item.rating}/5 · Edit review
                                </Button>
                              ) : null}
                            </div>
                          )}
                        </div>
                        <p className="col-start-2 text-body font-bold tabular-nums text-text-primary sm:col-start-auto sm:text-right">
                          {money(item.subtotal)}
                        </p>
                      </li>
                    ))}
                  </ul>
                </div>
                <div className="flex items-center justify-between gap-4 border-t border-border-default bg-neutral-50/70 px-4 py-3 sm:px-5">
                  <p className="text-body font-semibold text-text-secondary">Order total</p>
                  <p className="text-xl font-bold tabular-nums text-primary-700">
                    {money(order.totalAmount)}
                  </p>
                </div>
              </Card>
              <Card aria-label="Pickup information" className="space-y-3">
                <h2 className="mb-3 text-body font-semibold text-text-primary">
                  Pickup information
                </h2>
                <OrderPickupDeadlines order={order} now={now} compact />
                <p className="mt-3 text-caption leading-relaxed text-text-secondary">
                  GMT+7 · Pickup deadlines do not extend during payment.
                </p>
              </Card>
            </div>
            <aside
              aria-label="Payment summary"
              className="min-w-0 space-y-3 rounded-md border border-border-default bg-bg-surface p-4 sm:p-5 lg:sticky lg:top-24"
            >
              <h2 className="flex items-center gap-2 text-section font-bold text-text-primary">
                <CreditCard aria-hidden="true" size={20} className="text-primary-600" />
                Payment summary
              </h2>
              <dl className="space-y-3 text-caption">
                <div className="flex justify-between gap-4">
                  <dt className="text-text-secondary">Method</dt>
                  <dd className="min-w-0 max-w-[65%] break-words text-right font-semibold text-text-primary">
                    {order.paymentMethod === "PayAtShop"
                      ? "Online deposit + pay at shop"
                      : "Full online payment"}
                  </dd>
                </div>
                <div className="flex justify-between gap-4">
                  <dt className="text-text-secondary">
                    {order.paymentStatus === "Succeeded"
                      ? "Paid online"
                      : "Required online payment"}
                  </dt>
                  <dd className="shrink-0 font-bold tabular-nums text-text-primary">
                    {money(order.requiredOnlineAmount)}
                  </dd>
                </div>
                <div className="flex justify-between gap-4 border-t border-border-default pt-4">
                  <dt className="font-semibold text-text-secondary">Remaining at shop</dt>
                  <dd className="shrink-0 text-lg font-extrabold tabular-nums text-primary-700">
                    {money(order.remainingAtShop)}
                  </dd>
                </div>
              </dl>
              {pending && (
                <div className="space-y-3 border-t border-border-default pt-4">
                  <p className="text-caption text-text-secondary">
                    Payment deadline: {checkoutTime(order.paymentExpiresAt)}
                  </p>
                  <Button
                    loading={busy}
                    size="lg"
                    type="button"
                    disabled={busy || expired}
                    onClick={() => void update(() => paymentApi.payment(orderId), true)}
                    className="w-full whitespace-normal !px-3 py-3"
                  >
                    <CreditCard aria-hidden="true" size={18} className="shrink-0" />
                    {busy
                      ? "Checking…"
                      : expired
                        ? "Payment expired · Verifying"
                        : `${order.paymentMethod === "PayAtShop" ? "Pay deposit" : "Pay online"} ${money(order.requiredOnlineAmount)}`}
                  </Button>
                </div>
              )}
              {order.policy && (
                <div className="rounded-md bg-neutral-50 p-3">
                  <p className="text-caption font-medium text-text-secondary">
                    Your policy at checkout
                  </p>
                  <p className="mt-1 text-body font-semibold text-text-primary">
                    {order.policy.levelRank ? `Level ${order.policy.levelRank} · ` : ""}
                    {order.policy.levelName}
                  </p>
                  <p className="mt-2 text-caption leading-relaxed text-text-secondary">
                    {order.paymentMethod === "OnlinePayment"
                      ? "Full order amount is required online."
                      : order.policy.appliedRange
                        ? `Over ${money(order.policy.appliedRange.fromOrderAmount)} up to ${money(order.policy.appliedRange.toOrderAmount)} · Fixed deposit ${money(order.depositAmount)}.`
                        : `No deposit required for orders up to ${money(order.policy.freeOrderAmount ?? 0)}.`}
                  </p>
                  <p className="mt-2 text-caption leading-relaxed text-text-secondary">
                    Later level or policy changes do not affect the deposit recorded on this order.
                  </p>
                </div>
              )}
            </aside>
          </div>
          <div className="space-y-4">
            {["Completed", "Rejected", "Expired"].includes(order.status) && (
              <CustomerOrderRefunds key={`${order.id}:${order.status}`} order={order} />
            )}
            <CancelOrderAction
              order={order}
              now={now}
              busy={busy}
              onCancel={() => update(() => orderApi.cancel(orderId))}
            />
          </div>
        </div>
      )}
    </div>
  );
}

function OrderImage({ src, alt, shop = false }: { src?: string; alt: string; shop?: boolean }) {
  return (
    <MediaImage
      src={src}
      alt={alt}
      fallbackIcon={shop ? Store : Package}
      showFallbackLabel={false}
      className={`shrink-0 rounded-md ${shop ? "size-12" : "size-16"}`}
    />
  );
}
