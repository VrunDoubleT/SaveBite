import { EyeOff, Package, RotateCcw, Star, Store } from "lucide-react";
import { RecordId } from "@/shared/components/RecordId";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { Feedback } from "../types/feedback.types";
import { FeedbackCustomerAvatar } from "./FeedbackCustomerAvatar";

type FeedbackDetailsDialogProps = {
  feedback: Feedback | null;
  onClose: () => void;
  onModerate: (feedback: Feedback) => void;
};

const STATUS_PRESENTATION: Record<string, { label: string; badge: string; dot: string }> = {
  Visible: {
    label: "Visible",
    badge: "bg-emerald-50 text-emerald-700 ring-emerald-200",
    dot: "bg-emerald-500",
  },
  "1": {
    label: "Visible",
    badge: "bg-emerald-50 text-emerald-700 ring-emerald-200",
    dot: "bg-emerald-500",
  },
  HiddenByCustomer: {
    label: "Hidden by customer",
    badge: "bg-slate-100 text-slate-700 ring-slate-200",
    dot: "bg-slate-400",
  },
  "2": {
    label: "Hidden by customer",
    badge: "bg-slate-100 text-slate-700 ring-slate-200",
    dot: "bg-slate-400",
  },
  HiddenByAdmin: {
    label: "Hidden by admin",
    badge: "bg-red-50 text-red-700 ring-red-200",
    dot: "bg-red-500",
  },
  "3": {
    label: "Hidden by admin",
    badge: "bg-red-50 text-red-700 ring-red-200",
    dot: "bg-red-500",
  },
};

function FeedbackRating({ rating }: { rating: number }) {
  const score = Math.max(0, Math.min(5, Math.round(Number(rating) || 0)));

  return (
    <div className="flex shrink-0 items-center gap-2" aria-label={`${score} out of 5 stars`}>
      <div className="flex items-center gap-0.5" aria-hidden="true">
        {Array.from({ length: 5 }, (_, index) => (
          <Star
            key={index}
            className={`size-5 ${
              index < score ? "fill-amber-400 text-amber-400" : "fill-neutral-100 text-neutral-300"
            }`}
          />
        ))}
      </div>
      <span className="text-sm font-semibold tabular-nums text-text-secondary">{score}/5</span>
    </div>
  );
}

export function FeedbackDetailsDialog({
  feedback,
  onClose,
  onModerate,
}: FeedbackDetailsDialogProps) {
  if (!feedback) return null;

  const status =
    typeof feedback.status === "string" ? feedback.status : String(feedback.status);
  const canHide = status === "Visible" || status === "1";
  const canRestore = status === "HiddenByAdmin" || status === "3";
  const customerName = feedback.customerName || "Unknown customer";
  const statusPresentation = STATUS_PRESENTATION[status] ?? {
    label: status,
    badge: "bg-neutral-100 text-neutral-600 ring-neutral-200",
    dot: "bg-neutral-400",
  };

  return (
    <SharedModal
      isOpen
      onClose={onClose}
      title="Feedback Details"
      headerContent={<RecordId id={feedback.id} variant="full" />}
      maxWidthClass="max-w-2xl"
    >
      <div className="space-y-5">
        <article className="overflow-hidden rounded-2xl border border-neutral-200 bg-neutral-50/80 p-4 shadow-sm sm:p-5">
          <header className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3.5">
              <FeedbackCustomerAvatar
                avatarUrl={feedback.avatarUrl}
                customerName={customerName}
                size="md"
              />
              <div className="min-w-0">
                <h3 className="truncate text-sm font-semibold text-neutral-900 sm:text-base">
                  {customerName}
                </h3>
                <p className="mt-0.5 truncate text-sm text-neutral-500">
                  {feedback.customerEmail || "No email"}
                </p>
              </div>
            </div>
            <div className="flex flex-wrap items-center justify-end gap-3">
              <span
                className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset ${statusPresentation.badge}`}
              >
                <span
                  className={`size-1.5 rounded-full ${statusPresentation.dot}`}
                  aria-hidden="true"
                />
                {statusPresentation.label}
              </span>
              <FeedbackRating rating={feedback.rating} />
            </div>
          </header>

          <div className="mt-5 grid gap-3 sm:grid-cols-2">
            <section className="min-w-0 rounded-xl border border-neutral-200 bg-white p-3.5">
              <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-neutral-500">
                <Package className="size-4 shrink-0 text-primary-600" aria-hidden="true" />
                Product
              </div>
              <p className="mt-1.5 break-words text-sm font-semibold text-neutral-900">
                {feedback.productName || "Unknown product"}
              </p>
            </section>
            <section className="min-w-0 rounded-xl border border-neutral-200 bg-white p-3.5">
              <div className="flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-neutral-500">
                <Store className="size-4 shrink-0 text-emerald-600" aria-hidden="true" />
                Shop
              </div>
              <p className="mt-1.5 break-words text-sm font-semibold text-neutral-900">
                {feedback.shopName || "Unknown shop"}
              </p>
            </section>
          </div>

          <div className="mt-4 rounded-2xl border border-neutral-200 bg-white px-4 py-4 sm:px-5 sm:py-5">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-neutral-500">
              Customer feedback
            </p>
            <p className="whitespace-pre-wrap break-words text-[15px] leading-7 text-neutral-800">
              {feedback.comment || (
                <span className="italic text-neutral-400">No written comment.</span>
              )}
            </p>
          </div>
        </article>

        <footer className="flex flex-wrap items-center justify-end gap-3 border-t border-border-default pt-4">
          {canHide || canRestore ? (
            <button
              type="button"
              onClick={() => onModerate(feedback)}
              className={`inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-semibold shadow-sm transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${
                canHide
                  ? "border-red-200 bg-white text-red-700 hover:border-red-300 hover:bg-red-50 focus-visible:outline-red-600"
                  : "border-emerald-200 bg-white text-emerald-700 hover:border-emerald-300 hover:bg-emerald-50 focus-visible:outline-emerald-600"
              }`}
            >
              {canHide ? (
                <EyeOff className="size-4" aria-hidden="true" />
              ) : (
                <RotateCcw className="size-4" aria-hidden="true" />
              )}
              {canHide ? "Hide feedback" : "Restore feedback"}
            </button>
          ) : (
            <span className="rounded-lg bg-neutral-100 px-3 py-2 text-xs font-medium text-neutral-500">
              No action available
            </span>
          )}
        </footer>
      </div>
    </SharedModal>
  );
}
