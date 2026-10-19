import { useShopOrderEventsStore } from "@/features/shop-orders/stores/shopOrderEventsStore";
import { Pagination } from "@/shared/components/pagination";
import { ConfirmationCountdown } from "@/features/shop-orders/components/ConfirmationCountdown";
import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Search, Package } from "lucide-react";
import { shopOrderApi } from "@/features/shop-orders/api/shopOrderApi";
import type { ShopOrderPage } from "@/features/shop-orders/types/shopOrder.types";
import { OrderStatusPanel } from "@/features/checkout/components/OrderStatusPanel";
import { OrderPickupDeadlines } from "@/features/checkout/components/OrderPickupDeadlines";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { getApiErrorMessage } from "@/shared/api";
const statuses = [
  "All",
  "Pending",
  "Confirmed",
  "Preparing",
  "ReadyForPickup",
  "Completed",
  "Rejected",
  "Cancelled",
  "Expired",
  "NoShow",
];
import { useAuthStore } from "@/shared/stores/authStore";
export function ShopOrdersPage() {
  const { shopId, orderId } = useParams();
  const userId = useAuthStore((state) => state.user?.id);
  return <ShopOrdersPageContent key={`${userId}:${shopId}:${orderId}`} />;
}
function ShopOrdersPageContent() {
  const { shopId: staffShopId } = useParams();
  const [shopId, setShopId] = useState(staffShopId ?? "");
  const shopRevision = useShopOrderEventsStore((state) => state.revisions[shopId] ?? 0);
  const reconnectRevision = useShopOrderEventsStore((state) => state.reconnectRevision);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("All");
  const [input, setInput] = useState("");
  const [search, setSearch] = useState("");
  const [orderDate, setOrderDate] = useState("");
  const [searchDate, setSearchDate] = useState<string | undefined>();
  const [data, setData] = useState<ShopOrderPage | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [retry, setRetry] = useState(0);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  const generation = useRef(0);
  const base = staffShopId ? `/staff/shops/${staffShopId}/orders` : "/store-owner/orders";
  useEffect(() => {
    const abort = new AbortController();
    if (staffShopId) return;
    void shopOrderApi
      .ownerShop(abort.signal)
      .then(setShopId)
      .catch((e) => {
        if (!abort.signal.aborted) {
          setError(getApiErrorMessage(e));
          setLoading(false);
        }
      });
    return () => abort.abort();
  }, [staffShopId]);
  useEffect(() => {
    if (!shopId) return;
    const abort = new AbortController();
    const current = ++generation.current;
    void shopOrderApi.page(shopId, status, search, page, abort.signal, searchDate)
      .then((next) => {
        if (current === generation.current && !abort.signal.aborted) {
          const lastPage = Math.max(1, next.page.totalPages);
          if (page > lastPage) {
            setPage(lastPage);
            return;
          }
          setError("");
          setData(next);
        }
      })
      .catch((e) => {
        if (!abort.signal.aborted && current === generation.current) setError(getApiErrorMessage(e));
      })
      .finally(() => {
        if (!abort.signal.aborted && current === generation.current) {
          setLoading(false);
        }
      });
    return () => abort.abort();
  }, [shopId, status, search, searchDate, page, retry, shopRevision, reconnectRevision]);
  function reset() {
    setPage(1);
    generation.current++;
    setLoading(true);
    setData(null);
    setError("");
  }
  return (
    <div className="min-w-0 space-y-4">
      <div>
        <h1 className="text-xl font-bold text-neutral-900">Shop orders</h1>
        <p className="mt-1 text-sm text-neutral-500">
          Confirm paid orders within 5 minutes, prepare items and verify payment before handover.
        </p>
      </div>
      <form
        className="flex max-w-lg gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          reset();
          setSearch(input.trim());
          setSearchDate(/^\d{1,6}$/.test(input.trim()) ? orderDate || undefined : undefined);
          setRetry((value) => value + 1);
        }}
      >
        <label className="relative min-w-0 flex-1">
          <Search size={16} className="absolute left-3 top-3 text-neutral-400" />
          <span className="sr-only">Order ID or order code</span>
          <input
            value={input}
            onChange={(e) => {
              const value = e.target.value;
              setInput(value);
              if (!value.trim()) {
                setOrderDate("");
                if (search || searchDate) {
                  reset();
                  setSearch("");
                  setSearchDate(undefined);
                }
              }
            }}
            maxLength={64}
            placeholder="Pickup code or exact order ID"
            className="h-10 w-full rounded-lg border border-neutral-200 bg-white pl-9 pr-3 text-sm"
          />
        </label>
        {/^\d{1,6}$/.test(input.trim()) && (
          <label className="shrink-0 text-xs text-neutral-500">
            <span className="sr-only">Order date (Vietnam time)</span>
            <input type="date" value={orderDate || new Intl.DateTimeFormat("en-CA", { timeZone: "Asia/Ho_Chi_Minh", year: "numeric", month: "2-digit", day: "2-digit" }).format(new Date(now))} onChange={(e) => setOrderDate(e.target.value)} required
              aria-label="Order date (Vietnam time)" className="h-10 rounded-lg border border-neutral-200 bg-white px-2 text-sm" />
          </label>
        )}
        <button className="rounded-lg bg-emerald-600 px-4 text-sm font-semibold text-white">
          Search
        </button>
      </form>
      {/^\d{1,6}$/.test(input.trim()) && <p className="text-xs text-neutral-500">Pickup codes repeat daily. Select the order date in Vietnam time, especially for overnight pickups.</p>}
      <div
        className="flex gap-2 overflow-x-auto pb-2 [scrollbar-width:thin]"
        aria-label="Filter shop orders"
      >
        {statuses.map((value) => (
          <button
            key={value}
            onClick={() => {
              if (status !== value) {
                reset();
                setStatus(value);
              }
            }}
            className={`shrink-0 rounded-lg px-3 py-2 text-xs font-semibold ${status === value ? "bg-emerald-600 text-white" : "border border-neutral-200 bg-white text-neutral-600"}`}
          >
            {value === "ReadyForPickup"
              ? "Ready for pickup"
              : value === "NoShow"
                ? "No show"
                : value}{" "}
            {data?.statusCounts[value] ?? 0}
          </button>
        ))}
      </div>
      {error && (
        <div role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {error}
          <button
            onClick={() => {
              setError("");
              setRetry((value) => value + 1);
            }}
            className="ml-3 underline"
          >
            Retry
          </button>
        </div>
      )}
      {loading ? (
        <p className="py-8 text-sm text-neutral-500">Loading orders…</p>
      ) : data?.page.items.length === 0 ? (
        <div className="rounded-xl border border-neutral-200 bg-white p-8 text-center text-sm text-neutral-500">
          <Package className="mx-auto mb-2" />
          No matching orders.
        </div>
      ) : (
        data?.page.items.map((row) => (
          <article
            key={row.order.id}
            className="min-w-0 rounded-xl border border-neutral-200 bg-white p-4"
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="break-all text-sm font-bold text-neutral-900">
                  {row.order.orderCode}
                </p>
                <p className="mt-1 text-xs text-neutral-500">
                  {row.customerName}
                  {row.customerPhone ? ` · ${row.customerPhone}` : ""}
                </p>
              </div>
              <div className="text-right">
                <p className="mb-2 text-xs text-neutral-400">{checkoutTime(row.order.createdAt)}</p>
                <Link
                  to={`${base}/${row.order.id}`}
                  className="rounded-lg border border-emerald-200 px-3 py-2 text-xs font-semibold text-emerald-700"
                >
                  View details
                </Link>
              </div>
            </div>
            <div className="mt-3">
              <OrderStatusPanel order={row.order} compact />
            </div>
            {row.confirmationDeadlineAt && (
              <ConfirmationCountdown deadline={row.confirmationDeadlineAt} now={now} />
            )}
            <div className="my-3 space-y-2 border-y border-neutral-100 py-3">
              {row.order.items.slice(0, 2).map((item) => (
                <div key={item.flashDealVariantId} className="flex items-center gap-3">
                  {item.imageUrl ? (
                    <img src={item.imageUrl} alt="" className="size-11 rounded-lg object-cover" />
                  ) : (
                    <div className="size-11 shrink-0 rounded-lg bg-neutral-100" />
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium">{item.productName}</p>
                    <p className="text-xs text-neutral-500">
                      {item.variantName} · Qty {item.quantity}
                    </p>
                  </div>
                  <span className="text-sm font-semibold">{money(item.subtotal)}</span>
                </div>
              ))}
              {row.order.items.length > 2 && (
                <Link to={`${base}/${row.order.id}`} className="text-xs text-emerald-700">
                  +{row.order.items.length - 2} more products · View details
                </Link>
              )}
            </div>
            <OrderPickupDeadlines order={row.order} now={now} compact />
            <div className="mt-3 grid grid-cols-3 gap-2 border-t border-neutral-100 pt-3 text-right">
              {[
                ["Total", row.order.totalAmount],
                ["Paid", row.order.paidAmount ?? 0],
                ["Due", row.order.outstandingAmount ?? 0],
              ].map(([label, value]) => (
                <div key={label}>
                  <p className="text-xs text-neutral-500">{label}</p>
                  <p className="mt-1 text-sm font-bold">{money(Number(value))}</p>
                </div>
              ))}
            </div>
          </article>
        ))
      )}
      {!loading && data && !error && (
        <Pagination
          currentPage={data.page.page}
          totalPages={data.page.totalPages}
          totalItems={data.page.totalItems}
          pageSize={data.page.pageSize}
          itemLabel="orders"
          size="sm"
          onPageChange={(next) => {
            if (next === page) return;
            generation.current++;
            setLoading(true);
            setError("");
            setPage(next);
          }}
        />
      )}
    </div>
  );
}
