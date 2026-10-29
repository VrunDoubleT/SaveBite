import { RefundCustomerInfo } from "@/features/refunds/components/RefundCustomerInfo";
import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { RotateCcw, CircleAlert, Clock3 } from "lucide-react";
import { refundApi } from "@/features/refunds/api/refundApi";
import type { RefundPage } from "@/features/refunds/types/refund.types";
import { RefundStatusBadge } from "@/features/refunds/components/RefundStatusBadge";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { Pagination } from "@/shared/components/pagination";
import { getApiErrorMessage } from "@/shared/api";
const statuses: Record<string, string> = {
  All: "All refunds",

  PendingVerification: "Needs review",
  AwaitingCustomerDetails: "Waiting for customer",
  AwaitingTransfer: "Ready to transfer",
  Transferred: "Transferred",
  Rejected: "Declined",
  Withdrawn: "Withdrawn",
};
export function ShopRefundsPage() {
  const [params, setParams] = useSearchParams();
  const [data, setData] = useState<RefundPage | null>(null);
  const [error, setError] = useState("");
  const [revision, setRevision] = useState(0);
  const realtime = useRealtimeSubscriptions();
  const rawPage = Number(params.get("page") ?? 1);
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1;
  const status = Object.hasOwn(statuses, params.get("status") ?? "")
    ? params.get("status")!
    : "All";
  useEffect(() => {
    const abort = new AbortController();
    void refundApi
      .shopPage(page, status, abort.signal)
      .then((result) => {
        if (abort.signal.aborted) return;
        if (page > Math.max(1, result.totalPages)) {
          setParams(
            (old) => {
              const next = new URLSearchParams(old);
              next.set("page", String(Math.max(1, result.totalPages)));
              return next;
            },
            { replace: true },
          );
          return;
        }
        setData(result);
        setError("");
      })
      .catch((e) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(e));
      });
    return () => abort.abort();
  }, [page, status, revision, setParams]);
  useEffect(() => {
    const off = realtime.on(REALTIME_EVENTS.SHOP_ORDERS_CHANGED, (event) => {
      const change = event.data as { shopId?: string; reason?: string } | null;
      if (["refund-changed", "reject", "confirmation-expired"].includes(change?.reason ?? ""))
        setRevision((value) => value + 1);
    });
    const connected = realtime.onConnected((again) => {
      if (again) setRevision((value) => value + 1);
    });
    return () => {
      off();
      connected();
    };
  }, [realtime]);
  return (
    <div className="w-full min-w-0 space-y-4">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-neutral-900">Refund payments</h1>
          <p className="mt-1 text-sm text-neutral-500">
            Track refund obligations and requests. Open a refund to review or record payment.
          </p>
        </div>
        <label className="text-xs font-medium text-neutral-600">
          Show
          <select
            value={status}
            onChange={(event) => setParams({ status: event.target.value })}
            className="ml-2 rounded-lg border border-neutral-200 px-3 py-2 text-sm"
          >
            {Object.entries(statuses).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </label>
      </header>
      {error && (
        <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {error}
          <button className="ml-2 underline" onClick={() => setRevision((value) => value + 1)}>
            Retry
          </button>
        </p>
      )}
      {!data && !error && <p className="py-8 text-sm text-neutral-500">Loading refunds…</p>}
      {data?.items.length === 0 && (
        <div className="rounded-xl border border-neutral-200 bg-white p-8 text-center text-sm text-neutral-500">
          <RotateCcw className="mx-auto mb-3" size={28} />
          No refunds in this category.
        </div>
      )}
      <div className="space-y-3">
        {data?.items.map((refund) => (
          <article
            key={refund.id}
            className={`rounded-xl border bg-white p-4 sm:p-5 ${refund.requiresShopAction ? "border-amber-300 border-l-4 border-l-amber-500" : "border-neutral-200"}`}
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <p className="text-sm font-semibold text-neutral-900">Order {refund.orderCode}</p>
                  <span
                    className={`rounded-md px-2 py-1 text-[11px] font-medium ${refund.type === "ShopCancellation" ? "bg-amber-50 text-amber-800" : "bg-sky-50 text-sky-800"}`}
                  >
                    {refund.type === "ShopCancellation" ? "Required refund" : "Customer request"}
                  </span>
                </div>
                <p className="mt-1.5 text-xs text-neutral-500">
                  Created {checkoutTime(refund.createdAt)} · GMT+7
                </p>
              </div>
              <RefundStatusBadge status={refund.status} overdue={refund.isOverdue} />
            </div>
            <div className="mt-4">
              <RefundCustomerInfo refund={refund} linkPhone={false} />
            </div>
            {refund.requiresShopAction ? (
              <div className="mt-4 flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-amber-900">
                <CircleAlert size={17} className="mt-0.5 shrink-0" />
                <div>
                  <p className="text-sm font-semibold">
                    {refund.shopAction === "ReviewRequired"
                      ? "Action required · Review this request"
                      : "Action required · Complete the refund transfer"}
                  </p>
                  <p className="mt-1 text-xs">
                    {refund.shopAction === "ReviewRequired"
                      ? "Approve the full refund or decline with a reason before the deadline."
                      : "Transfer the balance and record the bank reference with proof before the deadline."}
                  </p>
                  {refund.reviewDeadlineAt && (
                    <p className="mt-2 flex items-center gap-1 text-xs font-semibold">
                      <Clock3 size={13} />
                      Due {checkoutTime(refund.reviewDeadlineAt)} · GMT+7
                    </p>
                  )}
                </div>
              </div>
            ) : (
              <p className="mt-3 text-xs font-medium text-neutral-500">
                {refund.shopAction === "WaitingCustomer"
                  ? "Waiting for customer · No shop action yet"
                  : refund.shopAction === "DeadlinePassed"
                    ? "Deadline passed · Awaiting automatic closure"
                    : "Closed · No further action required"}
              </p>
            )}
            <p className="mt-3 line-clamp-1 text-sm text-neutral-600">{refund.reason}</p>
            <div className="mt-4 flex flex-wrap items-end justify-between gap-3 border-t border-neutral-100 pt-3">
              <div>
                <p className="text-xs text-neutral-500">Refund amount</p>
                <p className="mt-1 text-base font-bold text-neutral-900">{money(refund.amount)}</p>
              </div>
              <Link
                to={`/store-owner/refunds/${refund.id}`}
                className={`rounded-lg px-3 py-2 text-xs font-semibold ${refund.requiresShopAction ? "bg-primary-700 text-white hover:bg-primary-800" : "border border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-50"}`}
              >
                {refund.requiresShopAction
                  ? refund.shopAction === "ReviewRequired"
                    ? "Review request"
                    : "Record transfer"
                  : "View refund"}
              </Link>
            </div>
          </article>
        ))}
      </div>
      {data && data.totalPages > 0 && (
        <Pagination
          currentPage={data.page}
          totalPages={data.totalPages}
          totalItems={data.totalItems}
          pageSize={data.pageSize}
          itemLabel="refunds"
          size="sm"
          onPageChange={(nextPage) =>
            setParams((old) => {
              const next = new URLSearchParams(old);
              next.set("page", String(nextPage));
              return next;
            })
          }
        />
      )}
    </div>
  );
}
