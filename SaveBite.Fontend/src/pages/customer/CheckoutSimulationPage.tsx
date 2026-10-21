import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { paymentApi } from "@/features/payments/api/paymentApi";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { getApiErrorMessage } from "@/shared/api";

export function CheckoutSimulationPage() {
  const [params] = useSearchParams();
  const orderId = params.get("orderId") ?? "";
  const paymentId = params.get("paymentId") ?? undefined;
  return <Simulation key={`${orderId}:${paymentId}`} orderId={orderId} paymentId={paymentId} />;
}
function Simulation({ orderId, paymentId }: { orderId: string; paymentId?: string }) {
  const navigate = useNavigate();
  const [order, setOrder] = useState<CheckoutOrder | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const controller = new AbortController();
    paymentApi
      .simulation(orderId, controller.signal, paymentId)
      .then((value) => {
        if (!controller.signal.aborted) setOrder(value);
      })
      .catch((cause) => {
        if (!controller.signal.aborted) setError(getApiErrorMessage(cause));
      });
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => {
      controller.abort();
      window.clearInterval(timer);
    };
  }, [orderId, paymentId]);
  const receipt = `/checkout?orderId=${encodeURIComponent(orderId)}`;
  const expired = !order?.paymentExpiresAt || Date.parse(order.paymentExpiresAt) <= now;
  const outdatedOrder = !paymentId && order?.status === "Pending" && order.paymentStatus === "Pending";
  const resolved =
    (paymentId
      ? !["Confirmed", "Preparing", "ReadyForPickup"].includes(order?.status ?? "")
      : order?.status !== "AwaitingPayment") || order?.paymentStatus !== "Pending";
  async function resolve(outcome: "paid" | "cancelled" | "expired") {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      await paymentApi.simulate(orderId, outcome, paymentId);
      navigate(receipt, { replace: true });
    } catch (cause) {
      setError(getApiErrorMessage(cause));
      setBusy(false);
    }
  }
  return (
    <main className="mx-auto max-w-xl px-4 py-10">
      <div className="rounded-2xl border border-amber-300 bg-amber-50 p-6">
        <p className="font-bold text-amber-800">DEVELOPMENT · SIMULATED PAYMENT</p>
        <h1 className="mt-3 text-2xl font-bold">Test payment</h1>
        <p className="mt-2 text-sm text-slate-700">
          No real money is transferred. Actions below update orders and stock in the connected
          database.
        </p>
      </div>
      {error && (
        <p role="alert" className="mt-4 rounded-xl bg-red-50 p-4 text-red-700">
          {error}
        </p>
      )}
      {!order && !error && <p className="mt-4">Loading orders…</p>}
      {order && (
        <div className="mt-4 rounded-2xl border border-slate-200 bg-white p-6">
          <p className="font-semibold">Order {order.orderCode}</p>
          <p className="mt-1">{order.shopName}</p>
          <p className="mt-2 text-sm text-slate-600">
            Order status: {order.status} · Payment status: {order.paymentStatus}
          </p>
          <p className="mt-4 text-sm text-slate-600">
            {paymentId
              ? "Remaining payment"
              : order.paymentMethod === "PayAtShop"
                ? "Online deposit"
                : "Full payment"}
          </p>
          <p className="text-3xl font-bold">{money(order.requiredOnlineAmount)}</p>
          <p className="mt-2 text-sm">Remaining at shop: {money(order.remainingAtShop)}</p>
          <p className="mt-2 text-sm">
            Payment deadline: {checkoutTime(order.paymentExpiresAt)} (GMT+7)
          </p>
          {expired || resolved ? (
            <p className="mt-4 text-amber-800">
              {expired
                ? "The payment deadline has passed. Create a new order to test payment again."
                : outdatedOrder
                  ? "This order uses an outdated checkout state. Restart the development API and reload this page."
                  : "This payment has already been processed or is unavailable for the current order status. Open order details to see the latest status."}
            </p>
          ) : (
            <div className="mt-6 grid gap-3">
              <button
                disabled={busy}
                onClick={() => void resolve("paid")}
                className="rounded-xl bg-emerald-600 p-3 font-semibold text-white disabled:opacity-50"
              >
                Payment successful
              </button>
              <button
                disabled={busy}
                onClick={() => void resolve("cancelled")}
                className="rounded-xl border border-red-300 p-3 text-red-700 disabled:opacity-50"
              >
                Cancel payment
              </button>
              <button
                disabled={busy}
                onClick={() => void resolve("expired")}
                className="rounded-xl border border-slate-300 p-3 disabled:opacity-50"
              >
                Simulate expiry now
              </button>
            </div>
          )}
          <p className="mt-4 text-xs text-slate-600">
            You can also close this page and wait for expiry to test automatic stock release.
          </p>
        </div>
      )}
      <Link to={receipt} className="mt-6 inline-block text-emerald-700 underline">
        View order details
      </Link>
    </main>
  );
}
