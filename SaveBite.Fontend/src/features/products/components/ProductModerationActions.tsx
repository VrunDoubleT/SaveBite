import { useId, useState, type FormEvent } from "react";
import {
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  Loader2,
  Send,
  ShieldAlert,
  ShieldCheck,
  Trash2,
} from "lucide-react";
import { productApi } from "../api/productApi";
import { warningApi } from "@/features/warnings/api/warningApi";

type ModerationAction = "warning" | "remove";

const REASON_MIN = 5;
const CORRECTION_MIN = 3;
const MAX_LENGTH = 1000;

const ACTIONS: { value: ModerationAction; label: string; icon: typeof Send; summary: string }[] = [
  {
    value: "warning",
    label: "Send warning",
    icon: AlertTriangle,
    summary: "The seller is notified of the violation and of the correction they must make.",
  },
  {
    value: "remove",
    label: "Remove product",
    icon: Trash2,
    summary:
      "The product is taken down and its flash deals are cancelled. Existing orders are preserved.",
  },
];

const REVIEW_LABEL: Record<number, { label: string; dot: string }> = {
  2: { label: "Approved", dot: "bg-green-500" },
  3: { label: "Needs revision", dot: "bg-orange-400" },
  4: { label: "Suspended", dot: "bg-red-500" },
};

const fieldClass =
  "mt-1.5 block w-full resize-y rounded-xl border border-border-default bg-white px-3.5 py-2.5 text-sm text-text-primary placeholder:text-text-muted transition focus:border-primary-500 focus:outline-none focus:ring-4 focus:ring-primary-500/15 disabled:cursor-not-allowed disabled:bg-neutral-50";

function Counter({ value, min }: { value: string; min: number }) {
  const length = value.trim().length;
  const tooShort = length > 0 && length < min;

  return (
    <span className={`text-xs tabular-nums ${tooShort ? "text-red-600" : "text-text-muted"}`}>
      {tooShort ? `At least ${min} characters · ` : ""}
      {value.length}/{MAX_LENGTH}
    </span>
  );
}

export function ProductModerationActions({
  productId,
  onRemoved,
  onReviewed,
  latestReview,
  warning,
}: {
  productId: string;
  onRemoved: () => void;
  onReviewed: () => void;
  latestReview?: { status: number; revisionId?: string };
  warning?: { firstEditedAt?: string } | null;
}) {
  const id = useId();

  const [action, setAction] = useState<ModerationAction>("warning");
  const [reason, setReason] = useState("");
  const [correction, setCorrection] = useState("");
  const [confirming, setConfirming] = useState(false);
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const [approving, setApproving] = useState(false);
  const isRemove = action === "remove";
  const canApprove =
    !!latestReview?.revisionId &&
    latestReview.status !== 2 &&
    latestReview.status !== 4 &&
    (!warning || !!warning.firstEditedAt);
  const reviewState = latestReview ? REVIEW_LABEL[latestReview.status] : undefined;

  const approve = async () => {
    if (!latestReview?.revisionId || approving || pending) return;
    setApproving(true);
    setError("");
    try {
      const result = await productApi.approveContent(productId, latestReview.revisionId);
      setMessage(result.message);
      onReviewed();
    } catch {
      setError("Could not approve the current revision. Reload and try again.");
    } finally {
      setApproving(false);
    }
  };
  const current = ACTIONS.find((item) => item.value === action) ?? ACTIONS[0];
  const valid =
    reason.trim().length >= REASON_MIN &&
    (isRemove || (!warning && correction.trim().length >= CORRECTION_MIN));

  const changeAction = (next: ModerationAction) => {
    setAction(next);
    setConfirming(false);
    setError("");
    setMessage("");
  };

  const perform = async () => {
    if (pending || !valid) return;

    setPending(true);
    setError("");
    setMessage("");

    try {
      const result = isRemove
        ? await productApi.removeForViolation(productId, reason.trim())
        : await warningApi.issueForProduct(productId, reason.trim(), correction.trim());

      setMessage(result.message);
      setReason("");
      setCorrection("");
      setConfirming(false);

      if (isRemove) onRemoved();
      else onReviewed();
    } catch {
      setError("Moderation action failed. Please try again.");
    } finally {
      setPending(false);
    }
  };

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (pending || !valid) return;

    if (isRemove && !confirming) {
      setConfirming(true);
      return;
    }

    void perform();
  };

  return (
    <section
      aria-labelledby={`${id}-title`}
      className="overflow-hidden rounded-3xl border border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)]"
    >
      <header className="flex items-start gap-4 px-6 pb-4 pt-6">
        <span className="flex size-11 shrink-0 items-center justify-center rounded-2xl bg-red-50 text-red-600">
          <ShieldAlert className="size-5" aria-hidden="true" />
        </span>
        <div className="min-w-0 space-y-0.5">
          <h2 id={`${id}-title`} className="text-lg font-semibold tracking-tight text-text-primary">
            Moderation decision
          </h2>
          <p className="text-sm text-text-secondary">
            Done reviewing? Approve the content, warn the seller, or remove the product if it
            violates content policy.
          </p>
        </div>
      </header>

      {/* Content review */}
      <div className="mx-6 mb-5 flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-neutral-50 px-4 py-3">
        <div className="flex items-center gap-3">
          <span
            className={`size-2 rounded-full ${reviewState?.dot ?? "bg-yellow-400"}`}
            aria-hidden="true"
          />
          <div>
            <p className="text-sm font-semibold text-text-primary">Content review</p>
            <p className="text-xs text-text-secondary">{reviewState?.label ?? "Pending review"}</p>
          </div>
        </div>
        <button
          type="button"
          onClick={approve}
          disabled={!canApprove || approving || pending}
          className="inline-flex items-center gap-2 rounded-xl bg-green-700 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-800 focus:outline-none focus-visible:ring-2 focus-visible:ring-green-600 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-40"
        >
          {approving ? (
            <Loader2
              className="size-4 animate-spin motion-reduce:animate-none"
              aria-hidden="true"
            />
          ) : (
            <ShieldCheck className="size-4" aria-hidden="true" />
          )}
          {approving ? "Approving…" : "Mark approved"}
        </button>
      </div>

      <form onSubmit={submit} noValidate>
        <div className="space-y-5 border-t border-border-default px-6 py-5">
          <fieldset disabled={pending} className="space-y-3">
            <legend className="sr-only">Moderation action</legend>

            <div className="inline-flex rounded-2xl bg-neutral-100 p-1">
              {ACTIONS.map(({ value, label, icon: Icon }) => (
                <label key={value} className="relative cursor-pointer">
                  <input
                    type="radio"
                    name={`${id}-action`}
                    value={value}
                    checked={action === value}
                    onChange={() => changeAction(value)}
                    className="peer sr-only"
                  />
                  <span
                    className={`flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:text-text-primary peer-checked:bg-white peer-checked:shadow-sm peer-focus-visible:ring-2 peer-focus-visible:ring-primary-500 peer-disabled:cursor-not-allowed peer-disabled:opacity-60 ${
                      value === "remove"
                        ? "peer-checked:text-red-700"
                        : "peer-checked:text-amber-700"
                    }`}
                  >
                    <Icon className="size-4" aria-hidden="true" />
                    {label}
                  </span>
                </label>
              ))}
            </div>

            <p
              className={`flex items-start gap-2.5 rounded-xl border-l-4 px-3.5 py-2.5 text-sm ${
                isRemove
                  ? "border-red-500 bg-red-50 text-red-800"
                  : "border-amber-400 bg-amber-50 text-amber-900"
              }`}
            >
              <current.icon className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
              {current.summary}
            </p>
          </fieldset>

          <div className={`grid gap-5 ${isRemove ? "" : "lg:grid-cols-2"}`}>
            <div>
              <label htmlFor={`${id}-reason`} className="text-sm font-medium text-text-primary">
                Reason
              </label>
              <textarea
                id={`${id}-reason`}
                value={reason}
                maxLength={MAX_LENGTH}
                rows={4}
                disabled={pending}
                onChange={(e) => {
                  setReason(e.target.value);
                  setConfirming(false);
                }}
                placeholder="Explain which policy the product violates."
                className={fieldClass}
              />
              <div className="mt-1 flex justify-end">
                <Counter value={reason} min={REASON_MIN} />
              </div>
            </div>

            {!isRemove && (
              <div>
                <label
                  htmlFor={`${id}-correction`}
                  className="text-sm font-medium text-text-primary"
                >
                  Required correction
                </label>
                <textarea
                  id={`${id}-correction`}
                  value={correction}
                  maxLength={MAX_LENGTH}
                  rows={4}
                  disabled={pending}
                  onChange={(e) => setCorrection(e.target.value)}
                  placeholder="Tell the seller what to change."
                  className={fieldClass}
                />
                <div className="mt-1 flex justify-end">
                  <Counter value={correction} min={CORRECTION_MIN} />
                </div>
              </div>
            )}
          </div>

          {isRemove && confirming && (
            <div
              role="alertdialog"
              aria-label="Confirm product removal"
              className="flex gap-3 rounded-2xl border border-red-200 bg-red-50 p-4"
            >
              <AlertTriangle className="mt-0.5 size-5 shrink-0 text-red-600" aria-hidden="true" />
              <div className="space-y-3">
                <p className="text-sm text-red-800">
                  Remove this product and cancel its flash deals? Existing orders will be preserved.
                </p>
                <div className="flex flex-wrap gap-2">
                  <button
                    type="button"
                    onClick={() => void perform()}
                    disabled={pending}
                    className="inline-flex items-center gap-2 rounded-xl bg-red-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-red-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-red-500 focus-visible:ring-offset-2 disabled:opacity-50"
                  >
                    {pending && (
                      <Loader2
                        className="size-4 animate-spin motion-reduce:animate-none"
                        aria-hidden="true"
                      />
                    )}
                    {pending ? "Removing…" : "Yes, remove product"}
                  </button>
                  <button
                    type="button"
                    onClick={() => setConfirming(false)}
                    disabled={pending}
                    className="rounded-xl border border-red-200 bg-white px-4 py-2 text-sm font-medium text-red-700 transition-colors hover:bg-red-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-red-500 disabled:opacity-50"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>

        <footer className="flex flex-wrap items-center justify-between gap-3 border-t border-border-default bg-neutral-50/70 px-6 py-4">
          <div className="min-h-5 text-sm" aria-live="polite">
            {error ? (
              <p role="alert" className="flex items-center gap-2 text-red-700">
                <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
                {error}
              </p>
            ) : message ? (
              <p role="status" className="flex items-center gap-2 text-green-700">
                <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
                {message}
              </p>
            ) : (
              <p className="text-text-muted">
                {valid ? "Ready to submit." : "Fill in the required fields to continue."}
              </p>
            )}
          </div>

          {!(isRemove && confirming) && (
            <button
              type="submit"
              disabled={pending || !valid}
              className={`inline-flex items-center gap-2 rounded-xl px-5 py-2.5 text-sm font-semibold shadow-sm transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${
                isRemove
                  ? "bg-red-600 text-white hover:bg-red-700 focus-visible:ring-red-500"
                  : "bg-amber-400 text-amber-950 hover:bg-amber-500 focus-visible:ring-amber-500"
              }`}
            >
              {pending ? (
                <Loader2
                  className="size-4 animate-spin motion-reduce:animate-none"
                  aria-hidden="true"
                />
              ) : isRemove ? (
                <Trash2 className="size-4" aria-hidden="true" />
              ) : (
                <Send className="size-4" aria-hidden="true" />
              )}
              {pending ? "Processing…" : isRemove ? "Remove product" : "Send warning"}
            </button>
          )}
        </footer>
      </form>
    </section>
  );
}
