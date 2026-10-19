import { useState } from "react";
import { XCircle } from "lucide-react";
import type { CheckoutOrder } from "../types/checkout.types";

export function CancelOrderAction({
  order,
  now,
  busy,
  onCancel,
}: {
  order: CheckoutOrder;
  now: number;
  busy: boolean;
  onCancel: () => Promise<void>;
}) {
  const [confirming, setConfirming] = useState(false);
  const beforeDeadline = !!order.hardDeadlineAt && Date.parse(order.hardDeadlineAt) > now;
  const canCancel = order.canCancel === true && beforeDeadline;
  const reason =
    order.cancellationUnavailableReason ||
    (!beforeDeadline
      ? "Cancellation is unavailable at or after the pickup deadline."
      : "Cancellation is unavailable for this order. Refresh your order details to check again.");
  return (
    <section aria-label="Cancel order" className="rounded-xl border border-red-100 bg-white p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0 flex-1">
          <h3 className="font-semibold text-neutral-900">Order cancellation</h3>
          <p className="mt-1 text-xs leading-relaxed text-neutral-500">
            {canCancel
              ? "Cancel before the pickup deadline. Payments and deposits are non-refundable."
              : reason}
          </p>
        </div>
        <button
          type="button"
          disabled={busy || !canCancel}
          onClick={() => setConfirming(true)}
          className="inline-flex min-h-10 w-full shrink-0 sm:w-auto items-center justify-center gap-2 rounded-lg border border-red-300 px-4 py-2 text-sm font-semibold text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:border-neutral-200 disabled:text-neutral-400"
        >
          <XCircle size={16} />
          Cancel order
        </button>
      </div>
      {confirming && canCancel && (
        <div
          role="alert"
          className="mt-4 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-950"
        >
          <p className="font-bold">Cancel this order?</p>
          <p className="mt-2">
            Any payment or deposit already paid will not be refunded, including payments confirmed
            while cancellation is being processed. This action cannot be undone.
          </p>
          <div className="mt-3 flex flex-wrap gap-3">
            <button
              type="button"
              disabled={busy}
              onClick={() => void onCancel()}
              className="rounded-lg bg-red-600 px-4 py-2 font-semibold text-white disabled:opacity-50"
            >
              {busy ? "Cancelling…" : "Confirm cancellation"}
            </button>
            <button
              type="button"
              disabled={busy}
              onClick={() => setConfirming(false)}
              className="rounded-lg border border-red-200 bg-white px-4 py-2"
            >
              Keep order
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
