import { RefundBankSelect } from "@/features/refunds/components/RefundBankSelect";
import { useEffect, useState } from "react";
import { shopOrderApi } from "../api/shopOrderApi";
import type { OrderRefund } from "../types/shopOrder.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { getApiErrorMessage } from "@/shared/api";
export function CustomerRefundPanel({ orderId }: { orderId: string }) {
  const [refund, setRefund] = useState<OrderRefund | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [bankBin, setBankBin] = useState("");
  const [bankName, setBankName] = useState("");
  const [accountNumber, setAccountNumber] = useState("");
  const [accountHolder, setAccountHolder] = useState("");
  const [code, setCode] = useState("");
  useEffect(() => {
    const abort = new AbortController();
    void shopOrderApi
      .refund(orderId, abort.signal)
      .then(setRefund)
      .catch((e) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(e));
      });
    return () => abort.abort();
  }, [orderId]);
  async function submit(verify: boolean) {
    if (busy) return;
    setBusy(true);
    setError("");
    try {
      setRefund(
        await (verify
          ? shopOrderApi.verify(orderId, code)
          : shopOrderApi.destination(orderId, { bankBin, bankName, accountNumber, accountHolder })),
      );
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      setBusy(false);
    }
  }
  if (!refund)
    return error ? <p className="text-xs text-red-700">Refund information: {error}</p> : null;
  const editable = refund.status === "AwaitingCustomerDetails";
  return (
    <section className="rounded-xl border border-amber-200 bg-amber-50 p-4">
      <h2 className="text-sm font-bold">Refund · {money(refund.amount)}</h2>
      <p className="mt-1 text-xs text-neutral-600">{refund.reason}</p>
      {error && (
        <p role="alert" className="mt-3 text-xs text-red-700">
          {error}
        </p>
      )}
      {editable ? (
        <>
          <p className="mt-3 text-xs text-neutral-600">
            Provide your bank account and confirm it with the code sent to your account email. The
            shop cannot transfer the refund before confirmation.
          </p>
          <form
            className="mt-3 grid gap-3 sm:grid-cols-3"
            onSubmit={(e) => {
              e.preventDefault();
              void submit(false);
            }}
          >
            <RefundBankSelect value={bankBin} disabled={busy} onChange={(bin, name) => { setBankBin(bin); setBankName(name); }} />
            <label className="text-xs font-medium">
              Account number
              <input
                required
                inputMode="numeric"
                pattern="[0-9]+"
                maxLength={50}
                value={accountNumber}
                onChange={(e) => setAccountNumber(e.target.value)}
                className="mt-1 w-full rounded-lg border border-amber-200 bg-white p-2 text-sm"
              />
            </label>
            <label className="text-xs font-medium">
              Account holder
              <input
                required
                maxLength={150}
                value={accountHolder}
                onChange={(e) => setAccountHolder(e.target.value)}
                className="mt-1 w-full rounded-lg border border-amber-200 bg-white p-2 text-sm"
              />
            </label>
            <button
              disabled={busy || !bankBin}
              className="justify-self-start rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50 sm:col-span-3"
            >
              Send confirmation code
            </button>
          </form>
          {refund.confirmationExpiresAt && !refund.destinationConfirmedAt && (
            <form
              className="mt-4 space-y-2 border-t border-amber-200 pt-3"
              onSubmit={(e) => {
                e.preventDefault();
                void submit(true);
              }}
            >
              <p className="text-xs">
                Code sent to your email. Valid until {checkoutTime(refund.confirmationExpiresAt)}.
              </p>
              <label className="block text-xs font-medium">
                6-digit email code
                <input
                  required
                  inputMode="numeric"
                  pattern="[0-9]{6}"
                  maxLength={6}
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  className="ml-2 w-28 rounded-lg border border-amber-200 bg-white p-2 text-sm"
                />
              </label>
              <button
                disabled={busy || code.length !== 6}
                className="rounded-lg bg-emerald-600 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50"
              >
                Confirm refund destination
              </button>
            </form>
          )}
        </>
      ) : (
        <div className="mt-3 space-y-1 text-xs">
          <p className="font-semibold">
            {refund.status === "Transferred"
              ? "Refund transfer recorded"
              : "Bank account confirmed · Waiting for transfer"}
          </p>
          <p>
            {refund.bankName} · {refund.accountNumber} · {refund.accountHolder}
          </p>
          {refund.transferReference && <p>Transfer reference: {refund.transferReference}</p>}
        </div>
      )}
    </section>
  );
}
