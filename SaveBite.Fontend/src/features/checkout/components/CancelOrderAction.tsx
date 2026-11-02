import { Button, Modal } from "@/shared/components/ui";
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
  const close = () => {
    if (!busy) setConfirming(false);
  };
  return (
    <>
      <Button
        variant="secondary"
        disabled={busy}
        onClick={() => setConfirming(true)}
        aria-haspopup="dialog"
        className="min-h-11 !text-danger-strong hover:!bg-danger-soft"
      >
        <XCircle aria-hidden="true" size={16} />
        Cancel order
      </Button>
      <Modal
        isOpen={confirming}
        onClose={close}
        closeOnClickOutside={!busy}
        title={canCancel ? "Cancel this order?" : "Cancellation unavailable"}
      >
        <p className="text-body leading-relaxed text-text-secondary">
          {canCancel
            ? "Any payment or deposit already paid will not be refunded, including payments confirmed while cancellation is being processed. This action cannot be undone."
            : reason}
        </p>
        <div className="mt-5 flex flex-wrap justify-end gap-3">
          <Button variant="secondary" disabled={busy} onClick={close} className="min-h-11">
            {canCancel ? "Keep order" : "Close"}
          </Button>
          {canCancel && (
            <Button
              variant="danger"
              loading={busy}
              onClick={async () => {
                await onCancel();
                setConfirming(false);
              }}
              className="min-h-11"
            >
              {busy ? "Cancelling…" : "Confirm cancellation"}
            </Button>
          )}
        </div>
      </Modal>
    </>
  );
}
