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
  const active = ["AwaitingPayment", "Pending", "Confirmed", "Preparing", "ReadyForPickup"].includes(order.status);
  const hard = order.hardDeadlineAt ? Date.parse(order.hardDeadlineAt) : NaN;
  const soft = order.softDeadlineAt ? Date.parse(order.softDeadlineAt) : NaN;
  const minutes = Math.ceil((hard - now) / 60000);
  const urgent = active && Number.isFinite(hard) && minutes <= 10;
  if (compact)
    return (
      <div className="grid grid-cols-2 gap-3 rounded-lg bg-neutral-50 p-3 text-xs">
        <div>
          <p className="text-neutral-500">Recommended arrival</p>
          <time
            dateTime={order.softDeadlineAt}
            className="mt-1 block font-semibold text-neutral-800"
          >
            {checkoutTime(order.softDeadlineAt)}
          </time>
          <span className="mt-0.5 block text-[11px] text-neutral-400">Soft deadline</span>
        </div>
        <div className="border-l border-neutral-200 pl-3">
          <p className={urgent ? "text-red-700" : "text-neutral-500"}>Latest pickup</p>
          <time
            dateTime={order.hardDeadlineAt}
            className={`mt-1 block font-semibold ${urgent ? "text-red-700" : "text-neutral-800"}`}
          >
            {checkoutTime(order.hardDeadlineAt)}
          </time>
          <span
            className={`mt-0.5 block text-[11px] ${urgent ? "text-red-600" : "text-neutral-400"}`}
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
      <div className="rounded-xl border border-emerald-100 bg-emerald-50/60 p-4">
        <p className="flex items-center gap-2 text-xs font-semibold text-emerald-800">
          <Clock3 size={15} />
          Recommended arrival · Soft deadline
        </p>
        <time
          dateTime={order.softDeadlineAt}
          className="mt-2 block text-base font-bold text-emerald-950"
        >
          {checkoutTime(order.softDeadlineAt)}
        </time>
        <p className="mt-1 text-xs text-emerald-800">
          {active && now >= soft
            ? "Recommended arrival time has passed."
            : "Aim to arrive by this time."}
        </p>
      </div>
      <div
        className={`rounded-xl border p-4 ${urgent ? "border-red-300 bg-red-50" : "border-amber-200 bg-amber-50/60"}`}
      >
        <p
          className={`flex items-center gap-2 text-xs font-semibold ${urgent ? "text-red-800" : "text-amber-800"}`}
        >
          <Flag size={15} />
          Latest pickup · Hard deadline
        </p>
        <time
          dateTime={order.hardDeadlineAt}
          className={`mt-2 block text-base font-bold ${urgent ? "text-red-950" : "text-amber-950"}`}
        >
          {checkoutTime(order.hardDeadlineAt)}
        </time>
        <p className={`mt-1 text-xs ${urgent ? "font-semibold text-red-800" : "text-amber-800"}`}>
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
