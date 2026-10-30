import { EyeOff, Mail, Phone, Star, Store, Undo2 } from "lucide-react";
import type { Feedback, FeedbackStatus } from "../types/feedback.types";

/* ---------- Status helpers ---------- */

const STATUS_CODES: Record<string, number> = {
  Visible: 1,
  HiddenByCustomer: 2,
  HiddenByAdmin: 3,
};

const statusCode = (status: FeedbackStatus): number =>
  typeof status === "number" ? status : (STATUS_CODES[status] ?? 0);

const STATUS_STYLES: Record<number, { label: string; badge: string; dot: string }> = {
  1: {
    label: "Visible",
    badge: "bg-emerald-50 text-emerald-700 ring-emerald-200",
    dot: "bg-emerald-500",
  },
  2: {
    label: "Hidden by customer",
    badge: "bg-slate-100 text-slate-700 ring-slate-200",
    dot: "bg-slate-400",
  },
  3: { label: "Hidden by admin", badge: "bg-red-50 text-red-700 ring-red-200", dot: "bg-red-500" },
};

const UNKNOWN_STYLE = {
  label: "Unknown",
  badge: "bg-neutral-100 text-neutral-600 ring-neutral-200",
  dot: "bg-neutral-400",
};

const focusRing =
  "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700";

/* ---------- Columns ---------- */

const COLUMNS = [
  { label: "Customer", className: "w-[24%]" },
  { label: "Feedback", className: "" },
  { label: "Product", className: "w-[22%]" },
  { label: "Status", className: "w-44" },
  { label: "Action", className: "w-32 text-right" },
];

/* ---------- Pieces ---------- */

function Stars({ rating }: { rating: number }) {
  const value = Math.max(0, Math.min(5, Math.round(Number(rating) || 0)));
  return (
    <div className="flex items-center gap-2">
      <div className="flex items-center gap-0.5" role="img" aria-label={`${value} out of 5 stars`}>
        {Array.from({ length: 5 }).map((_, i) => (
          <Star
            key={i}
            className={`size-4 ${
              i < value ? "fill-amber-400 text-amber-400" : "fill-neutral-100 text-neutral-300"
            }`}
            aria-hidden="true"
          />
        ))}
      </div>
      <span className="text-xs font-medium tabular-nums text-text-muted">{value}/5</span>
    </div>
  );
}

/* ---------- Table ---------- */

type FeedbackTableProps = {
  items: Feedback[];
  onModerate: (item: Feedback) => void;
};

export function FeedbackTable({ items, onModerate }: FeedbackTableProps) {
  return (
    <div className="overflow-hidden rounded-3xl border border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)]">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[960px] table-fixed text-left text-sm">
          <thead className="bg-neutral-50">
            <tr>
              {COLUMNS.map((column) => (
                <th
                  key={column.label}
                  scope="col"
                  className={`whitespace-nowrap px-5 py-3 text-xs font-semibold uppercase tracking-wide text-text-muted ${column.className}`}
                >
                  {column.label}
                </th>
              ))}
            </tr>
          </thead>

          <tbody className="divide-y divide-border-default">
            {items.map((item) => {
              const status = statusCode(item.status);
              const style = STATUS_STYLES[status] ?? UNKNOWN_STYLE;
              const canHide = status === 1;
              const canRestore = status === 3;
              const customer = item.customerName || "Unknown customer";

              return (
                <tr key={item.id} className="align-top transition-colors hover:bg-neutral-50/70">
                  {/* Customer */}
                  <td className="px-5 py-4">
                    <div className="flex items-start gap-3">
                      <div className="min-w-0 flex-1 space-y-1">
                        <p className="truncate font-medium text-text-primary" title={customer}>
                          {customer}
                        </p>
                        <p
                          className="flex min-w-0 items-center gap-1.5 text-xs text-text-secondary"
                          title={item.customerEmail || undefined}
                        >
                          <Mail className="size-3.5 shrink-0 text-text-muted" aria-hidden="true" />
                          <span className="truncate">{item.customerEmail || "No email"}</span>
                        </p>
                        <p className="flex min-w-0 items-center gap-1.5 text-xs text-text-secondary">
                          <Phone className="size-3.5 shrink-0 text-text-muted" aria-hidden="true" />
                          <span className="truncate">{item.customerPhone || "No phone"}</span>
                        </p>
                      </div>
                    </div>
                  </td>

                  {/* Feedback: rating + comment */}
                  <td className="px-5 py-4">
                    <div className="space-y-2">
                      <Stars rating={item.rating} />
                      {item.comment ? (
                        <p
                          className="line-clamp-3 whitespace-pre-wrap break-words leading-6 text-text-primary"
                          title={item.comment}
                        >
                          {item.comment}
                        </p>
                      ) : (
                        <p className="italic text-text-muted">(No comment)</p>
                      )}
                    </div>
                  </td>

                  {/* Product + shop */}
                  <td className="px-5 py-4">
                    <div className="space-y-2">
                      <p className="flex items-start gap-1.5 font-medium text-text-primary">
                        <span className="line-clamp-2 break-words" title={item.productName}>
                          {item.productName || "Unknown product"}
                        </span>
                      </p>
                      <p className="inline-flex max-w-full items-center gap-1.5 rounded-full bg-primary-50 px-2.5 py-0.5 text-xs font-medium text-primary-700">
                        <Store className="size-3 shrink-0" aria-hidden="true" />
                        <span className="truncate">{item.shopName || "Unknown shop"}</span>
                      </p>
                    </div>
                  </td>

                  {/* Status */}
                  <td className="px-5 py-4">
                    <span
                      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset ${style.badge}`}
                    >
                      <span className={`size-1.5 rounded-full ${style.dot}`} aria-hidden="true" />
                      {style.label}
                    </span>
                  </td>

                  {/* Action */}
                  <td className="px-5 py-4 text-right">
                    {canHide || canRestore ? (
                      <button
                        type="button"
                        onClick={() => onModerate(item)}
                        className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-3.5 py-1.5 text-[13px] font-medium transition-colors ${focusRing} ${
                          canHide
                            ? "border-red-200 bg-white text-red-700 hover:bg-red-50"
                            : "border-emerald-200 bg-white text-emerald-700 hover:bg-emerald-50"
                        }`}
                      >
                        {canHide ? (
                          <EyeOff className="size-4" aria-hidden="true" />
                        ) : (
                          <Undo2 className="size-4" aria-hidden="true" />
                        )}
                        {canHide ? "Hide" : "Restore"}
                      </button>
                    ) : (
                      <span className="text-text-muted" aria-label="No action available">
                        —
                      </span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
