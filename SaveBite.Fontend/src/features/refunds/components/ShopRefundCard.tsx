import { RefundItems } from "./RefundItems";
import { RefundCustomerInfo } from "./RefundCustomerInfo";
import { useEffect, useRef, useState } from "react";
import { Check, X, QrCode, ShieldCheck, Clock3 } from "lucide-react";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { getApiErrorMessage } from "@/shared/api";
import { refundApi } from "../api/refundApi";
import type { Refund, RefundQr } from "../types/refund.types";
import { uploadRefundFiles } from "../api/uploadRefundFiles";
import { RefundFileUpload } from "./RefundFileUpload";
import { RefundEvidenceGallery } from "./RefundEvidenceGallery";
import { RefundStatusBadge, RefundProgress } from "./RefundStatusBadge";
const button =
  "rounded-lg bg-primary-700 px-4 py-2.5 text-xs font-semibold text-white hover:bg-primary-800 disabled:opacity-50";
const card = "min-w-0 rounded-xl border border-neutral-200 bg-white p-4 sm:p-5";
const time = (value?: string) => (value ? `${checkoutTime(value)} · GMT+7` : "—");
const historyLabels: Record<string, string> = {
  Requested: "Refund requested",
  DestinationProvided: "Destination provided",
  DestinationConfirmed: "Destination confirmed by email",
  Approved: "Requested refund approved",
  Rejected: "Refund declined",
  Transferred: "Transfer recorded",
  Withdrawn: "Request withdrawn",
  DeadlineExpired: "Processing deadline passed",
  LegacySnapshot: "Imported refund record",
  VietQrGenerated: "Transfer QR generated",
};

export function ShopRefundCard({
  refund,
  onChanged,
}: {
  refund: Refund;
  onChanged: (refund: Refund) => void;
}) {
  const [busy, setBusy] = useState(false);
  const operation = useRef(false);
  const [error, setError] = useState("");
  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState("");
  const [qr, setQr] = useState<RefundQr | null>(null);
  const [proof, setProof] = useState<File[]>([]);
  const [reference, setReference] = useState("");
  const [transferTime, setTransferTime] = useState("");
  const [now, setNow] = useState(Date.now);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 30_000);
    return () => window.clearInterval(timer);
  }, []);
  async function action(call: () => Promise<Refund>) {
    if (operation.current) return;
    operation.current = true;
    setBusy(true);
    setError("");
    try {
      onChanged(await call());
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      operation.current = false;
      setBusy(false);
    }
  }
  const timedOut =
    refund.status === "Rejected" &&
    refund.history.some((item) => item.decision === "DeadlineExpired");
  const active = ["PendingVerification", "AwaitingTransfer"].includes(refund.status);
  const overdue =
    active &&
    (refund.isOverdue || (!!refund.reviewDeadlineAt && Date.parse(refund.reviewDeadlineAt) <= now));
  const review =
    refund.type === "CustomerRequest" && refund.status === "PendingVerification" && !overdue;
  const transferable =
    refund.status === "AwaitingTransfer" && !!refund.destinationConfirmedAt && !overdue;
  const transferred = ["Transferred", "Completed"].includes(refund.status);
  const required = refund.type === "ShopCancellation";
  const currentQr =
    qr &&
    qr.amount === refund.amount - refund.transferredAmount &&
    qr.bankBin === refund.bankBin &&
    qr.accountNumber === refund.accountNumber
      ? qr
      : null;
  const summary = overdue
    ? "The processing deadline has passed. This refund will be closed automatically."
    : review
      ? "Review the customer's reason and evidence, then approve the full amount or decline with a reason."
      : transferable
        ? "Transfer the full balance to the confirmed recipient, then record the bank reference and proof."
        : transferred
          ? "The requested refund transfer has been recorded. No further action is required."
          : refund.status === "Rejected"
            ? timedOut
              ? "This refund was closed automatically because a deadline passed. No further action is required."
              : "This request was declined. No transfer can be recorded."
            : refund.status === "Withdrawn"
              ? "The customer withdrew this request. No further action is required."
              : "Waiting for the customer to provide and email-confirm their refund destination.";
  const outcomeTone =
    overdue || refund.status === "Rejected"
      ? "border-red-200 bg-red-50 text-red-800"
      : transferable || transferred
        ? "border-primary-200 bg-primary-50 text-primary-800"
        : "border-neutral-200 bg-neutral-50 text-neutral-700";
  async function generateQr() {
    if (operation.current) return;
    operation.current = true;
    setBusy(true);
    setError("");
    try {
      setQr(await refundApi.qr(refund.id));
    } catch (e) {
      setError(getApiErrorMessage(e));
    } finally {
      operation.current = false;
      setBusy(false);
    }
  }
  const reviewButtons = (
    <div className="flex flex-wrap gap-2">
      <button
        className={`${button} inline-flex items-center gap-1.5`}
        disabled={busy}
        onClick={() => void action(() => refundApi.review(refund.id, true))}
      >
        <Check size={15} />
        Approve requested refund
      </button>
      <button
        type="button"
        className="inline-flex items-center gap-1.5 rounded-lg border border-red-200 bg-red-50 px-4 py-2.5 text-xs font-semibold text-red-700 disabled:opacity-50"
        disabled={busy}
        onClick={() => setRejecting(!rejecting)}
      >
        <X size={15} />
        Decline request
      </button>
    </div>
  );
  return (
    <article aria-label="Shop refund management" className="space-y-4">
      <section className={`rounded-xl border p-4 sm:p-5 ${outcomeTone}`}>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p className="text-xs font-medium opacity-80">
              {required ? "Required shop refund" : "Customer refund request"}
            </p>
            <h2 className="mt-1 text-base font-semibold">
              {overdue
                ? "Processing deadline passed"
                : review
                  ? "Your decision is needed"
                  : transferable
                    ? "Ready for bank transfer"
                    : transferred
                      ? "Refund transferred"
                      : refund.status === "Rejected"
                        ? timedOut
                          ? "Refund closed"
                          : "Request declined"
                        : refund.status === "Withdrawn"
                          ? "Request withdrawn"
                          : "Waiting for customer details"}
            </h2>
          </div>
          <RefundStatusBadge status={refund.status} overdue={overdue} />
        </div>
        <p className="mt-3 text-sm leading-relaxed">{summary}</p>
        {refund.rejectReason && (
          <p className="mt-3 whitespace-pre-wrap break-words border-t border-current/15 pt-3 text-sm">
            <span className="font-semibold">Decline reason: </span>
            {refund.rejectReason}
          </p>
        )}
        {refund.status === "AwaitingCustomerDetails" && refund.customerDetailsDeadlineAt && (
          <p className="mt-3 text-xs font-medium">
            Customer must confirm by {time(refund.customerDetailsDeadlineAt)}. Unconfirmed refunds
            close automatically after 24 hours.
          </p>
        )}
        {active && refund.reviewDeadlineAt && (
          <p className="mt-3 flex items-start gap-2 text-xs font-medium">
            <Clock3 size={14} className="shrink-0" />
            Review and transfer deadline: {time(refund.reviewDeadlineAt)}
          </p>
        )}
        {required &&
          !transferred &&
          refund.status !== "Withdrawn" &&
          refund.status !== "Rejected" && (
            <p className="mt-3 text-xs">
              This refund is required after shop rejection or confirmation expiry. Approval is not
              needed and it cannot be declined.
            </p>
          )}
      </section>
      <section className={card}>
        <h2 className="mb-3 text-sm font-semibold">Refund overview</h2>
        <dl className="grid gap-4 lg:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1fr)]">
          <div>
            <dt className="mb-2 text-xs text-neutral-500">Customer</dt>
            <dd>
              <RefundCustomerInfo refund={refund} />
            </dd>
          </div>
          <div>
            <dt className="text-xs text-neutral-500">Requested refund amount</dt>
            <dd className="mt-1 text-lg font-bold">{money(refund.amount)}</dd>
          </div>
          <div>
            <dt className="text-xs text-neutral-500">Recorded as transferred</dt>
            <dd className="mt-1 text-lg font-bold text-primary-700">
              {money(refund.transferredAmount)}
            </dd>
          </div>
        </dl>
        <div className="mt-4 border-t border-neutral-100 pt-4">
          <RefundProgress refund={refund} />
          <RefundItems refund={refund} />
        </div>
      </section>
      {error && (
        <p
          role="alert"
          className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
        >
          {error}
        </p>
      )}
      <section className={card}>
        <h2 className="text-sm font-semibold">
          {required ? "Refund reason" : "Customer request and evidence"}
        </h2>
        <p className="mt-1 text-xs text-neutral-500">Submitted {time(refund.createdAt)}</p>
        <p className="mt-4 whitespace-pre-wrap break-words text-sm leading-relaxed text-neutral-700">
          {refund.reason || "No additional reason recorded."}
        </p>
        <RefundEvidenceGallery
          urls={refund.items?.some((item) => item.evidenceUrls?.length) ? [] : refund.evidenceUrls}
          label="Customer evidence"
        />
      </section>
      <section className={card}>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="text-sm font-semibold">Refund recipient</h2>
          {refund.destinationConfirmedAt && (
            <span className="inline-flex items-center gap-1 text-xs font-medium text-primary-700">
              <ShieldCheck size={14} />
              Email confirmed
            </span>
          )}
        </div>
        {refund.destinationConfirmedAt ? (
          <dl className="mt-4 grid gap-x-8 gap-y-4 text-sm sm:grid-cols-2">
            {[
              ["Bank", refund.bankName],
              ["Account number", refund.accountNumber],
              ["Account holder", refund.accountHolder],
              ["Confirmed at", time(refund.destinationConfirmedAt)],
            ].map(([label, value]) => (
              <div key={label}>
                <dt className="text-xs text-neutral-500">{label}</dt>
                <dd className="mt-1 break-words font-medium">{value || "—"}</dd>
              </div>
            ))}
          </dl>
        ) : (
          <p className="mt-3 text-sm text-neutral-500">
            Customer details are not confirmed yet. Wait for email confirmation before transferring
            money.
          </p>
        )}
      </section>
      {review && (
        <section className={`${card} border-blue-200`}>
          <h2 className="text-sm font-semibold">Review this request</h2>
          <p className="mt-2 text-sm text-neutral-500">
            Your decision applies to the full {money(refund.amount)}. Approval keeps the current
            transfer deadline.
          </p>
          <div className="mt-4">{reviewButtons}</div>
          {rejecting && (
            <form
              className="mt-4 space-y-3 border-t border-neutral-100 pt-4"
              onSubmit={(event) => {
                event.preventDefault();
                void action(() => refundApi.review(refund.id, false, reason.trim()));
              }}
            >
              <label className="block text-xs font-medium">
                Reason visible to the customer
                <textarea
                  required
                  rows={3}
                  maxLength={1000}
                  disabled={busy}
                  className="mt-2 w-full rounded-lg border border-neutral-200 p-3 text-sm"
                  value={reason}
                  onChange={(event) => setReason(event.target.value)}
                />
              </label>
              <button
                disabled={busy || !reason.trim()}
                className="rounded-lg bg-red-700 px-4 py-2.5 text-xs font-semibold text-white disabled:opacity-50"
              >
                Confirm decline
              </button>
            </form>
          )}
        </section>
      )}
      {transferable && (
        <section className={card}>
          <h2 className="text-sm font-semibold">Transfer and record payment</h2>
          <p className="mt-2 text-sm text-neutral-500">
            Creating a QR does not transfer money. Transfer{" "}
            {money(refund.amount - refund.transferredAmount)} to the confirmed recipient, then
            complete the record below.
          </p>
          <div className="mt-5 grid items-start gap-5 lg:grid-cols-[240px_minmax(0,1fr)]">
            <div className="rounded-xl border border-neutral-200 bg-neutral-50 p-4 text-center">
              <p className="text-xs text-neutral-500">Remaining refund balance</p>
              <p className="mt-1 text-lg font-bold text-primary-700">
                {money(refund.amount - refund.transferredAmount)}
              </p>
              {currentQr ? (
                <>
                  <img
                    src={currentQr.imageUrl}
                    alt="Refund transfer QR"
                    className="mx-auto mt-3 w-full max-w-56 rounded-lg"
                  />
                  <p className="mt-2 break-all text-xs text-neutral-600">
                    {currentQr.transferContent}
                  </p>
                </>
              ) : (
                <div className="py-6 text-xs text-neutral-500">
                  <QrCode className="mx-auto mb-2" size={36} />
                  Generate QR to transfer the exact amount.
                </div>
              )}
              <button
                type="button"
                className={`${button} mt-3 w-full`}
                disabled={busy}
                onClick={() => void generateQr()}
              >
                {currentQr ? "Regenerate QR" : "Generate transfer QR"}
              </button>
            </div>
            <form
              className="min-w-0 space-y-4"
              onSubmit={(event) => {
                event.preventDefault();
                void action(async () => {
                  const images = await uploadRefundFiles(refund.orderId, proof, "transfer-proof");
                  return refundApi.transfer(
                    refund.id,
                    reference.trim(),
                    transferTime ? new Date(transferTime).toISOString() : undefined,
                    images[0]?.url,
                  );
                });
              }}
            >
              <h3 className="text-sm font-semibold">Record completed transfer</h3>
              <label className="block text-xs font-medium">
                Bank transfer reference
                <input
                  required
                  maxLength={150}
                  className="mt-2 w-full rounded-lg border border-neutral-200 p-3 text-sm"
                  disabled={busy}
                  value={reference}
                  onChange={(event) => setReference(event.target.value)}
                />
              </label>
              <label className="block text-xs font-medium">
                Transfer time (optional)
                <input
                  type="datetime-local"
                  className="mt-2 w-full rounded-lg border border-neutral-200 p-3 text-sm"
                  disabled={busy}
                  value={transferTime}
                  onChange={(event) => setTransferTime(event.target.value)}
                />
              </label>
              <div>
                <p className="mb-2 text-xs font-medium">
                  Transfer proof <span className="text-red-600">*</span>
                </p>
                <RefundFileUpload
                  purpose="transfer-proof"
                  max={1}
                  files={proof}
                  onChange={setProof}
                  disabled={busy}
                />
              </div>
              <button className={button} disabled={busy || !reference.trim() || proof.length < 1}>
                {busy
                  ? "Processing…"
                  : `Record full transfer · ${money(refund.amount - refund.transferredAmount)}`}
              </button>
            </form>
          </div>
        </section>
      )}
      {(refund.transferReference || refund.transferProofUrls.length > 0) && (
        <section className={card}>
          <h2 className="text-sm font-semibold">Recorded transfer</h2>
          <dl className="mt-4 grid gap-4 text-sm sm:grid-cols-2">
            <div>
              <dt className="text-xs text-neutral-500">Bank reference</dt>
              <dd className="mt-1 break-all font-medium">{refund.transferReference || "—"}</dd>
            </div>
            <div>
              <dt className="text-xs text-neutral-500">Transferred at</dt>
              <dd className="mt-1 font-medium">{time(refund.transferredAt)}</dd>
            </div>
          </dl>
          <RefundEvidenceGallery urls={refund.transferProofUrls} label="Transfer proof" />
        </section>
      )}
      <section className={card}>
        <h2 className="text-sm font-semibold">Processing history</h2>
        <p className="mt-1 text-xs text-neutral-500">Latest activity first · All times in GMT+7</p>
        <ol className="mt-5">
          {[...refund.history].reverse().map((entry) => (
            <li key={entry.id} className="relative border-l border-neutral-200 pb-5 pl-5 last:pb-0">
              <span
                className={`absolute -left-1.5 top-1 size-3 rounded-full ring-4 ring-white ${["Rejected", "DeadlineExpired"].includes(entry.decision) ? "bg-red-500" : "bg-primary-500"}`}
              />
              <p className="text-sm font-semibold">
                {historyLabels[entry.decision] || entry.decision}
              </p>
              <p className="mt-1 whitespace-pre-wrap break-words text-xs leading-relaxed text-neutral-600">
                {entry.note}
              </p>
              <p className="mt-1 text-xs text-neutral-500">
                {entry.actorName || entry.actorRole} ·{" "}
                {entry.actorRole === "StoreOwner" ? "Shop owner" : entry.actorRole} ·{" "}
                {time(entry.createdAt)}
              </p>
            </li>
          ))}
        </ol>
        {refund.history.length === 0 && (
          <p className="mt-3 text-sm text-neutral-500">No history recorded yet.</p>
        )}
      </section>
    </article>
  );
}
