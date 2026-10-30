import { useEffect, useState } from "react";
import { AlertCircle, EyeOff, Loader2, Package, Star, Store, Undo2, X } from "lucide-react";
import type { Feedback } from "../types/feedback.types";

export type ModerationMode = "hide" | "restore";

type FeedbackModerationDialogProps = {
  feedback: Feedback;
  mode: ModerationMode;
  onClose: () => void;
  /** Should throw/reject when the request fails so the dialog can show an error. */
  onConfirm: (reason: string) => Promise<void>;
};

const MAX_LENGTH = 1000;
const MIN_HIDE_LENGTH = 5;

const QUICK_REASONS = [
  "Spam or advertising",
  "Offensive language",
  "Irrelevant to the product",
  "Contains personal information",
];

const focusRing =
  "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700";

const THEME = {
  hide: {
    icon: EyeOff,
    iconWrap: "bg-red-50 text-red-700 ring-red-200",
    glow: "from-red-50/80",
    title: "Hide violating feedback",
    description: "This feedback will no longer be visible to customers.",
    label: "Reason for hiding",
    badge: "Required",
    placeholder: "Explain why this feedback violates the rules...",
    confirm: "Hide feedback",
    button: "bg-red-700 hover:bg-red-800",
    ring: "focus-visible:outline-red-700",
    error: "Unable to hide feedback.",
  },
  restore: {
    icon: Undo2,
    iconWrap: "bg-emerald-50 text-emerald-700 ring-emerald-200",
    glow: "from-emerald-50/80",
    title: "Restore feedback",
    description: "This feedback will become visible to everyone again.",
    label: "Reason for restoring",
    badge: "Optional",
    placeholder: "Add a note if needed...",
    confirm: "Restore feedback",
    button: "bg-emerald-700 hover:bg-emerald-800",
    ring: "focus-visible:outline-emerald-700",
    error: "Unable to restore feedback.",
  },
} as const;

function RatingStars({ rating }: { rating: number }) {
  const value = Math.max(0, Math.min(5, Math.round(Number(rating) || 0)));
  return (
    <div className="flex items-center gap-0.5" role="img" aria-label={`${value} out of 5 stars`}>
      {Array.from({ length: 5 }).map((_, i) => (
        <Star
          key={i}
          className={`size-3.5 ${
            i < value ? "fill-amber-400 text-amber-400" : "fill-neutral-100 text-neutral-300"
          }`}
          aria-hidden="true"
        />
      ))}
    </div>
  );
}

export function FeedbackModerationDialog({
  feedback,
  mode,
  onClose,
  onConfirm,
}: FeedbackModerationDialogProps) {
  const theme = THEME[mode];
  const Icon = theme.icon;
  const isRestore = mode === "restore";

  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [visible, setVisible] = useState(false);

  const trimmed = reason.trim();
  const isValid = isRestore ? true : trimmed.length >= MIN_HIDE_LENGTH;
  const tooShort = !isRestore && trimmed.length > 0 && trimmed.length < MIN_HIDE_LENGTH;
  const canSubmit = isValid && !busy;
  const customer = feedback.customerName || "Unknown customer";

  // Enter animation
  useEffect(() => {
    const id = requestAnimationFrame(() => setVisible(true));
    return () => cancelAnimationFrame(id);
  }, []);

  // Lock page scroll while open
  useEffect(() => {
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, []);

  // Escape closes
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !busy) onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [busy, onClose]);

  async function submit() {
    if (!canSubmit) return;

    setBusy(true);
    setError("");

    try {
      await onConfirm(trimmed);
    } catch {
      setError(theme.error);
      setBusy(false);
    }
  }

  function applyQuickReason(text: string) {
    setReason((current) => {
      const base = current.trim();
      return (base ? `${base}. ${text}` : text).slice(0, MAX_LENGTH);
    });
  }

  return (
    <div
      className={`fixed inset-0 z-50 flex items-end justify-center bg-neutral-900/50 p-0 backdrop-blur-sm transition-opacity duration-200 motion-reduce:transition-none sm:items-center sm:p-4 ${
        visible ? "opacity-100" : "opacity-0"
      }`}
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !busy) onClose();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="feedback-moderation-title"
        className={`flex max-h-[92vh] w-full max-w-xl flex-col overflow-hidden rounded-t-3xl bg-white shadow-2xl ring-1 ring-black/5 transition duration-200 motion-reduce:transition-none sm:rounded-3xl ${
          visible
            ? "translate-y-0 opacity-100 sm:scale-100"
            : "translate-y-6 opacity-0 sm:translate-y-0 sm:scale-95"
        }`}
      >
        {/* Header */}
        <div
          className={`relative flex items-start gap-4 bg-gradient-to-b ${theme.glow} to-white px-5 pb-4 pt-5 sm:px-6 sm:pt-6`}
        >
          <span
            className={`flex size-12 shrink-0 items-center justify-center rounded-2xl ring-1 ring-inset ${theme.iconWrap}`}
          >
            <Icon className="size-6" aria-hidden="true" />
          </span>
          <div className="min-w-0 flex-1 pt-0.5">
            <h2
              id="feedback-moderation-title"
              className="text-lg font-semibold tracking-tight text-text-primary"
            >
              {theme.title}
            </h2>
            <p className="mt-0.5 text-sm text-text-secondary">{theme.description}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={busy}
            aria-label="Close"
            className={`flex size-9 shrink-0 items-center justify-center rounded-full text-text-muted transition-colors hover:bg-neutral-100 hover:text-text-primary disabled:opacity-50 ${focusRing}`}
          >
            <X className="size-5" aria-hidden="true" />
          </button>
        </div>

        {/* Body */}
        <div className="space-y-5 overflow-y-auto px-5 pb-5 sm:px-6 sm:pb-6">
          {/* Feedback preview */}
          <section
            aria-label="Feedback details"
            className="overflow-hidden rounded-2xl border border-border-default bg-neutral-50/70"
          >
            <div className="flex items-center justify-between gap-3 border-b border-border-default px-4 py-3">
              <div className="flex min-w-0 items-center gap-2.5">
                <span
                  aria-hidden="true"
                  className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary-50 text-xs font-semibold uppercase text-primary-700"
                >
                  {customer.trim().charAt(0) || "?"}
                </span>
                <p className="truncate text-sm font-medium text-text-primary">{customer}</p>
              </div>
              <RatingStars rating={feedback.rating} />
            </div>

            <div className="space-y-3 px-4 py-3.5">
              <p className="max-h-36 overflow-y-auto whitespace-pre-wrap break-words text-sm leading-6 text-text-primary">
                {feedback.comment || <span className="italic text-text-muted">(No comment)</span>}
              </p>

              <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-text-secondary">
                <span className="inline-flex min-w-0 items-center gap-1.5">
                  <Package className="size-3.5 shrink-0 text-text-muted" aria-hidden="true" />
                  <span className="truncate">{feedback.productName || "Unknown product"}</span>
                </span>
                <span className="inline-flex min-w-0 items-center gap-1.5">
                  <Store className="size-3.5 shrink-0 text-text-muted" aria-hidden="true" />
                  <span className="truncate">{feedback.shopName || "Unknown shop"}</span>
                </span>
              </div>
            </div>

            {feedback.currentReason?.trim() && (
              <div className="border-t border-border-default bg-white px-4 py-3">
                <p className="text-xs font-semibold uppercase tracking-wide text-text-muted">
                  Previous reason
                </p>
                <p className="mt-1 whitespace-pre-wrap break-words text-sm text-text-secondary">
                  {feedback.currentReason.trim()}
                </p>
              </div>
            )}
          </section>

          {/* Reason */}
          <div className="space-y-2.5">
            <div className="flex items-center justify-between gap-3">
              <label
                htmlFor="feedback-moderation-reason"
                className="text-sm font-medium text-text-primary"
              >
                {theme.label}
              </label>
              <span
                className={`rounded-full px-2 py-0.5 text-[11px] font-semibold ${
                  isRestore ? "bg-neutral-100 text-text-muted" : "bg-red-50 text-red-700"
                }`}
              >
                {theme.badge}
              </span>
            </div>

            {!isRestore && (
              <div className="flex flex-wrap gap-2" aria-label="Quick reasons">
                {QUICK_REASONS.map((text) => (
                  <button
                    key={text}
                    type="button"
                    disabled={busy}
                    onClick={() => applyQuickReason(text)}
                    className={`rounded-full border border-border-default bg-white px-3 py-1 text-xs font-medium text-text-secondary transition-colors hover:border-neutral-300 hover:bg-neutral-50 hover:text-text-primary disabled:opacity-50 ${focusRing}`}
                  >
                    {text}
                  </button>
                ))}
              </div>
            )}

            <textarea
              id="feedback-moderation-reason"
              autoFocus
              rows={4}
              maxLength={MAX_LENGTH}
              value={reason}
              disabled={busy}
              placeholder={theme.placeholder}
              aria-invalid={tooShort}
              onChange={(event) => setReason(event.target.value)}
              onKeyDown={(event) => {
                if ((event.ctrlKey || event.metaKey) && event.key === "Enter") void submit();
              }}
              className={`block min-h-28 w-full resize-y rounded-2xl border bg-white p-3.5 text-sm leading-6 text-text-primary transition-colors placeholder:text-text-muted disabled:bg-neutral-50 ${
                focusRing
              } ${theme.ring} ${
                tooShort ? "border-red-300" : "border-border-default hover:border-neutral-300"
              }`}
            />

            <div className="flex items-center justify-between gap-3 text-xs">
              <p className={tooShort ? "font-medium text-red-600" : "text-text-muted"}>
                {isRestore
                  ? `Maximum ${MAX_LENGTH} characters.`
                  : `At least ${MIN_HIDE_LENGTH} characters.`}
              </p>
              <p className="tabular-nums text-text-muted">
                {reason.length}/{MAX_LENGTH}
              </p>
            </div>
          </div>

          {error && (
            <p
              role="alert"
              className="flex items-center gap-2 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700"
            >
              <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
              {error}
            </p>
          )}
        </div>

        {/* Footer */}
        <div className="flex flex-col-reverse gap-2 border-t border-border-default bg-neutral-50/70 px-5 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-6">
          <p className="hidden text-xs text-text-muted sm:block">
            Press{" "}
            <kbd className="rounded border border-border-default bg-white px-1 font-sans">Ctrl</kbd>{" "}
            +{" "}
            <kbd className="rounded border border-border-default bg-white px-1 font-sans">
              Enter
            </kbd>{" "}
            to confirm
          </p>

          <div className="flex flex-col-reverse gap-2 sm:flex-row">
            <button
              type="button"
              disabled={busy}
              onClick={onClose}
              className={`inline-flex h-10 items-center justify-center rounded-full border border-border-default bg-white px-5 text-[13px] font-medium text-text-primary transition-colors hover:bg-neutral-50 disabled:opacity-50 ${focusRing}`}
            >
              Cancel
            </button>
            <button
              type="button"
              disabled={!canSubmit}
              onClick={() => void submit()}
              className={`inline-flex h-10 items-center justify-center gap-2 rounded-full px-5 text-[13px] font-medium text-white shadow-sm transition-all active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-50 disabled:active:scale-100 ${focusRing} ${theme.button}`}
            >
              {busy && (
                <Loader2
                  className="size-4 animate-spin motion-reduce:animate-none"
                  aria-hidden="true"
                />
              )}
              {busy ? "Processing..." : theme.confirm}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
