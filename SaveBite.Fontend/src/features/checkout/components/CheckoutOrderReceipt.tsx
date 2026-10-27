import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import { CustomerOrderRefunds } from "@/features/refunds/components/CustomerOrderRefunds";
import { OrderPickupDeadlines } from "./OrderPickupDeadlines";
import { OrderPickupCode } from "./OrderPickupCode";
import { OrderStatusPanel } from "./OrderStatusPanel";
import { CancelOrderAction } from "./CancelOrderAction";
import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, CreditCard, Store, Package, MapPin, ShoppingBag } from "lucide-react";
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
  const [now, setNow] = useState(Date.now);
  const alive = useRef(true);
  const revision = useRef(0);
  const operation = useRef(false);
  const pending = order?.paymentStatus === "Pending" && order.status === "AwaitingPayment";
  const expired = !!order?.paymentExpiresAt && Date.parse(order.paymentExpiresAt) <= now;

  useEffect(() => {
    alive.current = true;
    const clock = window.setInterval(() => setNow(Date.now()), 1000);
    return () => { alive.current = false; window.clearInterval(clock); };
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
          if (!controller.signal.aborted && alive.current && current === revision.current) setOrder(next);
        }
      } catch (error) {
        if (!controller.signal.aborted && alive.current) setError(getApiErrorMessage(error));
      } finally { reading = false; }
    }
    const off = subscribeCustomerOrderEvents(realtime, () => { void refresh(); }, orderId);
    void refresh();
    return () => { off(); controller.abort(); };
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
    <div className="w-full min-w-0 py-5 md:py-6">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <Link
          to={APP_PATHS.ACCOUNT_ORDERS}
          className="inline-flex items-center gap-2 text-sm font-medium text-neutral-500 hover:text-emerald-700"
        >
          <ArrowLeft size={16} />
          My orders
        </Link>
      </div>
      <div className="mb-4 flex flex-wrap items-end justify-between gap-2">
        <div>
          <h1 className="text-2xl font-bold text-neutral-900">Order details</h1>
        </div>
        {order?.createdAt && (
          <p className="text-xs text-neutral-500">Placed {checkoutTime(order.createdAt)} · GMT+7</p>
        )}
      </div>
      {error && (
        <p
          role="alert"
          className="mb-5 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
        >
          {error}
        </p>
      )}
      {!order && !error && (
        <p role="status" className="py-12 text-center text-neutral-500">
          Verifying your order status…
        </p>
      )}
      {order && (
        <div className="space-y-4">
          <section
            aria-label="Order overview"
            className="flex flex-wrap items-center justify-between gap-x-6 gap-y-3 rounded-xl border border-neutral-200 bg-white px-4 py-3"
          >
            <OrderPickupCode code={order.orderCode} compact />
            <OrderStatusPanel order={order} compact />
          </section>
          {order.paymentReviewRequired && (
            <p
              role="alert"
              className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
            >
              Your payment needs review. Contact the shop to check the payment; your pickup deadline
              has not been extended.
            </p>
          )}
          {order.paymentMessage && (
            <p
              role="alert"
              className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900"
            >
              {order.paymentMessage}
            </p>
          )}
          <div className="grid min-w-0 items-start gap-4 lg:grid-cols-[minmax(0,1fr)_300px]">
            <div className="min-w-0 space-y-4">
              <section
                aria-label="Shop and ordered products"
                className="overflow-hidden rounded-xl border border-neutral-200 bg-white"
              >
                <header className="flex items-center gap-3 border-b border-neutral-100 p-4 sm:p-5">
                  <OrderImage src={order.shopLogoUrl} alt={order.shopName} shop />
                  <div className="min-w-0">
                    <p className="text-xs font-medium text-neutral-500">Pickup shop</p>
                    <h2 className="mt-0.5 break-words text-base font-bold text-neutral-900">
                      {order.shopName}
                    </h2>
                    {order.shopAddress && (
                      <p className="mt-1 flex items-start gap-1.5 text-xs text-neutral-500">
                        <MapPin size={15} className="mt-0.5 shrink-0" />
                        <span className="break-words">{order.shopAddress}</span>
                      </p>
                    )}
                  </div>
                </header>
                <div className="px-4 sm:px-5">
                  <div className="flex items-center justify-between gap-3 pt-4">
                    <h3 className="flex items-center gap-2 text-sm font-semibold text-neutral-900">
                      <ShoppingBag size={17} className="text-emerald-600" />
                      Ordered products
                    </h3>
                    <span className="text-xs text-neutral-500">
                      {order.items.reduce((sum, item) => sum + item.quantity, 0)} items
                    </span>
                  </div>
                  <ul className="divide-y divide-neutral-100" aria-label="Ordered products">
                    {order.items.map((item) => (
                      <li
                        key={item.flashDealVariantId}
                        className="grid grid-cols-[64px_minmax(0,1fr)] gap-x-3 gap-y-2 py-4 sm:grid-cols-[64px_minmax(0,1fr)_auto] sm:gap-x-4"
                      >
                        <div className="row-span-2 sm:row-span-1">
                          <OrderImage src={item.imageUrl} alt={item.productName} />
                        </div>
                        <div className="min-w-0">
                          <p className="break-words text-sm font-semibold text-neutral-900">
                            {item.productName}
                          </p>
                          {item.variantName && (
                            <p className="mt-1 text-xs text-neutral-500">{item.variantName}</p>
                          )}
                          <p className="mt-1.5 text-xs text-neutral-500">
                            Qty{" "}
                            <span className="font-semibold text-neutral-800">{item.quantity}</span>{" "}
                            × {money(item.unitPrice)}
                          </p>
                        </div>
                        <p className="col-start-2 text-sm font-bold tabular-nums text-neutral-900 sm:col-start-auto sm:text-right">
                          {money(item.subtotal)}
                        </p>
                      </li>
                    ))}
                  </ul>
                </div>
                <div className="flex items-center justify-between gap-4 border-t border-neutral-100 bg-neutral-50/70 px-4 py-3 sm:px-5">
                  <p className="text-sm font-semibold text-neutral-600">Order total</p>
                  <p className="text-xl font-bold tabular-nums text-emerald-700">
                    {money(order.totalAmount)}
                  </p>
                </div>
              </section>
              <section
                aria-label="Pickup information"
                className="rounded-xl border border-neutral-200 bg-white p-4 sm:p-5"
              >
                <h2 className="mb-3 text-sm font-semibold text-neutral-900">Pickup information</h2>
                <OrderPickupDeadlines order={order} now={now} compact />
                <p className="mt-3 text-xs leading-relaxed text-neutral-500">
                  GMT+7 · Pickup deadlines do not extend during payment.
                </p>
              </section>

            </div>
            <aside
              aria-label="Payment summary"
              className="min-w-0 space-y-3 rounded-xl border border-neutral-200 bg-white p-4 sm:p-5 lg:sticky lg:top-24"
            >
              <h2 className="flex items-center gap-2 text-base font-bold text-neutral-900">
                <CreditCard size={20} className="text-emerald-600" />
                Payment summary
              </h2>
              <dl className="space-y-3 text-xs">
                <div className="flex justify-between gap-4">
                  <dt className="text-neutral-500">Method</dt>
                  <dd className="min-w-0 max-w-[65%] break-words text-right font-semibold text-neutral-800">
                    {order.paymentMethod === "PayAtShop"
                      ? "Online deposit + pay at shop"
                      : "Full online payment"}
                  </dd>
                </div>
                <div className="flex justify-between gap-4">
                  <dt className="text-neutral-500">
                    {order.paymentStatus === "Succeeded"
                      ? "Paid online"
                      : "Required online payment"}
                  </dt>
                  <dd className="shrink-0 font-bold tabular-nums text-neutral-900">
                    {money(order.requiredOnlineAmount)}
                  </dd>
                </div>
                <div className="flex justify-between gap-4 border-t border-neutral-100 pt-4">
                  <dt className="font-semibold text-neutral-700">Remaining at shop</dt>
                  <dd className="shrink-0 text-lg font-extrabold tabular-nums text-emerald-700">
                    {money(order.remainingAtShop)}
                  </dd>
                </div>
              </dl>
              {pending && (
                <div className="space-y-3 border-t border-neutral-100 pt-4">
                  <p className="text-xs text-neutral-500">
                    Payment deadline: {checkoutTime(order.paymentExpiresAt)}
                  </p>
                  <button
                    type="button"
                    disabled={busy || expired}
                    onClick={() => void update(() => paymentApi.payment(orderId), true)}
                    className="flex min-h-12 w-full items-center justify-center gap-2 rounded-xl bg-emerald-600 px-3 py-3 text-sm font-bold text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:bg-neutral-200 disabled:text-neutral-500"
                  >
                    <CreditCard size={18} className="shrink-0" />
                    {busy
                      ? "Checking…"
                      : expired
                        ? "Payment expired · Verifying"
                        : `${order.paymentMethod === "PayAtShop" ? "Pay deposit" : "Pay online"} ${money(order.requiredOnlineAmount)}`}
                  </button>
                </div>
              )}
              {order.policy && (
                <div className="rounded-lg bg-neutral-50 p-3">
                  <p className="text-xs font-medium text-neutral-500">Your policy at checkout</p>
                  <p className="mt-1 text-sm font-semibold text-neutral-900">
                    {order.policy.levelRank ? `Level ${order.policy.levelRank} · ` : ""}
                    {order.policy.levelName}
                  </p>
                  <p className="mt-2 text-xs leading-relaxed text-neutral-600">
                    {order.paymentMethod === "OnlinePayment"
                      ? "Full order amount is required online."
                      : order.policy.appliedRange
                        ? `Over ${money(order.policy.appliedRange.fromOrderAmount)} up to ${money(order.policy.appliedRange.toOrderAmount)} · Fixed deposit ${money(order.depositAmount)}.`
                        : `No deposit required for orders up to ${money(order.policy.freeOrderAmount ?? 0)}.`}
                  </p>
                  <p className="mt-2 text-xs leading-relaxed text-neutral-500">
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
    <span
      className={`relative flex shrink-0 items-center justify-center overflow-hidden border border-neutral-100 bg-neutral-50 text-neutral-400 ${shop ? "size-12 rounded-lg" : "size-16 rounded-lg"}`}
    >
      {shop ? <Store size={27} /> : <Package size={27} />}
      {src && (
        <img
          loading="lazy"
          src={src}
          alt={alt}
          onError={(event) => {
            event.currentTarget.style.display = "none";
          }}
          className="absolute inset-0 size-full object-cover"
        />
      )}
    </span>
  );
}
