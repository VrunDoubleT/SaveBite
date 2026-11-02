import { Button, Card } from "@/shared/components/ui";
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
    <Card aria-label="Cancel order">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0 flex-1">
          <h3 className="font-semibold text-text-primary">Order cancellation</h3>
          <p className="mt-1 text-caption leading-relaxed text-text-secondary">
            {canCancel
              ? "Cancel before the pickup deadline. Payments and deposits are non-refundable."
              : reason}
          </p>
        </div>
        <Button
          variant="secondary"
          type="button"
          disabled={busy || !canCancel}
          onClick={() => setConfirming(true)}
          className="inline-flex min-h-10 w-full shrink-0 sm:w-auto items-center justify-center gap-2 rounded-md border border-danger-soft px-4 py-2 text-body font-semibold text-danger-strong hover:bg-danger-soft disabled:cursor-not-allowed disabled:border-border-default disabled:text-text-muted"
        >
          <XCircle aria-hidden="true" size={16} />
          Cancel order
        </Button>
      </div>
      {confirming && canCancel && (
        <div
          role="alert"
          className="mt-4 rounded-md border border-danger-soft bg-danger-soft p-4 text-body text-danger-strong"
        >
          <p className="font-bold">Cancel this order?</p>
          <p className="mt-2">
            Any payment or deposit already paid will not be refunded, including payments confirmed
            while cancellation is being processed. This action cannot be undone.
          </p>
          <div className="mt-3 flex flex-wrap gap-3">
            <Button
              variant="danger"
              loading={busy}
              type="button"
              disabled={busy}
              onClick={() => void onCancel()}
            >
              {busy ? "Cancelling…" : "Confirm cancellation"}
            </Button>
            <Button
              variant="secondary"
              type="button"
              disabled={busy}
              onClick={() => setConfirming(false)}
              className="rounded-md border border-danger-soft bg-bg-surface px-4 py-2"
            >
              Keep order
            </Button>
          </div>
        </div>
      )}
    </Card>
  );
}
