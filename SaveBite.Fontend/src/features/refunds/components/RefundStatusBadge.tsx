import type { Refund } from "../types/refund.types";
import { refundStatusLabels } from "../utils/refundStatus";
const colors: Record<string, string> = {
  CustomerDetailsExpired: "border-accent-100 bg-accent-50 text-accent-700",
  Overdue: "border-danger-soft bg-danger-soft text-danger-strong",
  AwaitingCustomerDetails: "border-accent-100 bg-accent-50 text-accent-700",
  PendingVerification: "border-secondary-100 bg-secondary-50 text-secondary-700",
  AwaitingTransfer: "border-secondary-100 bg-secondary-50 text-secondary-700",
  Transferred: "border-primary-200 bg-primary-50 text-primary-800",
  Completed: "border-primary-200 bg-primary-50 text-primary-800",
  Rejected: "border-danger-soft bg-danger-soft text-danger-strong",
  Withdrawn: "border-border-default bg-neutral-100 text-text-secondary",
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
        className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-caption font-semibold ${colors[status] ?? colors.Withdrawn}`}
      >
        <span className="size-1.5 rounded-full bg-current" />
        {refundStatusLabels[status] ?? status}
      </span>
      {overdue && (
        <span className="rounded-full border border-danger-soft bg-danger-soft px-2.5 py-1 text-caption font-semibold text-danger-strong">
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
  const stopped = ["Rejected", "Withdrawn", "CustomerDetailsExpired"].includes(refund.status);
  const current = ["Transferred", "Completed"].includes(refund.status)
    ? steps.length - 1
    : ["AwaitingTransfer", "Overdue"].includes(refund.status)
      ? steps.length - 2
      : refund.status === "PendingVerification" || refund.status === "Rejected"
        ? 1
        : 0;
  return (
    <ol
      aria-label="Refund progress"
      className="grid grid-cols-2 gap-2 rounded-lg bg-neutral-50 p-3 md:flex md:gap-4"
    >
      {steps.map((label, index) => (
        <li
          key={label}
          className={`flex min-w-0 items-center gap-2 md:flex-1 ${index === current && !stopped ? "font-semibold text-primary-800" : index < current ? "text-primary-700" : "text-text-muted"}`}
          aria-current={index === current && !stopped ? "step" : undefined}
        >
          <span
            className={`flex size-6 shrink-0 items-center justify-center rounded-full text-caption font-semibold ${index <= current && !stopped ? "bg-primary-100 text-primary-800" : "bg-neutral-200 text-text-secondary"}`}
          >
            {index + 1}
          </span>
          <span className="text-caption">{label}</span>
        </li>
      ))}
    </ol>
  );
}
