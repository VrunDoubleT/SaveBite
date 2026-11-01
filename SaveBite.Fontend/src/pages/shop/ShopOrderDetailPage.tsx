import { ShopOrderRefunds } from "@/features/refunds/components/ShopOrderRefunds";
import { paymentMethodLabel } from "@/features/payments/utils/paymentLabels";
import { useShopOrderEventsStore } from "@/features/shop-orders/stores/shopOrderEventsStore";
import { ConfirmationCountdown } from "@/features/shop-orders/components/ConfirmationCountdown";
import { HistoryActor } from "@/features/shop-orders/components/HistoryActor";
import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { shopOrderApi } from "@/features/shop-orders/api/shopOrderApi";
import type { ShopOrder } from "@/features/shop-orders/types/shopOrder.types";
import { OrderStatusPanel } from "@/features/checkout/components/OrderStatusPanel";
import { OrderPickupDeadlines } from "@/features/checkout/components/OrderPickupDeadlines";
import { PaymentQr } from "@/features/shop-orders/components/PaymentQr";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";
export function ShopOrderDetailPage() {
  const { shopId, orderId } = useParams();
  const userId = useAuthStore((state) => state.user?.id);
  return <ShopOrderDetailPageContent key={`${userId}:${shopId}:${orderId}`} />;
}
function ShopOrderDetailPageContent() {
  const { shopId: staffShopId, orderId = "" } = useParams();
  const [shopId, setShopId] = useState(staffShopId ?? "");
  const orderRevision = useShopOrderEventsStore((state) => state.orderRevisions[`${shopId}:${orderId}`] ?? 0);
  const reconnectRevision = useShopOrderEventsStore((state) => state.reconnectRevision);
  const [data, setData] = useState<ShopOrder | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [reason, setReason] = useState("");
  const [rejecting, setRejecting] = useState(false);
  const [handover, setHandover] = useState(false);
  const [cashReceived, setCashReceived] = useState(false);
  const [now, setNow] = useState(Date.now);
  const inFlight = useRef(false);
  const revision = useRef(0);
  const base = staffShopId ? `/staff/shops/${staffShopId}/orders` : "/store-owner/orders";
  useEffect(() => {
    const abort = new AbortController();
    if (!staffShopId)
      void shopOrderApi
        .ownerShop(abort.signal)
        .then(setShopId)
        .catch((e) => {
          if (!abort.signal.aborted) setError(getApiErrorMessage(e));
        });
    return () => abort.abort();
  }, [staffShopId]);
  // Re-read the authoritative order after a matching shop event or reconnect, including events received during an action.
  useEffect(() => {
    if (!shopId || busy) return;
    const abort = new AbortController();
    const current = ++revision.current;
    void shopOrderApi.detail(shopId, orderId, abort.signal)
      .then((next) => {
        if (!abort.signal.aborted && current === revision.current) {
          setData(next);
          setError("");
          if (next.order.status !== "Pending") setRejecting(false);
          if (next.order.status !== "ReadyForPickup") setHandover(false);
          if (next.order.status !== "ReadyForPickup" || (next.order.outstandingAmount ?? 0) <= 0) setCashReceived(false);
        }
      })
      .catch((e) => {
        if (!abort.signal.aborted && current === revision.current) setError(getApiErrorMessage(e));
      });
    return () => abort.abort();
  }, [shopId, orderId, orderRevision, reconnectRevision, busy]);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  async function act(action: string, body?: object) {
    if (inFlight.current) return;
    inFlight.current = true;
    revision.current++;
    setBusy(true);
    setError("");
    try {
      setData(await shopOrderApi.action(shopId, orderId, action, body));
      setRejecting(false);
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }
  const o = data?.order;
  async function simulateRemaining(outcome: "paid" | "cancelled" | "expired") {
    if (inFlight.current || !remaining) return;
    inFlight.current = true;
    revision.current++;
    setBusy(true);
    setError("");
    try {
      await shopOrderApi.simulateRemaining(shopId, orderId, remaining.id, outcome);
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      inFlight.current = false;
      // Leaving the busy state reloads detail, even if the realtime event arrived during this request.
      setBusy(false);
    }
  }
  const active =
    !!o &&
    !["Completed", "Cancelled", "Rejected", "Expired", "NoShow"].includes(o.status) &&
    !!o.hardDeadlineAt &&
    Date.parse(o.hardDeadlineAt) > now;
  const verified = o?.paymentStatus === "Succeeded" && !o.paymentReviewRequired;
  const remainingPayments = data?.payments.filter((p) => p.type === "Remaining") ?? [];
  const remaining = remainingPayments.find((p) => p.status === "Succeeded")
    ?? remainingPayments.find((p) => p.status === "Pending")
    ?? remainingPayments.at(-1);
  const button =
    "rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:bg-neutral-200 disabled:text-neutral-500";
  return (
    <div className="min-w-0 space-y-4">
      <Link to={base} className="text-sm font-medium text-emerald-700">
        ← Shop orders
      </Link>
      {error && (
        <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
          {error}
        </p>
      )}
      {!o || !data ? (
        <p className="text-sm text-neutral-500">
          {error ? "Unable to load order." : "Loading order…"}
        </p>
      ) : (
        <>
          <header className="rounded-xl border border-neutral-200 bg-white p-4">
            <div className="flex flex-wrap justify-between gap-3">
              <div>
                <p className="text-xs text-neutral-500">Pickup code</p>
                <h1 className="mt-1 break-all text-lg font-bold">{o.orderCode}</h1>
                <p className="mt-1 text-xs text-neutral-500">Ordered {checkoutTime(o.createdAt)}</p>
              </div>
              <div className="text-sm">
                <p className="font-semibold">{data.customerName}</p>
                <p className="mt-1 text-neutral-500">{data.customerPhone || "No phone provided"}</p>
              </div>
            </div>
            <div className="mt-4">
              <OrderStatusPanel order={o} compact />
            </div>
          </header>
          <div className="grid items-start gap-4 xl:grid-cols-[minmax(0,1fr)_320px]">
            <div className="min-w-0 space-y-4">
              <section className="rounded-xl border border-neutral-200 bg-white p-4">
                <h2 className="text-sm font-bold">Purchased items</h2>
                <div className="mt-3 divide-y divide-neutral-100">
                  {o.items.map((item) => (
                    <div key={item.flashDealVariantId} className="flex gap-3 py-3">
                      {item.imageUrl ? (
                        <img
                          src={item.imageUrl}
                          alt=""
                          className="size-14 shrink-0 rounded-lg object-cover"
                        />
                      ) : (
                        <div className="size-14 shrink-0 rounded-lg bg-neutral-100" />
                      )}
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-semibold">{item.productName}</p>
                        <p className="mt-1 text-xs text-neutral-500">
                          {item.variantName} · {money(item.unitPrice)} × {item.quantity}
                        </p>
                      </div>
                      <p className="text-sm font-semibold">{money(item.subtotal)}</p>
                    </div>
                  ))}
                </div>
                <div className="grid grid-cols-3 gap-2 border-t border-neutral-100 pt-3 text-right">
                  {[
                    ["Order total", o.totalAmount],
                    ["Paid", o.paidAmount ?? 0],
                    ["Amount due", o.outstandingAmount ?? 0],
                  ].map(([label, value]) => (
                    <div key={label}>
                      <p className="text-xs text-neutral-500">{label}</p>
                      <p className="mt-1 text-sm font-bold">{money(Number(value))}</p>
                    </div>
                  ))}
                </div>
              </section>
              <section className="rounded-xl border border-neutral-200 bg-white p-4">
                <h2 className="mb-3 text-sm font-bold">Pickup deadlines</h2>
                <OrderPickupDeadlines order={o} now={now} compact />
                <p className="mt-3 text-xs text-neutral-500">
                  The hard deadline cannot be extended. Ready orders not collected in time become No
                  show.
                </p>
              </section>
              <section className="rounded-xl border border-neutral-200 bg-white p-4">
                <h2 className="text-sm font-bold">Order history</h2>
                <ol className="mt-3 space-y-3">
                  {data.history.map((event, i) => (
                    <li key={i} className="border-l-2 border-emerald-200 pl-3">
                      <div className="flex flex-wrap justify-between gap-2 text-xs">
                        <strong>{event.toStatus}</strong>
                        <time className="text-neutral-400">{checkoutTime(event.createdAt)}</time>
                      </div>
                      <HistoryActor
                        actorId={event.actorId}
                        actorRole={event.actorRole}
                        actorName={event.actorName}
                      />
                      <p className="mt-1 text-xs leading-relaxed text-neutral-500">{event.note}</p>
                    </li>
                  ))}
                </ol>
              </section>
            </div>
            <aside className="min-w-0 space-y-4">
              <section className="rounded-xl border border-neutral-200 bg-white p-4">
                <h2 className="text-sm font-bold">Order actions</h2>
                {data.confirmationDeadlineAt && (
                  <ConfirmationCountdown deadline={data.confirmationDeadlineAt} now={now} />
                )}
                <div className="mt-3 flex flex-col gap-2">
                  {o.status === "Pending" && (
                    <>
                      <button
                        disabled={
                          busy ||
                          !active ||
                          !verified ||
                          !data.confirmationDeadlineAt ||
                          Date.parse(data.confirmationDeadlineAt) <= now
                        }
                        onClick={() => void act("confirm")}
                        className={button}
                      >
                        Confirm order
                      </button>
                      {!verified && (
                        <p className="text-xs text-neutral-500">
                          Waiting for verified payment/deposit.
                        </p>
                      )}
                      <button
                        disabled={busy || !active}
                        onClick={() => setRejecting(!rejecting)}
                        className="rounded-lg border border-red-200 px-4 py-2 text-sm font-semibold text-red-700 disabled:opacity-50"
                      >
                        Reject order
                      </button>
                    </>
                  )}
                  {active && o.status === "Confirmed" && (
                    <button
                      disabled={busy || !verified}
                      onClick={() => void act("preparation", { status: "Preparing" })}
                      className={button}
                    >
                      Start preparing
                    </button>
                  )}
                  {active && o.status === "Preparing" && (
                    <button
                      disabled={busy || !verified}
                      onClick={() => void act("preparation", { status: "ReadyForPickup" })}
                      className={button}
                    >
                      Mark ready for pickup
                    </button>
                  )}
                  {active && o.status === "ReadyForPickup" && (
                    <>
                      <label className="flex items-start gap-2 text-xs text-neutral-600">
                        <input
                          type="checkbox"
                          checked={handover}
                          onChange={(e) => setHandover(e.target.checked)}
                          className="mt-0.5 accent-emerald-600"
                        />
                        I have handed the goods to the customer.
                      </label>
                      {o.paymentMethod === "PayAtShop" && (o.outstandingAmount ?? 0) > 0 && (
                        <label className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-900">
                          <input type="checkbox" checked={cashReceived} onChange={(e) => setCashReceived(e.target.checked)}
                            disabled={busy || (remaining?.collectedAmount ?? 0) > 0} className="mt-0.5 accent-emerald-600" />
                          <span>I have received <strong>{money(o.outstandingAmount ?? 0)}</strong> in cash from the customer.</span>
                        </label>
                      )}
                      <button
                        disabled={busy || !verified || ((o.outstandingAmount ?? 1) > 0 && !cashReceived) || !handover}
                        onClick={() => void act("complete", { cashReceived })}
                        className={button}
                      >
                        Complete order
                      </button>
                      {(o.outstandingAmount ?? 0) > 0 && (
                        <p className="text-xs text-amber-800">
                          Collect the balance by online QR, or confirm cash receipt above before completing.
                        </p>
                      )}
                    </>
                  )}
                  {!active && (
                    <p className="text-xs text-neutral-500">
                      No further fulfillment actions are available.
                    </p>
                  )}
                </div>
                {rejecting && (
                  <form
                    onSubmit={(e) => {
                      e.preventDefault();
                      void act("reject", { reason });
                    }}
                    className="mt-3 space-y-2 border-t border-neutral-100 pt-3"
                  >
                    <label className="block text-xs font-semibold">
                      Reason for rejection
                      <textarea
                        required
                        maxLength={1000}
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        className="mt-1 min-h-20 w-full rounded-lg border border-neutral-200 p-2 text-sm"
                      />
                    </label>
                    <p className="text-xs text-neutral-500">
                      This will reject the order, release its stock and record a refund request for any collected payment. No refund transfer will be made automatically.
                    </p>
                    <button
                      disabled={busy || !reason.trim()}
                      className="rounded-lg bg-red-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50"
                    >
                      Confirm rejection
                    </button>
                  </form>
                )}
              </section>
              <section className="rounded-xl border border-neutral-200 bg-white p-4">
                <h2 className="text-sm font-bold">Payments</h2>
                <p className="mt-1 text-xs text-neutral-500">
                  {o.paymentMethod === "PayAtShop"
                    ? "Deposit + remaining payment"
                    : "Full online payment"}
                </p>
                <div className="my-3 space-y-3">
                  {data.payments.map((p) => (
                    <div key={p.id} className="flex justify-between gap-2 text-xs">
                      <div>
                        <p className="font-semibold">{p.type}</p>
                        <p className="mt-1 text-neutral-500">
                          {paymentMethodLabel(p.method)} · {p.status}
                          {p.paidAt ? ` · ${checkoutTime(p.paidAt)}` : ""}
                        </p>
                      </div>
                      <strong>{money(p.amount)}</strong>
                    </div>
                  ))}
                  {o.paymentMethod === "PayAtShop" && !remaining && (
                    <div className="flex justify-between gap-2 border-t border-neutral-100 pt-3 text-xs">
                      <div>
                        <p className="font-semibold">Remaining</p>
                        <p className="mt-1 text-neutral-500">
                          {o.remainingAtShop > 0 ? "Not collected" : "No payment due"}
                        </p>
                      </div>
                      <strong>{money(o.remainingAtShop)}</strong>
                    </div>
                  )}
                </div>
                {active &&
                  ["Confirmed", "Preparing", "ReadyForPickup"].includes(o.status) &&
                  o.paymentMethod === "PayAtShop" &&
                  (o.outstandingAmount ?? 0) > 0 && (
                    <button
                      disabled={busy || cashReceived}
                      className={`${button} w-full`}
                      onClick={() => void act("remaining-payment")}
                    >
                      {remaining?.checkoutUrl
                        ? "Show remaining payment QR"
                        : remaining && ["Cancelled", "Expired", "Failed"].includes(remaining.status)
                          ? "Regenerate remaining payment QR"
                          : "Generate remaining payment QR"}
                    </button>
                  )}
                {active && remaining?.status === "Pending" && remaining.checkoutUrl && (
                  <div className="mt-3">
                    <PaymentQr
                      key={remaining.id}
                      value={remaining.qrCode}
                      url={remaining.checkoutUrl}
                    />
                    {remaining.checkoutUrl.startsWith("/checkout/simulation?") && (
                      <div className="mt-3 space-y-2 rounded-lg border border-amber-200 bg-amber-50 p-3">
                        <p className="text-xs font-bold text-amber-800">DEVELOPMENT · SIMULATED REMAINING PAYMENT</p>
                        <p className="text-xs text-amber-900">No real money is transferred. Success updates payment and notifies connected order pages.</p>
                        <div className="flex flex-wrap gap-2">
                          <button disabled={busy || cashReceived} onClick={() => void simulateRemaining("paid")} className={button}>Simulate payment success</button>
                          <button disabled={busy || cashReceived} onClick={() => void simulateRemaining("cancelled")} className="rounded-lg border border-amber-300 px-3 py-2 text-xs disabled:opacity-50">Simulate cancellation</button>
                          <button disabled={busy || cashReceived} onClick={() => void simulateRemaining("expired")} className="rounded-lg border border-amber-300 px-3 py-2 text-xs disabled:opacity-50">Simulate expiry</button>
                        </div>
                      </div>
                    )}
                  </div>
                )}
                {o.paymentReviewRequired && (
                  <p className="mt-3 rounded-lg bg-red-50 p-3 text-xs text-red-700">
                    Payment requires review. Completion is blocked.
                  </p>
                )}
              </section>

            </aside>
          </div>
          {data.isOwner && <ShopOrderRefunds shopId={shopId} orderId={orderId} />}
        </>
      )}
    </div>
  );
}
