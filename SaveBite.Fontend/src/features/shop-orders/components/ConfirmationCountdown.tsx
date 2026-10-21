import { Clock3 } from "lucide-react";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { confirmationRemaining } from "../utils/orderHistory";

export function ConfirmationCountdown({ deadline, now }: { deadline: string; now: number }) {
  const remaining = confirmationRemaining(deadline, now);
  if (!remaining) return null;
  return (
    <div
      className={`mt-3 rounded-lg border p-3 text-xs ${remaining.urgent ? "border-red-200 bg-red-50 text-red-800" : "border-amber-100 bg-amber-50 text-amber-900"}`}
    >
      <p className="flex items-center gap-1.5 font-semibold">
        <Clock3 size={14} className="shrink-0" />
        <span role="timer" aria-live="off" aria-label="Time left to confirm">
          {remaining.label}
        </span>
      </p>
      <p className="mt-1 text-[11px] opacity-80">
        Confirm by <time dateTime={deadline}>{checkoutTime(deadline)}</time>
      </p>
    </div>
  );
}
