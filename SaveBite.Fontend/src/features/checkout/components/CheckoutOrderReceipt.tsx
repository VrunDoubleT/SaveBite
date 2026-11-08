import { CustomerActionButton } from "@/shared/components/CustomerMutationControls";
import { Card, ErrorState, MediaImage, Skeleton } from "@/shared/components/ui";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import { CustomerOrderRefunds } from "@/features/refunds/components/CustomerOrderRefunds";
import { OrderPickupDeadlines } from "./OrderPickupDeadlines";
import { OrderPickupCode } from "./OrderPickupCode";
import { OrderStatusPanel } from "./OrderStatusPanel";
import { CancelOrderAction } from "./CancelOrderAction";
import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { CreditCard, Clock3, Store, MapPin, ShoppingBag } from "lucide-react";
import { OrderItems, type OrderReviewTarget } from "@/features/orders/components/OrderItems";
import { OrderPaymentAmounts } from "@/features/orders/components/OrderPaymentAmounts";
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
import { getUploadDraft } from "@/shared/uploads/uploadDraftStore";
import { useAuthStore } from "@/shared/stores/authStore";

export function CheckoutOrderReceipt({ orderId }: { orderId: string }) {
  const realtime = useRealtimeSubscriptions();
  const location = useLocation();
  const navigate = useNavigate();
  const userId = useAuthStore((state) => state.user?.id);
  const [order, setOrder] = useState<CheckoutOrder | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [ratingItemId, setRatingItemId] = useState<string | null>(null);
  const [ratingUploadDraftId, setRatingUploadDraftId] = useState<string>();
  const [editingFeedbackTarget, setEditingFeedbackTarget] = useState<OrderReviewTarget | null>(
    null,
  );
  const [editingUploadDraftId, setEditingUploadDraftId] = useState<string>();
  const [now, setNow] = useState(Date.now);
  const alive = useRef(true);
  const revision = useRef(0);
  const operation = useRef(false);
  const pending = order?.paymentStatus === "Pending" && order.status === "AwaitingPayment";
  const expired = !!order?.paymentExpiresAt && Date.parse(order.paymentExpiresAt) <= now;
  const resumeUploadDraftId = (location.state as { uploadDraftId?: string } | null)?.uploadDraftId;

  useEffect(() => {
    if (!resumeUploadDraftId) return;
    let active = true;
    void getUploadDraft(resumeUploadDraftId)
      .then((draft) => {
        if (!active) return;
        const form = draft?.formData as
          | { kind?: string; orderItemId?: string; feedbackId?: string }
          | undefined;
        if (!draft || draft.userId !== userId || draft.purpose !== "feedback" || !form) {
          setError("This saved review is unavailable or has expired.");
          return;
        }
        if (form.kind === "create-review" && form.orderItemId) {
          setRatingItemId(form.orderItemId);
          setRatingUploadDraftId(draft.id);
        } else if (form.kind === "edit-review" && form.feedbackId) {
          setEditingFeedbackTarget({ feedbackId: form.feedbackId });
          setEditingUploadDraftId(draft.id);
        } else {
          setError("This saved review is unavailable or has expired.");
        }
      })
      .catch((error) => setError(getApiErrorMessage(error)))
      .finally(() => {
        if (active) navigate(`${location.pathname}${location.search}`, { replace: true, state: null });
      });
    return () => {
      active = false;
    };
  }, [resumeUploadDraftId, userId, navigate, location.pathname, location.search]);

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
          if (!controller.signal.aborted && alive.current && current === revision.current) {
            setOrder(next);
            setError(null);
          }
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
        onClose={() => {
          setRatingItemId(null);
          setRatingUploadDraftId(undefined);
        }}
        order={order}
        focusOrderItemId={ratingItemId || undefined}
        uploadDraftId={ratingUploadDraftId}
        onSubmitted={() => void update(() => orderApi.order(orderId))}
      />
      <EditReviewModal
        isOpen={Boolean(editingFeedbackTarget)}
        onClose={() => {
          setEditingFeedbackTarget(null);
          setEditingUploadDraftId(undefined);
        }}
        feedbackId={editingFeedbackTarget?.feedbackId ?? null}
        initialProductName={editingFeedbackTarget?.productName}
        initialVariantName={editingFeedbackTarget?.variantName}
        initialImageUrl={editingFeedbackTarget?.imageUrl}
        orderId={orderId}
        uploadDraftId={editingUploadDraftId}
        onUpdated={() => void update(() => orderApi.order(orderId))}
      />
      <div className="mb-6 space-y-3">
        <nav aria-label="Breadcrumb" className="text-caption text-text-secondary">
          <ol className="flex flex-wrap items-center gap-2">
            <li>
              <Link
                to={APP_PATHS.HOME}
                className="inline-flex min-h-8 items-center font-medium hover:text-primary-700"
              >
                Home
              </Link>
            </li>
            <li className="flex items-center gap-2">
              <span aria-hidden="true">›</span>
              <Link
                to={APP_PATHS.ACCOUNT_ORDERS}
                className="inline-flex min-h-8 items-center font-medium hover:text-primary-700"
              >
                My orders
              </Link>
            </li>
            <li className="flex items-center gap-2">
              <span aria-hidden="true">›</span>
              <span aria-current="page" className="font-medium text-text-primary">
                Order details
              </span>
            </li>
          </ol>
        </nav>
        <h1 className="text-page font-semibold text-text-primary">Order details</h1>
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
          <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_24rem]">
            <Skeleton className="h-96" />
            <Skeleton className="h-72" />
          </div>
        </div>
      )}
      {order && (
        <div className="space-y-4">
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
          <div className="grid min-w-0 items-start gap-4 lg:grid-cols-[minmax(0,1fr)_24rem]">
            <div className="min-w-0 space-y-4">
              <Card padding="none" aria-label="Pickup information" className="overflow-hidden">
                <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border-default bg-primary-50/50 p-4 sm:p-5">
                  <OrderPickupCode code={order.orderCode} compact />
                  <CancelOrderAction
                    order={order}
                    now={now}
                    busy={busy}
                    onCancel={() => update(() => orderApi.cancel(orderId))}
                  />
                  {order.createdAt && (
                    <p className="flex w-full items-center gap-1.5 text-caption text-text-secondary">
                      <Clock3 size={14} aria-hidden="true" />
                      <time dateTime={order.createdAt}>
                        Placed {checkoutTime(order.createdAt)} · GMT+7
                      </time>
                    </p>
                  )}
                </div>
                <div className="space-y-4 p-4 sm:p-5">
                  <h2 className="text-body font-semibold text-text-primary">Pickup information</h2>
                  <OrderStatusPanel order={order} compact />
                  <OrderPickupDeadlines order={order} now={now} compact />
                  <p className="text-caption text-text-secondary">
                    GMT+7 · Pickup deadlines do not extend during payment.
                  </p>
                  <details className="group border-t border-border-default pt-2">
                    <summary className="min-h-8 cursor-pointer text-caption font-medium text-text-secondary">
                      About these statuses
                    </summary>
                    <div className="pt-3">
                      <OrderStatusPanel order={order} />
                    </div>
                  </details>
                </div>
              </Card>
              <Card
                padding="none"
                aria-label="Shop and ordered products"
                className="overflow-hidden"
              >
                <header className="flex items-center gap-3 border-b border-border-default bg-primary-50/50 p-4 sm:p-5">
                  <Link
                    to={APP_PATHS.SHOP_DETAIL.replace(":id", encodeURIComponent(order.shopId))}
                    className="shrink-0 transition-opacity hover:opacity-80"
                    aria-label={`Visit ${order.shopName}`}
                  >
                    <MediaImage
                      src={order.shopLogoUrl}
                      alt={order.shopName}
                      fallbackIcon={Store}
                      showFallbackLabel={false}
                      className="size-10 shrink-0 rounded-md cursor-pointer"
                    />
                  </Link>
                  <div className="min-w-0">
                    <p className="text-caption font-medium text-text-secondary">Pickup shop</p>
                    <h2 className="mt-0.5 break-words text-section font-bold text-text-primary">
                      <Link
                        to={APP_PATHS.SHOP_DETAIL.replace(":id", encodeURIComponent(order.shopId))}
                        className="transition-colors hover:text-primary-700 hover:underline cursor-pointer"
                      >
                        {order.shopName}
                      </Link>
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
                  <OrderItems
                    items={order.items}
                    onEditReview={setEditingFeedbackTarget}
                    onRate={order.status === "Completed" ? setRatingItemId : undefined}
                  />
                </div>
                <div className="flex items-center justify-between gap-4 border-t border-border-default bg-neutral-50/70 px-4 py-3 sm:px-5">
                  <p className="text-body font-semibold text-text-secondary">Order total</p>
                  <p className="text-section font-bold tabular-nums text-primary-700">
                    {money(order.totalAmount)}
                  </p>
                </div>
              </Card>
            </div>
            <div className="min-w-0 space-y-4">
              <aside
                aria-label="Payment summary"
                className="min-w-0 space-y-4 rounded-lg border border-border-default bg-bg-surface p-4 shadow-sm sm:p-5"
              >
                <h2 className="flex items-center gap-2 text-section font-bold text-text-primary">
                  <CreditCard aria-hidden="true" size={20} className="text-primary-600" />
                  Payment summary
                </h2>
                <OrderPaymentAmounts order={order} />
                <dl className="space-y-3 border-t border-border-default pt-4 text-body">
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
                    <CustomerActionButton
                      loading={busy}
                      size="md"
                      type="button"
                      disabled={busy || expired}
                      onClick={() => void update(() => paymentApi.payment(orderId), true)}
                      className="min-h-11 w-full whitespace-normal !px-3 py-2"
                    >
                      <CreditCard aria-hidden="true" size={18} className="shrink-0" />
                      {busy
                        ? "Checking…"
                        : expired
                          ? "Payment expired · Verifying"
                          : `${order.paymentMethod === "PayAtShop" ? "Pay deposit" : "Pay online"} ${money(order.requiredOnlineAmount)}`}
                    </CustomerActionButton>
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
                      Later level or policy changes do not affect the deposit recorded on this
                      order.
                    </p>
                  </div>
                )}
              </aside>
              {["Completed", "Rejected", "Expired"].includes(order.status) && (
                <CustomerOrderRefunds key={`${order.id}:${order.status}`} order={order} />
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
