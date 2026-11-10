import { CustomerActionButton } from "@/shared/components/CustomerMutationControls";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import { Pagination } from "@/shared/components/pagination";
import { OrderPickupCode } from "@/features/checkout/components/OrderPickupCode";
import { useCallback, useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ArrowRight, ChevronDown, Clock3, MapPin, ShoppingBag, Star, Store } from "lucide-react";
import {
  Button,
  ButtonLink,
  Card,
  EmptyState,
  ErrorState,
  MediaImage,
  FilterBar,
  Skeleton,
} from "@/shared/components/ui";
import { OrderItems, type OrderReviewTarget } from "@/features/orders/components/OrderItems";
import { OrderPaymentAmounts } from "@/features/orders/components/OrderPaymentAmounts";
import { RateOrderModal } from "@/features/feedbacks/components/RateOrderModal";
import { EditReviewModal } from "@/features/feedbacks/components/EditReviewModal";
import { APP_PATHS } from "@/app/router/paths";
import { orderApi } from "@/features/orders/api/orderApi";
import { OrderStatusPanel } from "@/features/checkout/components/OrderStatusPanel";
import { OrderPickupDeadlines } from "@/features/checkout/components/OrderPickupDeadlines";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
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
  const requestedPage = Number(params.get("page") ?? 1);
  const page = Number.isSafeInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1;
  const [refresh, setRefresh] = useState(0);
  const changePage = useCallback((value: number) => setParams((current) => {
    const next = new URLSearchParams(current);
    if (value === 1) next.delete("page"); else next.set("page", String(value));
    return next;
  }), [setParams]);
  return (
    <CustomerOrders
      key={`${userId}:${status}:${page}:${refresh}`}
      page={page}
      onPageChange={changePage}
      status={status}
      onFilter={(value) =>
        setParams((current) => {
          const next = new URLSearchParams(current);
          next.delete("page");
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
  page,
  onPageChange,
  status,
  onFilter,
  onRefresh,
}: {
  page: number;
  onPageChange: (page: number) => void;
  status: string;
  onFilter: (value: string) => void;
  onRefresh: () => void;
}) {
  const realtime = useRealtimeSubscriptions();
  const [orders, setOrders] = useState<CheckoutOrder[]>([]);
  const [counts, setCounts] = useState<Record<string, number>>({});
  const [loading, setLoading] = useState(true);
  const [pageError, setPageError] = useState<string | null>(null);
  const [ratingOrder, setRatingOrder] = useState<CheckoutOrder | null>(null);
  const [editingFeedbackTarget, setEditingFeedbackTarget] = useState<
    (OrderReviewTarget & { orderId: string }) | null
  >(null);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
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
          const result = await orderApi.numberedPage(status, page, controller.signal);
          if (controller.signal.aborted) return;
          setOrders(result.page.items);
          setCounts(result.statusCounts);
          const total = status === "All" ? Object.values(result.statusCounts).reduce((sum, count) => sum + count, 0) : result.statusCounts[status] ?? 0;
          const lastPage = Math.max(1, Math.ceil(total / 10));
          if (page > lastPage) onPageChange(lastPage);
          setPageError(null);
        }
      } catch (error) {
        if (!controller.signal.aborted) setPageError(getApiErrorMessage(error));
      } finally {
        reading = false;
        if (!controller.signal.aborted) {
          setLoading(false);
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
      controller.abort();
      window.clearInterval(clock);
    };
  }, [status, page, realtime, onPageChange]);
  const countFor = (value: string) =>
    value === "All"
      ? Object.values(counts).reduce((sum, count) => sum + count, 0)
      : (counts[value] ?? 0);
  return (
    <div className="min-w-0 space-y-4">
      <FilterBar
        filters={[
          {
            label: "Order status",
            value: status,
            options: tabs.map(([value, label]) => ({
              value,
              label: `${label}${loading ? "" : ` (${countFor(value)})`}`,
            })),
            onChange: onFilter,
          },
        ]}
      />
      <div className="flex flex-wrap items-center justify-between gap-2 text-caption text-text-secondary">
        <p role="status">
          {loading
            ? "Loading orders…"
            : `${orders.length} ${orders.length === 1 ? "order" : "orders"} shown · ${tabs.find(([value]) => value === status)?.[1]}`}
        </p>
        <span className="inline-flex items-center gap-1.5">
          <Clock3 size={14} aria-hidden="true" />
          Pickup times · GMT+7
        </span>
      </div>
      {pageError && (
        <ErrorState title="Couldn't load orders" description={pageError} onRetry={onRefresh} />
      )}
      {loading && (
        <div role="status" aria-label="Loading order cards" className="space-y-4">
          {[0, 1, 2].map((key) => (
            <Card padding="compact" key={key} className="space-y-4">
              <Skeleton className="h-14 w-3/4" />
              <Skeleton className="h-8 w-2/3" />
              <Skeleton className="h-24" />
              <Skeleton className="h-16" />
            </Card>
          ))}
        </div>
      )}
      {!loading && !pageError && orders.length === 0 && (
        <EmptyState
          icon={ShoppingBag}
          title={status === "All" ? "You have no orders yet" : "No orders in this status"}
          description={
            status === "All"
              ? "Find a deal and save a meal today."
              : "Choose another status to find your orders."
          }
          action={
            status === "All" ? (
              <ButtonLink to={APP_PATHS.CART}>Open cart</ButtonLink>
            ) : (
              <Button variant="secondary" onClick={() => onFilter("All")}>
                View all orders
              </Button>
            )
          }
        />
      )}
      <EditReviewModal
        isOpen={Boolean(editingFeedbackTarget)}
        onClose={() => setEditingFeedbackTarget(null)}
        feedbackId={editingFeedbackTarget?.feedbackId ?? null}
        initialProductName={editingFeedbackTarget?.productName}
        initialVariantName={editingFeedbackTarget?.variantName}
        initialImageUrl={editingFeedbackTarget?.imageUrl}
        orderId={editingFeedbackTarget?.orderId}
        onUpdated={onRefresh}
      />
      <RateOrderModal
        isOpen={Boolean(ratingOrder)}
        onClose={() => setRatingOrder(null)}
        order={ratingOrder}
        onSubmitted={onRefresh}
      />
      {orders.map((order) => {
        const detailUrl = APP_PATHS.ORDER_DETAIL.replace(":orderId", encodeURIComponent(order.id));
        return (
          <Card
            padding="none"
            key={order.id}
            aria-label={`Order ${order.orderCode}`}
            className="overflow-hidden"
          >
            <header className="space-y-4 border-b border-border-default bg-primary-50/40 p-4 sm:p-5">
              <div className="flex items-start gap-3">
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
                <div className="min-w-0 flex-1">
                  <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                    <Link
                      to={APP_PATHS.SHOP_DETAIL.replace(":id", encodeURIComponent(order.shopId))}
                      className="transition-colors hover:text-primary-700 hover:underline cursor-pointer"
                    >
                      {order.shopName}
                    </Link>
                  </h2>
                  {order.shopAddress && (
                    <p className="mt-1 flex items-start gap-1 text-caption text-text-secondary">
                      <MapPin size={14} className="mt-0.5 shrink-0" aria-hidden="true" />
                      <span
                        className="line-clamp-2 [overflow-wrap:anywhere]"
                        title={order.shopAddress}
                      >
                        {order.shopAddress}
                      </span>
                    </p>
                  )}
                </div>
              </div>
              <OrderStatusPanel order={order} compact />
            </header>
            <div className="space-y-3 p-4 sm:p-5">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <OrderPickupCode code={order.orderCode} compact showHint={false} />
                {order.createdAt && (
                  <time dateTime={order.createdAt} className="text-caption text-text-secondary">
                    Placed {checkoutTime(order.createdAt)}
                  </time>
                )}
              </div>
              {["AwaitingPayment", "Pending", "Confirmed", "Preparing", "ReadyForPickup"].includes(
                order.status,
              ) ? (
                <OrderPickupDeadlines order={order} now={now} compact />
              ) : (
                <details className="rounded-md bg-neutral-50 px-3">
                  <summary className="flex min-h-11 cursor-pointer list-none items-center gap-2 text-caption font-medium text-text-secondary">
                    <Clock3 size={14} aria-hidden="true" />
                    Pickup times · GMT+7
                    <ChevronDown size={15} aria-hidden="true" className="ml-auto" />
                  </summary>
                  <div className="pb-3">
                    <OrderPickupDeadlines order={order} now={now} compact />
                  </div>
                </details>
              )}
              {(order.paymentReviewRequired || order.paymentMessage) && (
                <p className="rounded-md bg-accent-50 p-3 text-caption text-accent-700">
                  {order.paymentMessage ||
                    "Your payment needs review. Open order details and contact the shop."}
                </p>
              )}
              <OrderItems
                items={order.items.slice(0, 2)}
                onEditReview={(target) => setEditingFeedbackTarget({ ...target, orderId: order.id })}
              />
              {order.items.length > 2 && (
                <p className="text-caption text-text-secondary">
                  +{order.items.length - 2} more products ·{" "}
                  <Link
                    to={detailUrl}
                    className="font-semibold text-primary-700 underline underline-offset-2"
                  >
                    See all items
                  </Link>
                </p>
              )}
              <section
                aria-label="Order payment summary"
                className="space-y-3 border-t border-border-default pt-4"
              >
                <div className="flex items-center justify-between gap-3 text-caption text-text-secondary">
                  <p className="font-medium">Payment summary</p>
                  <p>{order.items.reduce((sum, item) => sum + item.quantity, 0)} items</p>
                </div>
                <OrderPaymentAmounts order={order} />
              </section>
              <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border-default pt-4">
                {order.status === "Completed" && order.items.some((item) => item.canReview) ? (
                  <CustomerActionButton
                    variant="secondary"
                    onClick={() => setRatingOrder(order)}
                    className="min-h-11 !border-accent-100 !bg-accent-50 !text-accent-700 hover:!bg-accent-100"
                  >
                    <Star size={16} aria-hidden="true" />
                    Rate order
                  </CustomerActionButton>
                ) : null}
                <ButtonLink to={detailUrl} className="min-h-11 w-full sm:ml-auto sm:w-auto">
                  View details
                  <ArrowRight size={16} aria-hidden="true" />
                </ButtonLink>
              </div>
            </div>
          </Card>
        );
      })}
      {!loading && !pageError && countFor(status) > 10 && (
        <Pagination currentPage={page} totalPages={Math.max(1, Math.ceil(countFor(status) / 10))}
          totalItems={countFor(status)} pageSize={10} onPageChange={onPageChange}
          itemLabel="orders" showFirstLast />
      )}
    </div>
  );
}
