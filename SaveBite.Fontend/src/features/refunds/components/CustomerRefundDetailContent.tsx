import { RefundItems } from "./RefundItems";
import { RefundEvidenceGallery as EvidenceGallery } from "./RefundEvidenceGallery";
import { useState } from "react";
import { CheckCircle2, CircleAlert, Clock3, X, XCircle } from "lucide-react";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { refundApi } from "../api/refundApi";
import type { Refund } from "../types/refund.types";
import { RefundDestinationForm } from "./RefundDestinationForm";

const decisions: Record<string, string> = {
  Requested: "Refund requested",
  DestinationProvided: "Bank details submitted",
  DestinationConfirmed: "Destination confirmed by email",
  Approved: "Shop approved the requested refund",
  Rejected: "Shop declined the request",
  Transferred: "Shop recorded the transfer",
  Withdrawn: "Request withdrawn",
  DeadlineExpired: "Processing deadline passed",
  VietQrGenerated: "Shop generated transfer QR",
  LegacySnapshot: "Imported refund record",
};
const card = "min-w-0 rounded-xl border border-neutral-200 bg-white p-4 sm:p-5";
const time = (value?: string) => (value ? `${checkoutTime(value)} · GMT+7` : "—");

export function CustomerRefundDetailContent({
  refund,
  busy,
  mutate,
}: {
  refund: Refund;
  busy: boolean;
  mutate: (action: () => Promise<Refund>) => Promise<void>;
}) {
  const [withdraw, setWithdraw] = useState(false);
  const rejected = refund.status === "Rejected";
  const paid = ["Transferred", "Completed"].includes(refund.status);
  const overdue = refund.isOverdue;
  const approved =
    refund.type === "CustomerRequest" &&
    (!!refund.history.find((item) => item.decision === "Approved") ||
      refund.status === "AwaitingTransfer" ||
      (paid && !!refund.reviewedAt));
  const required = refund.type === "ShopCancellation";
  const withdrawn = refund.status === "Withdrawn";
  const timeout = [...refund.history]
    .reverse()
    .find((item) => item.decision === "DeadlineExpired" && item.toStatus === "Rejected");
  const timedOut = rejected && !!timeout;
  const decision = [...refund.history]
    .reverse()
    .find((item) => ["Approved", "Rejected"].includes(item.decision));
  const tone =
    rejected || overdue
      ? "border-red-200 bg-red-50 text-red-800"
      : paid || approved || required
        ? "border-primary-200 bg-primary-50 text-primary-800"
        : withdrawn
          ? "border-neutral-200 bg-neutral-50 text-neutral-700"
          : "border-blue-200 bg-blue-50 text-blue-900";
  const title = rejected
    ? timedOut
      ? "Refund closed because the deadline passed"
      : "The shop declined your refund request"
    : overdue
      ? "Shop processing deadline passed"
      : withdrawn
        ? "You withdrew this request"
        : paid
          ? "The shop has recorded your refund transfer"
          : approved
            ? "The shop approved your requested refund"
            : required
              ? "The shop is required to refund this order"
              : refund.status === "PendingVerification"
                ? "The shop is reviewing your request"
                : "Confirm your refund destination to continue";
  const transfer = [...refund.history].reverse().find((item) => item.decision === "Transferred");
  const outcomeActor = timedOut ? "System" : paid ? transfer?.actorName : decision?.actorName;
  const outcomeAt = timedOut
    ? timeout?.createdAt
    : paid
      ? refund.transferredAt || transfer?.createdAt
      : refund.reviewedAt || decision?.createdAt;
  const outcomeLabel = rejected
    ? timedOut
      ? "Closure reason"
      : "Reason for declining"
    : overdue
      ? "Processing deadline"
      : paid
        ? "Recorded transfer"
        : approved
          ? "Approved amount"
          : "What happens next";
  const outcomeText = rejected
    ? refund.rejectReason || decision?.note || "No reason was recorded."
    : overdue
      ? "The processing deadline has passed. The system will close this refund automatically."
      : paid
        ? `${money(refund.transferredAmount)} recorded as transferred. See the reference and proof below.`
        : approved
          ? `${money(refund.amount)} approved in full. Waiting for the shop to record the bank transfer.`
          : withdrawn
            ? "The shop will no longer review or transfer money for this request."
            : required
              ? refund.destinationConfirmedAt
                ? "Your destination is confirmed. The shop must transfer the collected amount; no approval is required."
                : "Confirm your destination so the shop can transfer the required refund."
              : refund.status === "PendingVerification"
                ? "The shop has not made a decision yet. Follow updates in the processing history below."
                : "Provide your refund destination and confirm it by email.";
  const steps = required
    ? ["Refund destination", "Bank transfer", "Finished"]
    : ["Refund destination", "Shop review", "Bank transfer", "Finished"];
  const current = paid
    ? steps.length - 1
    : approved || (required && refund.destinationConfirmedAt)
      ? steps.length - 2
      : refund.destinationConfirmedAt
        ? 1
        : 0;
  const stopped = rejected || overdue || withdrawn;
  return (
    <div className="space-y-4">
      <RefundItems refund={refund} />
      <section
        aria-label="Refund outcome"
        className={`rounded-xl border p-4 text-left sm:p-5 ${tone}`}
      >
        <div className="flex items-start gap-3">
          {rejected ? (
            <XCircle className="mt-0.5 shrink-0" size={22} />
          ) : overdue ? (
            <CircleAlert className="mt-0.5 shrink-0" size={22} />
          ) : paid || approved ? (
            <CheckCircle2 className="mt-0.5 shrink-0" size={22} />
          ) : (
            <Clock3 className="mt-0.5 shrink-0" size={22} />
          )}
          <div className="min-w-0 flex-1">
            <h2 className="text-base font-semibold">{title}</h2>
            <div className="mt-3 space-y-3">
              <div>
                <p className="text-xs font-medium opacity-80">{outcomeLabel}</p>
                <p className="mt-1 whitespace-pre-wrap break-words text-sm font-semibold leading-relaxed">
                  {outcomeText}
                </p>
              </div>
              {(rejected || approved || paid) && (
                <div className="flex flex-col items-start gap-1 border-t border-current/15 pt-3 text-xs">
                  <span>
                    {outcomeActor
                      ? `${outcomeActor}${timedOut ? "" : " · Shop owner"}`
                      : refund.shopName}
                  </span>
                  <span>{time(outcomeAt)}</span>
                </div>
              )}
            </div>
            {overdue && approved && (
              <p className="mt-2 text-sm">
                The shop approved this request but did not finish recording the transfer before the
                deadline.
              </p>
            )}
            {refund.reviewDeadlineAt && !paid && !rejected && !withdrawn && (
              <p className="mt-3 text-xs font-medium">
                Shop processing deadline: {time(refund.reviewDeadlineAt)}
              </p>
            )}
          </div>
        </div>
      </section>
      <section aria-label="Refund progress" className={card}>
        <ol className="isolate flex items-start">
          {steps.map((step, index) => (
            <li
              key={step}
              aria-current={index === current ? "step" : undefined}
              className="relative flex min-w-0 flex-1 flex-col items-center gap-2 px-1 text-center"
            >
              {index < steps.length - 1 && (
                <span
                  aria-hidden="true"
                  className={`absolute left-1/2 top-4 -z-10 h-0.5 w-full ${index < current || paid ? "bg-primary-500" : "bg-neutral-200"}`}
                />
              )}
              <span
                className={`relative flex size-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold ring-4 ring-white ${index === current && stopped ? "bg-red-100 text-red-700" : index <= current ? "bg-primary-600 text-white" : "bg-neutral-100 text-neutral-400"}`}
              >
                {index === current && stopped ? (
                  <X size={15} />
                ) : index < current || paid ? (
                  "✓"
                ) : (
                  index + 1
                )}
              </span>
              <span
                className={`text-xs ${index === current ? "font-semibold text-neutral-900" : "text-neutral-500"}`}
              >
                {step}
              </span>
            </li>
          ))}
        </ol>
      </section>
      <div className="space-y-4">
        <section className={card}>
          <h2 className="mb-3 text-sm font-semibold">Refund amount</h2>
          <dl className="grid grid-cols-2 gap-3">
            <div className="rounded-lg bg-neutral-50 p-3">
              <dt className="text-xs text-neutral-500">Requested amount</dt>
              <dd className="mt-1 break-words text-lg font-bold">{money(refund.amount)}</dd>
            </div>
            <div className="rounded-lg bg-primary-50 p-3">
              <dt className="text-xs text-neutral-500">Recorded as transferred</dt>
              <dd className="mt-1 break-words text-lg font-bold text-primary-700">
                {money(refund.transferredAmount)}
              </dd>
            </div>
          </dl>
        </section>
        <section className={card}>
          <h2 className="text-sm font-semibold">
            {required ? "Why this refund is required" : "Your request"}
          </h2>
          <p className="mt-3 whitespace-pre-wrap break-words text-sm text-neutral-700">
            {refund.reason || "No additional reason provided."}
          </p>
          <p className="mt-2 text-xs text-neutral-500">
            {time(refund.createdAt)} · {required ? "Shop cancellation refund" : "Customer request"}
          </p>
          <EvidenceGallery
            urls={
              refund.items?.some((item) => item.evidenceUrls?.length) ? [] : refund.evidenceUrls
            }
            label="Request evidence"
          />
        </section>

        <section className={card}>
          <h2 className="text-sm font-semibold">Refund destination</h2>
          {refund.bankName ? (
            <dl className="mt-4 grid gap-x-8 gap-y-4 text-sm sm:grid-cols-2">
              {[
                ["Bank", refund.bankName],
                [
                  "Account number",
                  refund.accountNumber && `•••• ${refund.accountNumber.slice(-4)}`,
                ],
                ["Account holder", refund.accountHolder],
                [
                  "Email confirmation",
                  refund.destinationConfirmedAt
                    ? time(refund.destinationConfirmedAt)
                    : "Not confirmed yet",
                ],
              ].map(([label, value]) => (
                <div key={label} className="space-y-1">
                  <dt className="text-neutral-500">{label}</dt>
                  <dd className="break-words font-medium text-neutral-900">{value || "—"}</dd>
                </div>
              ))}
            </dl>
          ) : (
            <p className="mt-3 text-sm text-neutral-500">No refund destination provided yet.</p>
          )}
          {refund.status === "AwaitingCustomerDetails" && (
            <RefundDestinationForm refund={refund} busy={busy} mutate={mutate} />
          )}
        </section>
        {(refund.transferReference || refund.transferProofUrls.length > 0) && (
          <section className={card}>
            <h2 className="text-sm font-semibold">Transfer details</h2>
            <p className="mt-3 break-all text-sm font-medium">{refund.transferReference}</p>
            <p className="mt-1 text-xs text-neutral-500">{time(refund.transferredAt)}</p>
            <EvidenceGallery urls={refund.transferProofUrls} label="Transfer proof" />
          </section>
        )}
        <section className={card}>
          <h2 className="mb-4 text-sm font-semibold">Processing history</h2>
          <ol>
            {[...refund.history].reverse().map((entry) => (
              <li
                key={entry.id}
                className="relative border-l border-neutral-200 pb-5 pl-5 last:pb-0"
              >
                <span
                  className={`absolute -left-1.5 top-1 size-3 rounded-full ring-4 ring-white ${entry.decision === "Rejected" || entry.decision === "DeadlineExpired" ? "bg-red-500" : entry.actorRole === "Customer" ? "bg-neutral-300" : "bg-primary-600"}`}
                />
                <h3
                  className={`text-sm font-semibold ${entry.decision === "Rejected" ? "text-red-700" : "text-neutral-900"}`}
                >
                  {decisions[entry.decision] || entry.decision}
                </h3>
                {entry.note && (
                  <p className="mt-1 whitespace-pre-wrap break-words text-xs leading-relaxed text-neutral-600">
                    {entry.note}
                  </p>
                )}
                <p className="mt-1 text-xs text-neutral-500">
                  {entry.actorName || entry.actorRole} ·{" "}
                  {entry.actorRole === "StoreOwner"
                    ? "Shop owner"
                    : entry.actorRole === "Customer"
                      ? "Customer"
                      : entry.actorRole}{" "}
                  · {time(entry.createdAt)}
                </p>
                {entry.proofUrl && (
                  <EvidenceGallery urls={[entry.proofUrl]} label="Recorded proof" />
                )}
              </li>
            ))}
          </ol>
          {refund.history.length === 0 && (
            <p className="text-sm text-neutral-500">No history recorded yet.</p>
          )}
        </section>
        {refund.type === "CustomerRequest" &&
          ["AwaitingCustomerDetails", "PendingVerification"].includes(refund.status) && (
            <section className={card}>
              <h2 className="text-sm font-semibold">Withdraw request</h2>
              <p className="mt-2 text-xs leading-relaxed text-neutral-500">
                Withdrawing stops the shop from reviewing or transferring this refund.
              </p>
              {withdraw ? (
                <div className="mt-3 space-y-3 rounded-lg bg-red-50 p-3">
                  <p className="text-xs text-red-800">Withdraw this refund request?</p>
                  <div className="flex flex-wrap gap-3">
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => void mutate(() => refundApi.withdraw(refund.id))}
                      className="text-xs font-semibold text-red-700 disabled:opacity-50"
                    >
                      Yes, withdraw
                    </button>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => setWithdraw(false)}
                      className="text-xs text-neutral-600"
                    >
                      Keep request
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => setWithdraw(true)}
                  className="mt-3 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-semibold text-red-700 hover:bg-red-100 disabled:opacity-50"
                >
                  Withdraw request
                </button>
              )}
            </section>
          )}
      </div>
      <p className="break-all border-t border-neutral-200 pt-4 text-xs text-neutral-400">
        Refund ID: {refund.id}
      </p>
      <Link
        to={APP_PATHS.ORDER_DETAIL.replace(":orderId", refund.orderId)}
        className="inline-block text-xs font-medium text-primary-700 hover:underline"
      >
        View associated order
      </Link>
    </div>
  );
}
