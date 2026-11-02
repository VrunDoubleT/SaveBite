import { Clock3, Flag } from "lucide-react";
import type { CheckoutOrder } from "../types/checkout.types";
import { checkoutTime } from "../utils/checkoutPolicy";

export function OrderPickupDeadlines({
  order,
  now,
  compact = false,
}: {
  order: CheckoutOrder;
  now: number;
  compact?: boolean;
}) {
  const active = [
    "AwaitingPayment",
    "Pending",
    "Confirmed",
    "Preparing",
    "ReadyForPickup",
  ].includes(order.status);
  const hard = order.hardDeadlineAt ? Date.parse(order.hardDeadlineAt) : NaN;
  const soft = order.softDeadlineAt ? Date.parse(order.softDeadlineAt) : NaN;
  const minutes = Math.ceil((hard - now) / 60000);
  const urgent = active && Number.isFinite(hard) && minutes <= 10;
  if (compact)
    return (
      <div className="grid grid-cols-2 gap-3 rounded-md bg-neutral-50 p-3 text-caption">
        <div>
          <p className="text-text-secondary">Recommended arrival</p>
          <time
            dateTime={order.softDeadlineAt}
            className="mt-1 block font-semibold text-text-primary"
          >
            {checkoutTime(order.softDeadlineAt)}
          </time>
          <span className="mt-0.5 block text-caption text-text-muted">Soft deadline</span>
        </div>
        <div className="border-l border-border-default pl-3">
          <p className={urgent ? "text-danger-strong" : "text-text-secondary"}>Latest pickup</p>
          <time
            dateTime={order.hardDeadlineAt}
            className={`mt-1 block font-semibold ${urgent ? "text-danger-strong" : "text-text-primary"}`}
          >
            {checkoutTime(order.hardDeadlineAt)}
          </time>
          <span
            className={`mt-0.5 block text-caption ${urgent ? "text-danger-strong" : "text-text-muted"}`}
          >
            {active && Number.isFinite(hard)
              ? minutes <= 0
                ? "Pickup deadline passed"
                : `${minutes} min left · Hard deadline`
              : "Hard deadline"}
          </span>
        </div>
      </div>
    );
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <div className="rounded-md border border-primary-100 bg-primary-50/60 p-4">
        <p className="flex items-center gap-2 text-caption font-semibold text-primary-800">
          <Clock3 aria-hidden="true" size={15} />
          Recommended arrival · Soft deadline
        </p>
        <time
          dateTime={order.softDeadlineAt}
          className="mt-2 block text-section font-bold text-primary-900"
        >
          {checkoutTime(order.softDeadlineAt)}
        </time>
        <p className="mt-1 text-caption text-primary-800">
          {active && now >= soft
            ? "Recommended arrival time has passed."
            : "Aim to arrive by this time."}
        </p>
      </div>
      <div
        className={`rounded-md border p-4 ${urgent ? "border-danger-soft bg-danger-soft" : "border-accent-100 bg-accent-50/60"}`}
      >
        <p
          className={`flex items-center gap-2 text-caption font-semibold ${urgent ? "text-danger-strong" : "text-accent-700"}`}
        >
          <Flag aria-hidden="true" size={15} />
          Latest pickup · Hard deadline
        </p>
        <time
          dateTime={order.hardDeadlineAt}
          className={`mt-2 block text-section font-bold ${urgent ? "text-danger-strong" : "text-accent-700"}`}
        >
          {checkoutTime(order.hardDeadlineAt)}
        </time>
        <p
          className={`mt-1 text-caption ${urgent ? "font-semibold text-danger-strong" : "text-accent-700"}`}
        >
          {active && Number.isFinite(hard)
            ? minutes <= 0
              ? "Pickup deadline has passed."
              : `Collect within ${minutes} min. This deadline cannot be extended.`
            : "The final pickup deadline recorded for this order."}
        </p>
      </div>
    </div>
  );
}
