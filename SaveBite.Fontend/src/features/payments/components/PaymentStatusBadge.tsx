const colors: Record<string, string> = {
  Pending: "bg-amber-50 text-amber-800 ring-amber-200",
  Succeeded: "bg-emerald-50 text-emerald-800 ring-emerald-200",
  Failed: "bg-red-50 text-red-700 ring-red-200",
  Cancelled: "bg-neutral-100 text-neutral-600 ring-neutral-200",
  Expired: "bg-neutral-100 text-neutral-600 ring-neutral-200",
};

export function PaymentStatusBadge({ status }: { status: string }) {
  return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${colors[status] ?? colors.Cancelled}`}>{status === "Succeeded" ? "Paid" : status}</span>;
}
