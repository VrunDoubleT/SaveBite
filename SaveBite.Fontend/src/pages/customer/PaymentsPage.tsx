import { paymentTypeLabel } from "@/features/payments/utils/paymentLabels";
import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { CreditCard, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { paymentApi } from "@/features/payments/api/paymentApi";
import type { CustomerPaymentPage } from "@/features/payments/types/paymentHistory.types";
import { PaymentStatusBadge } from "@/features/payments/components/PaymentStatusBadge";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { Pagination } from "@/shared/components/pagination";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

const statuses = ["All", "Pending", "Succeeded", "Failed", "Cancelled", "Expired"];

export function PaymentsPage() {
  const userId = useAuthStore((state) => state.user?.id);
  const [params, setParams] = useSearchParams();
  const requestedPage = Number(params.get("page") ?? 1);
  const page = Number.isSafeInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1;
  const status = statuses.includes(params.get("status") ?? "") ? params.get("status")! : "All";
  const [retry, setRetry] = useState(0);
  const requestKey = `${userId}:${page}:${status}:${retry}`;
  const [result, setResult] = useState<{ key: string; data?: CustomerPaymentPage; error?: string } | null>(null);
  useEffect(() => {
    const abort = new AbortController();
    void paymentApi.page(page, status, abort.signal).then((data) => {
      if (abort.signal.aborted) return;
      const lastPage = Math.max(1, data.totalPages);
      if (page > lastPage) {
        setParams((previous) => { const next = new URLSearchParams(previous); next.set("page", String(lastPage)); return next; }, { replace: true });
        return;
      }
      setResult({ key: requestKey, data });
    }).catch((error) => {
      if (!abort.signal.aborted) setResult({ key: requestKey, error: getApiErrorMessage(error) });
    });
    return () => abort.abort();
  }, [page, status, requestKey, setParams]);
  const current = result?.key === requestKey ? result : null;
  const data = current?.data;
  return (
    <div className="min-w-0 space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-neutral-500">Full payments, deposits and remaining payments, all in one place.</p>
        <label className="flex items-center gap-2 text-xs font-medium text-neutral-600">
          Payment status
          <select value={status} onChange={(e) => setParams((previous) => {
            const next = new URLSearchParams(previous); next.delete("page");
            if (e.target.value === "All") next.delete("status"); else next.set("status", e.target.value);
            return next;
          })} className="rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm">
            {statuses.map((value) => <option key={value} value={value}>{value === "Succeeded" ? "Paid" : value}</option>)}
          </select>
        </label>
      </div>
      {!current && <p role="status" className="py-8 text-sm text-neutral-500">Loading payments…</p>}
      {current?.error && <div role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{current.error}<button onClick={() => setRetry((value) => value + 1)} className="ml-3 underline">Retry</button></div>}
      {data?.items.length === 0 && <div className="rounded-xl border border-neutral-200 bg-white p-8 text-center text-sm text-neutral-500"><CreditCard className="mx-auto mb-2" />No payments found.</div>}
      {data?.items.map((payment) => (
        <article key={payment.id} className="rounded-xl border border-neutral-200 bg-white p-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              {payment.shopLogoUrl ? <img src={payment.shopLogoUrl} alt="" className="size-10 rounded-lg object-cover" /> : <Store className="size-10 rounded-lg bg-neutral-100 p-2 text-neutral-400" />}
              <div className="min-w-0"><p className="truncate text-sm font-semibold text-neutral-900">{payment.shopName}</p><p className="mt-1 text-xs text-neutral-500">Order {payment.orderCode}</p></div>
            </div>
            <PaymentStatusBadge status={payment.status} />
          </div>
          <div className="mt-4 flex flex-wrap items-end justify-between gap-4 border-t border-neutral-100 pt-3">
            <div><p className="text-sm font-medium">{paymentTypeLabel(payment.type)}</p><p className="mt-1 text-xs text-neutral-500">Created {checkoutTime(payment.createdAt)}</p>{payment.paidAt && <p className="mt-1 text-xs text-neutral-500">Paid {checkoutTime(payment.paidAt)}</p>}</div>
            <div className="flex flex-wrap items-end gap-4">
              <div className="text-right"><p className="text-xs text-neutral-500">Requested</p><p className="mt-1 text-sm font-semibold">{money(payment.amount)}</p></div>
              <div className="text-right"><p className="text-xs text-neutral-500">Collected</p><p className="mt-1 text-sm font-semibold text-emerald-700">{money(payment.collectedAmount)}</p></div>
              <Link to={`${APP_PATHS.ACCOUNT_PAYMENTS}/${payment.id}`} className="rounded-lg border border-emerald-200 px-3 py-2 text-xs font-semibold text-emerald-700 hover:bg-emerald-50">View details</Link>
            </div>
          </div>
        </article>
      ))}
      {data && data.totalPages > 0 && <Pagination currentPage={data.page} totalPages={data.totalPages} totalItems={data.totalItems} pageSize={data.pageSize} itemLabel="payments" size="sm" onPageChange={(nextPage) => setParams((previous) => {
        const next = new URLSearchParams(previous); next.set("page", String(nextPage)); return next;
      })} />}
    </div>
  );
}
