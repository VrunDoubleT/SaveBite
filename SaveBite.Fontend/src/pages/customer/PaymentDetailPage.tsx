import { paymentTypeLabel, paymentMethodLabel } from "@/features/payments/utils/paymentLabels";
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { paymentApi } from "@/features/payments/api/paymentApi";
import type { CustomerPaymentDetail } from "@/features/payments/types/paymentHistory.types";
import { PaymentStatusBadge } from "@/features/payments/components/PaymentStatusBadge";
import { OrderPickupDeadlines } from "@/features/checkout/components/OrderPickupDeadlines";
import { OrderPickupCode } from "@/features/checkout/components/OrderPickupCode";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

export function PaymentDetailPage() {
  const { paymentId = "" } = useParams();
  const userId = useAuthStore((state) => state.user?.id);
  return <PaymentDetails key={`${userId}:${paymentId}`} paymentId={paymentId} />;
}
function PaymentDetails({ paymentId }: { paymentId: string }) {
  const [data, setData] = useState<CustomerPaymentDetail | null>(null);
  const [error, setError] = useState("");
  const [retry, setRetry] = useState(0);
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    const abort = new AbortController();
    void paymentApi.detail(paymentId, abort.signal).then((result) => {
      if (!abort.signal.aborted) { setData(result); setError(""); }
    }).catch((e) => { if (!abort.signal.aborted) setError(getApiErrorMessage(e)); });
    return () => abort.abort();
  }, [paymentId, retry]);
  useEffect(() => { const timer = window.setInterval(() => setNow(Date.now()), 1000); return () => clearInterval(timer); }, []);
  const payment = data?.payment, order = data?.order;
  return (
    <div className="min-w-0 space-y-4">
      <Link to={APP_PATHS.ACCOUNT_PAYMENTS} className="inline-flex items-center gap-1 text-sm text-emerald-700"><ArrowLeft size={16} />All payments</Link>
      {error && <div role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}<button onClick={() => setRetry((value) => value + 1)} className="ml-3 underline">Retry</button></div>}
      {!data && !error && <p role="status" className="py-8 text-sm text-neutral-500">Loading payment details…</p>}
      {payment && order && <>
        <section className="rounded-xl border border-neutral-200 bg-white p-4">
          <div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-base font-semibold">{paymentTypeLabel(payment.type)}</h2><PaymentStatusBadge status={payment.status} /></div>
          <p className="mt-2 break-all text-xs text-neutral-500">Payment ID: {payment.id}</p>
          <div className="mt-4 grid grid-cols-2 gap-4 border-y border-neutral-100 py-4">
            <div><p className="text-xs text-neutral-500">Payment amount</p><p className="mt-1 text-lg font-bold">{money(payment.amount)}</p></div>
            <div className="text-right"><p className="text-xs text-neutral-500">Amount collected</p><p className="mt-1 text-lg font-bold text-emerald-700">{money(payment.collectedAmount)}</p></div>
          </div>
          <dl className="mt-3 grid gap-3 text-sm sm:grid-cols-3">
            <div><dt className="text-xs text-neutral-500">Method</dt><dd className="mt-1">{paymentMethodLabel(payment.method)}</dd></div>
            <div><dt className="text-xs text-neutral-500">Created</dt><dd className="mt-1">{checkoutTime(payment.createdAt)}</dd></div>
            <div><dt className="text-xs text-neutral-500">Paid at</dt><dd className="mt-1">{payment.paidAt ? checkoutTime(payment.paidAt) : "Not recorded"}</dd></div>
          </dl>
        </section>
        <section className="rounded-xl border border-neutral-200 bg-white p-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div><p className="mb-1 text-xs font-medium text-neutral-500">Related order</p><OrderPickupCode code={order.orderCode} compact /></div>
            <div className="text-right"><p className="text-xs text-neutral-500">Order status</p><p className="mt-1 text-sm font-semibold">{order.status.replace(/([a-z])([A-Z])/g, "$1 $2")}</p><Link to={`/orders/${order.id}`} className="mt-2 inline-block text-xs font-semibold text-emerald-700 underline">View order details</Link></div>
          </div>
          <div className="my-4 flex items-center gap-3 border-y border-neutral-100 py-3">
            {order.shopLogoUrl ? <img src={order.shopLogoUrl} alt="" className="size-11 rounded-lg object-cover" /> : <Store className="size-11 rounded-lg bg-neutral-100 p-2 text-neutral-400" />}
            <div className="min-w-0"><p className="text-sm font-semibold">{order.shopName}</p><p className="mt-1 text-xs text-neutral-500">{order.shopAddress}</p></div>
          </div>
          <div className="space-y-3">{order.items.map((item) => <div key={item.flashDealVariantId} className="flex items-center gap-3">
            {item.imageUrl ? <img src={item.imageUrl} alt="" className="size-12 shrink-0 rounded-lg object-cover" /> : <div className="size-12 shrink-0 rounded-lg bg-neutral-100" />}
            <div className="min-w-0 flex-1"><p className="text-sm font-medium">{item.productName}</p><p className="mt-1 text-xs text-neutral-500">{item.variantName} · Qty {item.quantity} × {money(item.unitPrice)}</p></div>
            <p className="text-sm font-semibold">{money(item.subtotal)}</p>
          </div>)}</div>
          <div className="my-4 grid grid-cols-3 gap-2 border-y border-neutral-100 py-3 text-right">
            {[["Order total", order.totalAmount], ["Paid for order", order.paidAmount ?? 0], ["Still due", order.outstandingAmount ?? 0]].map(([label, value]) => <div key={label}><p className="text-xs text-neutral-500">{label}</p><p className="mt-1 text-sm font-bold">{money(Number(value))}</p></div>)}
          </div>
          <OrderPickupDeadlines order={order} now={now} compact />
        </section>
      </>}
    </div>
  );
}
