import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import { readCustomerOrderPages } from "@/features/orders/utils/readCustomerOrderPages";
import { OrderPickupCode } from "@/features/checkout/components/OrderPickupCode";
import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ArrowRight, CheckCircle2, MapPin, Package, ShoppingBag, Star, Store } from "lucide-react";
import { RateOrderModal } from "@/features/feedbacks/components/RateOrderModal";
import { APP_PATHS } from "@/app/router/paths";
import { orderApi } from "@/features/orders/api/orderApi";
import { OrderStatusPanel } from "@/features/checkout/components/OrderStatusPanel";
import { OrderPickupDeadlines } from "@/features/checkout/components/OrderPickupDeadlines";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

const tabs = [
  ["All", "All orders"],
  ["AwaitingPayment", "Awaiting payment"],
  ["Pending", "Pending"],
  ["Confirmed", "Confirmed"],
  ["Preparing", "Preparing"],
  ["ReadyForPickup", "Ready for pickup"],
  ["Completed", "Completed"],
  ["Cancelled", "Cancelled"],
  ["Expired", "Expired"],
  ["NoShow", "No show"],
  ["Rejected", "Rejected"],
] as const;

export function OrdersPage() {
  const userId = useAuthStore((state) => state.user?.id);
  const [params, setParams] = useSearchParams();
  const requested = params.get("status") ?? "All";
  const status = tabs.some((tab) => tab[0] === requested) ? requested : "All";
  const [refresh, setRefresh] = useState(0);
  return (
    <CustomerOrders
      key={`${userId}:${status}:${refresh}`}
      status={status}
      onFilter={(value) =>
        setParams((current) => {
          const next = new URLSearchParams(current);
          if (value === "All") next.delete("status");
          else next.set("status", value);
          return next;
        })
      }
      onRefresh={() => setRefresh((value) => value + 1)}
    />
  );
}
function CustomerOrders({
  status,
  onFilter,
  onRefresh,
}: {
  status: string;
  onFilter: (value: string) => void;
  onRefresh: () => void;
}) {
  const realtime = useRealtimeSubscriptions();
  const generation = useRef(0);
  const loadedItems = useRef(10);
  const [orders, setOrders] = useState<CheckoutOrder[]>([]);
  const [counts, setCounts] = useState<Record<string, number>>({});
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [ratingOrder, setRatingOrder] = useState<CheckoutOrder | null>(null);
  const [now, setNow] = useState(Date.now);
  const alive = useRef(true);
  const operation = useRef(false);
  const abort = useRef<AbortController | null>(null);
  const sentinel = useRef<HTMLDivElement>(null);
  useEffect(() => {
    alive.current = true;
    const controller = new AbortController();
    let reading = false;
    let dirty = false;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      operation.current = true;
      abort.current?.abort();
      try {
        while (dirty && !controller.signal.aborted) {
          dirty = false;
          const current = ++generation.current;
          const result = await readCustomerOrderPages(
            (cursor) => orderApi.orderPage(status, cursor, controller.signal),
            loadedItems.current,
          );
          if (controller.signal.aborted || current !== generation.current) return;
          loadedItems.current = result.page.items.length;
          setOrders(result.page.items);
          setCounts(result.statusCounts);
          setNextCursor(result.page.nextCursor);
          setHasMore(result.page.hasMore);
          setPageError(null);
        }
      } catch (error) {
        if (!controller.signal.aborted) setPageError(getApiErrorMessage(error));
      } finally {
        reading = false;
        if (!controller.signal.aborted) {
          operation.current = false;
          setLoading(false);
          setLoadingMore(false);
        }
      }
    }
    const off = subscribeCustomerOrderEvents(realtime, () => {
      void refresh();
    });
    void refresh();
    const clock = window.setInterval(() => setNow(Date.now()), 1000);
    return () => {
      off();
      alive.current = false;
      controller.abort();
      abort.current?.abort();
      window.clearInterval(clock);
    };
  }, [status, realtime]);
  const loadMore = useCallback(async () => {
    if (operation.current || !hasMore || !nextCursor) return;
    operation.current = true;
    const currentGeneration = generation.current;
    setLoadingMore(true);
    setPageError(null);
    const controller = new AbortController();
    abort.current = controller;
    try {
      const result = await orderApi.orderPage(status, nextCursor, controller.signal);
      if (!alive.current || controller.signal.aborted || currentGeneration !== generation.current)
        return;
      const merged = [
        ...new Map([...orders, ...result.page.items].map((order) => [order.id, order])).values(),
      ];
      loadedItems.current = merged.length;
      setOrders(merged);
      setCounts(result.statusCounts);
      setNextCursor(result.page.nextCursor);
      setHasMore(result.page.hasMore);
    } catch (error) {
      if (alive.current && !controller.signal.aborted) setPageError(getApiErrorMessage(error));
    } finally {
      if (currentGeneration === generation.current) {
        operation.current = false;
        if (alive.current) setLoadingMore(false);
      }
    }
  }, [hasMore, nextCursor, status, orders]);
  useEffect(() => {
    if (loading || loadingMore || pageError || !hasMore || !sentinel.current) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) void loadMore();
      },
      { rootMargin: "300px" },
    );
    observer.observe(sentinel.current);
    return () => observer.disconnect();
  }, [loading, loadingMore, pageError, hasMore, loadMore, orders.length]);
  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-neutral-900">My orders</h1>
          <p className="mt-1 text-sm text-neutral-500">
            Track your meals, payments, and pickup times. All times are GMT+7.
          </p>
        </div>
      </div>
      <nav
        aria-label="Filter orders by status"
        className="order-status-scroll flex max-w-full gap-2 overflow-x-auto rounded-xl border border-neutral-200 bg-white p-2"
      >
        {tabs.map(([value, label]) => (
          <button
            key={value}
            type="button"
            aria-pressed={status === value}
            onClick={() => onFilter(value)}
            className={`inline-flex shrink-0 items-center gap-2 rounded-lg px-3 py-2.5 text-sm font-semibold transition ${status === value ? "bg-emerald-600 text-white" : "text-neutral-600 hover:bg-neutral-100"}`}
          >
            {label}
            <span
              className={`rounded-full px-1.5 py-0.5 text-xs ${status === value ? "bg-white/20" : "bg-neutral-100"}`}
            >
              {loading
                ? "…"
                : value === "All"
                  ? Object.values(counts).reduce((sum, count) => sum + count, 0)
                  : (counts[value] ?? 0)}
            </span>
          </button>
        ))}
      </nav>
      {pageError && (
        <div
          role="alert"
          className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
        >
          {pageError}
          <button
            type="button"
            onClick={() => (orders.length ? void loadMore() : onRefresh())}
            className="ml-3 font-semibold underline"
          >
            Retry
          </button>
        </div>
      )}
      {loading && (
        <p role="status" className="py-8 text-center text-sm text-neutral-500">
          Loading orders…
        </p>
      )}
      {!loading && !pageError && !hasMore && orders.length === 0 && (
        <div className="rounded-2xl border border-neutral-200 bg-white p-10 text-center">
          <ShoppingBag size={36} className="mx-auto text-emerald-600" />
          <h2 className="mt-4 font-semibold text-neutral-900">
            {status === "All" ? "You have no orders yet" : "No orders in this status"}
          </h2>
          <p className="mt-2 text-sm text-neutral-500">
            {status === "All"
              ? "Find a deal and save a meal today."
              : "Choose another status to find your orders."}
          </p>
          <Link to={APP_PATHS.CART} className="mt-4 inline-block font-semibold text-emerald-700">
            Open cart
          </Link>
        </div>
      )}
      <RateOrderModal
        isOpen={Boolean(ratingOrder)}
        onClose={() => setRatingOrder(null)}
        order={ratingOrder}
        onSubmitted={onRefresh}
      />
      {orders.map((order) => (
        <article
          key={order.id}
          aria-label={`Order ${order.orderCode}`}
          className="overflow-hidden rounded-2xl border border-neutral-200 bg-white shadow-xs"
        >
          <header className="px-4 pt-4 sm:px-5 sm:pt-5">
            <div className="mb-4 flex items-start justify-between gap-3 border-b border-neutral-100 pb-3 text-xs text-neutral-500">
              <div className="min-w-0 flex-1">
                <OrderPickupCode code={order.orderCode} compact />
              </div>
              <div className="flex shrink-0 flex-col items-end gap-2">
                {order.createdAt && (
                  <time dateTime={order.createdAt}>{checkoutTime(order.createdAt)}</time>
                )}
                <Link
                  to={APP_PATHS.ORDER_DETAIL.replace(":orderId", encodeURIComponent(order.id))}
                  className="inline-flex min-h-10 shrink-0 items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-700"
                >
                  View details
                  <ArrowRight size={16} />
                </Link>
              </div>
            </div>
            <div className="flex min-w-0 items-center gap-3">
              <span className="relative flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-neutral-100 text-emerald-700">
                <Store size={20} />
                {order.shopLogoUrl && (
                  <img
                    src={order.shopLogoUrl}
                    alt=""
                    onError={(event) => {
                      event.currentTarget.style.display = "none";
                    }}
                    className="absolute inset-0 size-full object-cover"
                  />
                )}
              </span>
              <div className="min-w-0 flex-1">
                <h2 className="truncate font-bold text-neutral-900">{order.shopName}</h2>
                {order.shopAddress && (
                  <p className="mt-0.5 flex items-center gap-1 text-xs text-neutral-500">
                    <MapPin size={12} className="shrink-0" />
                    <span className="truncate">{order.shopAddress}</span>
                  </p>
                )}
              </div>
            </div>
            <div className="mt-3">
              <OrderStatusPanel order={order} compact />
            </div>
          </header>
          <div className="space-y-3 p-4 sm:p-5">
            <ul className="divide-y divide-neutral-100" aria-label="Ordered products">
              {order.items.slice(0, 2).map((item) => (
                <li key={item.flashDealVariantId} className="flex items-start gap-3 py-3">
                  <span className="relative flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-neutral-100 text-neutral-400">
                    <Package size={24} />
                    {item.imageUrl && (
                      <img
                        loading="lazy"
                        src={item.imageUrl}
                        alt={item.productName}
                        onError={(event) => {
                          event.currentTarget.style.display = "none";
                        }}
                        className="absolute inset-0 size-full object-cover"
                      />
                    )}
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="line-clamp-2 text-sm font-semibold text-neutral-900">
                      {item.productName}
                    </p>
                    {item.variantName && (
                      <p className="mt-1 text-xs text-neutral-500">{item.variantName}</p>
                    )}
                    <p className="mt-1 text-xs text-neutral-500">
                      Qty {item.quantity} × {money(item.unitPrice)}
                    </p>
                    {item.rating && (
                      <Link
                        to={
                          item.feedbackId
                            ? `${APP_PATHS.ACCOUNT_REVIEWS}?feedbackId=${item.feedbackId}`
                            : APP_PATHS.ACCOUNT_REVIEWS
                        }
                        className="mt-1.5 inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-semibold text-emerald-700 hover:bg-emerald-100"
                      >
                        <CheckCircle2 size={12} />
                        Reviewed
                        <Star size={11} className="fill-amber-400 text-amber-400" />
                        {item.rating}/5
                      </Link>
                    )}
                  </div>
                  <p className="shrink-0 text-sm font-bold text-neutral-900">
                    {money(item.subtotal)}
                  </p>
                </li>
              ))}
            </ul>
            {order.items.length > 2 && (
              <p className="text-xs text-neutral-500">
                +{order.items.length - 2} more products · See all in{" "}
                <Link
                  to={APP_PATHS.ORDER_DETAIL.replace(":orderId", encodeURIComponent(order.id))}
                  className="font-semibold text-emerald-700 underline underline-offset-2 hover:text-emerald-800"
                >
                  order details
                </Link>
              </p>
            )}
            <section
              className="space-y-2 border-t border-neutral-100 pt-3"
              aria-label="Order payment summary"
            >
              <div className="flex items-center justify-between gap-3 text-xs text-neutral-500">
                <p className="flex items-center gap-1.5 font-medium">
                  <ShoppingBag size={14} aria-hidden="true" />
                  Payment summary
                </p>
                <p>{order.items.reduce((sum, item) => sum + item.quantity, 0)} items</p>
              </div>
              <OrderAmounts order={order} />
            </section>
            {order.status === "Completed" && order.items.some((i) => i.canReview) && (
              <div className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-amber-50 px-3.5 py-2.5 text-xs text-amber-800">
                <span className="inline-flex items-center gap-1.5 font-medium">
                  <Star size={14} className="shrink-0 fill-amber-400 text-amber-500" />
                  You haven&apos;t rated all items in this completed order yet.
                </span>
                <button
                  type="button"
                  onClick={() => setRatingOrder(order)}
                  className="font-semibold text-amber-900 underline underline-offset-2 hover:text-amber-700"
                >
                  Rate now
                </button>
              </div>
            )}
            <OrderPickupDeadlines order={order} now={now} compact />
          </div>
        </article>
      ))}
      <div
        id="orders-sentinel"
        ref={sentinel}
        aria-live="polite"
        className="min-h-10 py-3 text-center text-sm text-neutral-500"
      >
        {loadingMore
          ? "Loading more orders…"
          : !loading && !pageError && orders.length > 0
            ? hasMore
              ? "Scroll to load more orders"
              : "All orders loaded"
            : null}
      </div>
    </div>
  );
}

function OrderAmounts({ order }: { order: CheckoutOrder }) {
  const paid =
    order.paidAmount ?? (order.paymentStatus === "Succeeded" ? order.requiredOnlineAmount : 0);
  const outstanding =
    order.outstandingAmount ??
    (["Cancelled", "Expired", "NoShow", "Rejected"].includes(order.status)
      ? 0
      : Math.max(0, order.totalAmount - paid));
  return (
    <dl
      aria-label="Payment amounts"
      className="grid grid-cols-3 divide-x divide-neutral-200 rounded-lg bg-neutral-50 px-1 py-3 sm:px-2"
    >
      {[
        { label: "Order total", amount: order.totalAmount, color: "text-neutral-900" },
        { label: "Amount paid", amount: paid, color: "text-emerald-700" },
        {
          label: "Amount due",
          amount: outstanding,
          color: outstanding > 0 ? "text-amber-700" : "text-emerald-700",
        },
      ].map(({ label, amount, color }) => (
        <div key={label} className="min-w-0 px-2 text-right sm:px-3">
          <dt className="text-[11px] font-medium text-neutral-500 sm:text-xs">{label}</dt>
          <dd
            className={`mt-1 break-words text-sm font-bold leading-snug tabular-nums sm:text-base ${color}`}
          >
            {money(amount)}
          </dd>
        </div>
      ))}
    </dl>
  );
}
