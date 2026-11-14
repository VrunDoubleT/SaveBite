import { Clock3 } from "lucide-react";

/** Include the date and timezone so overnight deals never look like an earlier same-day cutoff. */
export function PickupDeadlineNotice({ deadline }: { deadline: string }) {
  const date = new Date(deadline);
  if (!Number.isFinite(date.getTime())) return null;
  const formatted = new Intl.DateTimeFormat("en-GB", {
    timeZone: "Asia/Bangkok",
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(date);
  return (
    <p className="flex items-start gap-1.5 text-xs leading-relaxed text-text-secondary">
      <Clock3 className="mt-0.5 size-3.5 shrink-0 text-primary-700" aria-hidden="true" />
      <span>
        Final pickup deadline:{" "}
        <time dateTime={deadline} className="font-semibold text-text-primary">
          {formatted}
        </time>
      </span>
    </p>
  );
}
