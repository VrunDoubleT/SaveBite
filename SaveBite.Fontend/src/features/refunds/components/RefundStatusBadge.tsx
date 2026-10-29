import type { Refund } from "../types/refund.types";
import { refundStatusLabels } from "../utils/refundStatus";
const colors: Record<string, string> = {
  AwaitingCustomerDetails: "border-amber-200 bg-amber-50 text-amber-800",
  PendingVerification: "border-blue-200 bg-blue-50 text-blue-800",
  AwaitingTransfer: "border-violet-200 bg-violet-50 text-violet-800",
  Transferred: "border-emerald-200 bg-emerald-50 text-emerald-800",
  Completed: "border-emerald-200 bg-emerald-50 text-emerald-800",
  Rejected: "border-red-200 bg-red-50 text-red-700",
  Withdrawn: "border-neutral-200 bg-neutral-100 text-neutral-600",
};
export function RefundStatusBadge({
  status,
  overdue = false,
}: {
  status: string;
  overdue?: boolean;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <span
        className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-semibold ${colors[status] ?? colors.Withdrawn}`}
      >
        <span className="size-1.5 rounded-full bg-current" />
        {refundStatusLabels[status] ?? status}
      </span>
      {overdue && (
        <span className="rounded-full border border-red-200 bg-red-50 px-2.5 py-1 text-xs font-semibold text-red-700">
          Processing deadline passed
        </span>
      )}
    </div>
  );
}
export function RefundProgress({ refund }: { refund: Refund }) {
  const steps =
    refund.type === "CustomerRequest"
      ? ["Customer details", "Shop review", "Bank transfer", "Refunded"]
      : ["Customer details", "Bank transfer", "Refunded"];
  const stopped = ["Rejected", "Withdrawn"].includes(refund.status);
  const current = ["Transferred", "Completed"].includes(refund.status)
    ? steps.length - 1
    : refund.status === "AwaitingTransfer"
      ? steps.length - 2
      : refund.status === "PendingVerification" || refund.status === "Rejected"
        ? 1
        : 0;
  return (
    <ol
      aria-label="Refund progress"
      className="grid grid-cols-2 gap-2 rounded-xl bg-neutral-50 p-3 md:flex md:gap-4"
    >
      {steps.map((label, index) => (
        <li
          key={label}
          className={`flex min-w-0 items-center gap-2 md:flex-1 ${index === current && !stopped ? "font-semibold text-emerald-800" : index < current ? "text-emerald-700" : "text-neutral-400"}`}
          aria-current={index === current && !stopped ? "step" : undefined}
        >
          <span
            className={`flex size-6 shrink-0 items-center justify-center rounded-full text-[11px] font-semibold ${index <= current && !stopped ? "bg-emerald-100 text-emerald-800" : "bg-neutral-200 text-neutral-500"}`}
          >
            {index + 1}
          </span>
          <span className="text-xs">{label}</span>
        </li>
      ))}
    </ol>
  );
}
