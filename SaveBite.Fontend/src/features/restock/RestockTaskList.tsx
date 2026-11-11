import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { User, PackageCheck, RefreshCw } from "lucide-react";
import { getApiErrorMessage } from "@/shared/api";
import { OrderResalePanel } from "@/features/shop-orders/components/OrderResalePanel";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { formatCartMoney } from "@/features/cart/utils/formatCartMoney";
import { useShopSuspended } from "@/shared/hooks/useShopSuspended";
import { useRestockWorkspace } from "./restockContext";
import { restockApi, type RestockPage } from "./restockApi";

export function RestockTaskList({
  pendingOnly,
  onChanged,
  flashDealId,
  readOnly = false,
}: {
  pendingOnly: boolean;
  onChanged?: () => void;
  flashDealId?: string;
  readOnly?: boolean;
}) {
  const { shopId, revision, refresh, error: workspaceError } = useRestockWorkspace();
  const { shopId: staffShopId } = useParams();
  const shopSuspended = useShopSuspended(shopId);
  const [result, setResult] = useState<{
    data: RestockPage;
    revision: number;
    page: number;
  } | null>(null);
  const [page, setPage] = useState(1);
  const [error, setError] = useState("");
  const base = staffShopId ? `/staff/shops/${staffShopId}` : "/store-owner";
  useEffect(() => {
    if (!shopId) return;
    const abort = new AbortController();
    async function load() {
      try {
        const data = await restockApi.list(shopId, pendingOnly, page, abort.signal, flashDealId);
        if (abort.signal.aborted) return;
        if (page > Math.max(1, data.totalPages)) {
          setPage(Math.max(1, data.totalPages));
          return;
        }
        setResult({ data, revision, page });
        setError("");
      } catch (cause) {
        if (!abort.signal.aborted) setError(getApiErrorMessage(cause));
      }
    }
    void load();
    return () => abort.abort();
  }, [shopId, revision, page, pendingOnly, flashDealId]);
  const data = result?.data;
  const updating =
    !!shopId &&
    !error &&
    !workspaceError &&
    (!result || result.revision !== revision || result.page !== page);
  return (
    <div className="space-y-4" aria-busy={updating}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-neutral-500">
          {data
            ? `${data.totalItems} ${pendingOnly ? "orders awaiting decisions" : "orders in restock history"}`
            : "Loading restock tasks…"}
        </p>
        <button
          type="button"
          onClick={() => {
            setError("");
            refresh();
          }}
          className="inline-flex min-h-11 items-center gap-2 rounded-lg border border-neutral-200 bg-white px-3 text-sm font-semibold"
        >
          <RefreshCw className={`size-4 ${updating ? "animate-spin" : ""}`} aria-hidden />
          Refresh
        </button>
      </div>
      {(error || workspaceError) && (
        <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {error || workspaceError}
        </p>
      )}
      {updating && (
        <p role="status" className="text-sm text-neutral-500">
          Updating restock tasks…
        </p>
      )}
      {data?.items.length === 0 && !updating && (
        <div className="rounded-xl border border-dashed border-neutral-300 bg-white p-8 text-center">
          <PackageCheck className="mx-auto mb-3 size-8 text-emerald-600" aria-hidden />
          <p className="font-semibold">
            {pendingOnly ? "No restock decisions pending" : "No restock history yet"}
          </p>
          <p className="mt-2 text-sm text-neutral-500">
            Cancelled and no-show orders appear here when stock needs a decision.
          </p>
        </div>
      )}
      {data?.items.map((task) => (
        <article
          key={task.orderId}
          className={`overflow-hidden rounded-xl border bg-white ${task.decision.canDecide ? "border-orange-300 shadow-sm" : "border-neutral-200"}`}
        >
          <div
            className={`space-y-3 border-b p-4 ${task.decision.canDecide ? "border-orange-200 bg-orange-50/60" : "border-neutral-100 bg-neutral-50"}`}
          >
            <div className="flex flex-wrap items-center justify-between gap-2">
              <Link
                to={`${base}/orders/${task.orderId}`}
                className="min-h-11 inline-flex items-center font-bold text-emerald-700 hover:underline"
              >
                Order {task.orderCode}
              </Link>
              <span className="rounded-full bg-white px-3 py-1 text-xs font-semibold">
                {task.orderStatus === "NoShow" ? "Customer no-show" : "Cancelled"}
              </span>
            </div>
            <div className="flex flex-wrap items-center gap-3 text-sm">
              {task.customerAvatarUrl ? (
                <img
                  src={task.customerAvatarUrl}
                  alt=""
                  className="size-10 rounded-full object-cover"
                />
              ) : (
                <User className="size-10 rounded-full bg-white p-2 text-neutral-500" aria-hidden />
              )}
              <div>
                <p className="font-semibold">{task.customerName}</p>
                {task.customerPhone && (
                  <a
                    href={`tel:${task.customerPhone}`}
                    className="text-neutral-500 hover:underline"
                  >
                    {task.customerPhone}
                  </a>
                )}
              </div>
              <p className="sm:ml-auto text-neutral-600">
                Order total: <strong>{formatCartMoney(task.totalAmount)}</strong>
              </p>
            </div>
            <p className="text-sm text-neutral-600">
              {task.cancelReason ||
                (task.orderStatus === "NoShow"
                  ? "Customer did not collect the order before the pickup deadline."
                  : "No cancellation reason provided.")}
            </p>
            <p className="text-xs text-neutral-500">
              {task.cancelledAt
                ? `Cancelled: ${checkoutTime(task.cancelledAt)}`
                : `Updated: ${checkoutTime(task.updatedAt)}`}
            </p>
          </div>
          <div className="p-3">
            <OrderResalePanel
              key={`${task.orderId}:${task.decision.canDecide}:${task.decision.items.map((i) => `${i.status}:${i.resaleQuantity}`).join()}`}
              shopId={shopId}
              orderId={task.orderId}
              revision={revision}
              readOnly={readOnly || shopSuspended}
              initialData={task.decision}
              onChanged={() => {
                refresh();
                onChanged?.();
              }}
            />
          </div>
        </article>
      ))}
      {data && data.totalPages > 1 && (
        <nav aria-label="Restock pagination" className="flex items-center justify-center gap-4">
          <button
            type="button"
            disabled={page <= 1 || updating}
            onClick={() => setPage((value) => value - 1)}
            className="min-h-11 rounded-lg border px-4 disabled:opacity-50"
          >
            Previous
          </button>
          <span className="text-sm">
            {page} / {data.totalPages}
          </span>
          <button
            type="button"
            disabled={page >= data.totalPages || updating}
            onClick={() => setPage((value) => value + 1)}
            className="min-h-11 rounded-lg border px-4 disabled:opacity-50"
          >
            Next
          </button>
        </nav>
      )}
    </div>
  );
}
