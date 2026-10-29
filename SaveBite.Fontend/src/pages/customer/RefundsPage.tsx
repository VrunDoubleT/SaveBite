import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { RotateCcw, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { getApiErrorMessage } from "@/shared/api";
import { Pagination } from "@/shared/components/pagination";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { refundApi } from "@/features/refunds/api/refundApi";
import { RefundStatusBadge } from "@/features/refunds/components/RefundStatusBadge";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import type { RefundPage } from "@/features/refunds/types/refund.types";
const statuses: Record<string, string> = { All: "All statuses", Overdue: "Overdue · Admin required", AwaitingCustomerDetails: "Bank details required", PendingVerification: "Waiting for shop review", AwaitingTransfer: "Waiting for transfer", Rejected: "Declined", Transferred: "Transferred", Completed: "Completed", Withdrawn: "Withdrawn" };
export function RefundsPage() {
  const [params, setParams] = useSearchParams();
  const rawPage = Number(params.get("page") ?? 1);
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1;
  const status = Object.hasOwn(statuses, params.get("status") ?? "") ? params.get("status")! : "All";
  const type = ["CustomerRequest", "ShopCancellation"].includes(params.get("type") ?? "") ? params.get("type")! : "All";
  const [revision, setRevision] = useState(0);
  const [result, setResult] = useState<{ key: string; data?: RefundPage; error?: string } | null>(null);
  const realtime = useRealtimeSubscriptions();
  const key = `${page}:${status}:${type}:${revision}`;
  useEffect(() => {
    const abort = new AbortController();
    void refundApi.page(page, status, type, abort.signal).then((data) => {
      if (abort.signal.aborted) return;
      if (page > Math.max(1, data.totalPages)) { setParams((old) => { const next = new URLSearchParams(old); next.set("page", String(Math.max(1, data.totalPages))); return next; }, { replace: true }); return; }
      setResult({ key, data });
    }).catch((e) => { if (!abort.signal.aborted) setResult({ key, error: getApiErrorMessage(e) }); });
    return () => abort.abort();
  }, [key, page, status, type, setParams]);
  useEffect(() => {
    const off = realtime.on(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, (event) => {
      const data = event.data as { reason?: string } | null;
      if (["refund-changed", "reject", "confirmation-expired"].includes(data?.reason ?? "")) setRevision((value) => value + 1);
    });
    const connected = realtime.onConnected((reconnected) => { if (reconnected) setRevision((value) => value + 1); });
    return () => { off(); connected(); };
  }, [realtime]);
  const current = result?.key === key ? result : null;
  function filter(name: string, value: string) { setParams((old) => { const next = new URLSearchParams(old); next.delete("page"); if (value === "All") next.delete(name); else next.set(name, value); return next; }); }
  return <div className="min-w-0 space-y-4">
    <div className="flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-neutral-500">Manage your requests and refunds required after a shop cancellation.</p><Link to={APP_PATHS.ACCOUNT_ORDERS} className="text-xs font-medium text-emerald-700 hover:underline">Request a refund from order details</Link></div>
    <div className="flex flex-wrap gap-3">
      <label className="text-xs font-medium text-neutral-600">Refund type<select value={type} onChange={(e) => filter("type", e.target.value)} className="ml-2 rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm"><option value="All">All refunds</option><option value="CustomerRequest">My requests</option><option value="ShopCancellation">Shop cancellations</option></select></label>
      <label className="text-xs font-medium text-neutral-600">Status<select value={status} onChange={(e) => filter("status", e.target.value)} className="ml-2 rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm">{Object.entries(statuses).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
    </div>
    {current?.error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{current.error}<button type="button" className="ml-3 underline" onClick={() => setRevision((value) => value + 1)}>Retry</button></p>}
    {!current && <p role="status" className="py-6 text-sm text-neutral-500">Loading refunds…</p>}
    {current?.data?.items.length === 0 && <div className="rounded-xl border border-neutral-200 bg-white p-8 text-center text-sm text-neutral-500"><RotateCcw className="mx-auto mb-2" size={24} />No refunds found.</div>}
    {current?.data?.items.map((refund) => <section key={refund.id} className="rounded-xl border border-neutral-200 bg-white p-4"><header className="mb-3 flex flex-wrap items-start justify-between gap-2"><div className="flex min-w-0 items-center gap-3"><RefundShopAvatar src={refund.shopLogoUrl} name={refund.shopName} /><div className="min-w-0"><Link to={APP_PATHS.SHOP_DETAIL.replace(":id", refund.shopId)} className="break-words text-sm font-semibold hover:text-emerald-700 hover:underline focus-visible:underline">{refund.shopName}</Link><p className="mt-1 text-xs text-neutral-500">Order <Link to={APP_PATHS.ORDER_DETAIL.replace(":orderId", refund.orderId)} className="font-medium text-emerald-700 hover:underline focus-visible:underline">{refund.orderCode}</Link></p></div></div><Link to={APP_PATHS.REFUND_DETAIL.replace(":refundId", refund.id)} className="rounded-lg border border-emerald-200 px-3 py-2 text-xs font-medium text-emerald-700">View refund details</Link></header><div className="flex flex-wrap items-center justify-between gap-3 border-t border-neutral-100 pt-3"><div><p className="text-base font-bold text-neutral-900">{money(refund.amount)}</p><p className="mt-1 text-xs text-neutral-500">{refund.type === "CustomerRequest" ? "Your request" : "Required shop refund"} · {checkoutTime(refund.createdAt)}</p></div><RefundStatusBadge status={refund.status} overdue={refund.isOverdue} /></div></section>)}
    {current?.data && current.data.totalPages > 0 && <Pagination currentPage={current.data.page} totalPages={current.data.totalPages} totalItems={current.data.totalItems} pageSize={current.data.pageSize} itemLabel="refunds" size="sm" onPageChange={(value) => setParams((old) => { const next = new URLSearchParams(old); next.set("page", String(value)); return next; })} />}
  </div>;
}

function RefundShopAvatar({ src, name }: { src?: string; name: string }) {
  const [failedSrc, setFailedSrc] = useState<string | null>(null);
  return <span className="flex size-11 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-neutral-200 bg-neutral-50 text-neutral-400">
    {src && src !== failedSrc ? <img src={src} alt={`${name} logo`} loading="lazy" onError={() => setFailedSrc(src)} className="size-full object-cover" /> : <Store size={22} aria-hidden="true" />}
  </span>;
}
