import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { RefundItems } from "./RefundItems";
import { RefundStatusBadge } from "./RefundStatusBadge";
import { RefundDestinationForm } from "./RefundDestinationForm";
import { refundApi } from "../api/refundApi";
import { isRefundActive as active } from "../utils/refundStatus";
import type { Refund } from "../types/refund.types";

const labels: Record<string, string> = {
  AwaitingCustomerDetails: "Bank details required",
  PendingVerification: "Waiting for shop review",
  AwaitingTransfer: "Waiting for bank transfer",
  Rejected: "Request declined",
  Transferred: "Refund transferred",
  Completed: "Refund completed",
  Withdrawn: "Request withdrawn",
};
export function RefundCard({
  refund,
  busy,
  mutate,
}: {
  refund: Refund;
  busy: boolean;
  mutate: (action: () => Promise<Refund>) => Promise<void>;
}) {
  const [withdraw, setWithdraw] = useState(false);
  const needsBank = refund.status === "AwaitingCustomerDetails";
  const canWithdraw =
    refund.type === "CustomerRequest" &&
    ["AwaitingCustomerDetails", "PendingVerification"].includes(refund.status);
  return (
    <article className="space-y-3 rounded-xl border border-neutral-200 p-4 sm:p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-semibold">
          {money(refund.amount)}{" "}
          <span className="ml-2 text-xs font-normal text-neutral-500">
            {refund.type === "CustomerRequest" ? "Your request" : "Shop cancellation refund"}
          </span>
        </p>
        <RefundStatusBadge status={refund.status} overdue={refund.isOverdue} />
      </div>

      <RefundItems refund={refund} />
      <p className="mt-2 text-xs text-neutral-600">{refund.reason}</p>
      {refund.rejectReason && (
        <p className="mt-2 text-xs text-red-700">
          {refund.history.some((entry) => entry.decision === "DeadlineExpired")
            ? "Closed by system"
            : "Shop decision"}
          : {refund.rejectReason}
        </p>
      )}
      {refund.type === "ShopCancellation" && active(refund) && (
        <p className="mt-2 text-xs text-neutral-500">
          The shop must return the collected amount. No approval is required; confirm your bank
          account to receive it.
        </p>
      )}
      {refund.status === "PendingVerification" && (
        <p className="mt-2 text-xs text-neutral-500">
          Bank account confirmed. The shop is reviewing your selected refund request.
        </p>
      )}
      {refund.status === "AwaitingTransfer" && (
        <p className="mt-2 text-xs text-neutral-500">
          Your refund is ready for manual bank transfer by the shop.
        </p>
      )}
      {refund.reviewDeadlineAt && active(refund) && (
        <p className={`mt-2 text-xs ${refund.isOverdue ? "text-amber-700" : "text-neutral-500"}`}>
          {refund.isOverdue
            ? "The processing deadline has passed. This refund will be closed automatically."
            : `Shop processing deadline: ${checkoutTime(refund.reviewDeadlineAt)} · GMT+7`}
        </p>
      )}
      {needsBank && <RefundDestinationForm refund={refund} busy={busy} mutate={mutate} />}
      {!needsBank && refund.bankName && (
        <p className="mt-3 text-xs text-neutral-500">
          {refund.bankName} · {refund.accountNumber} · {refund.accountHolder}
        </p>
      )}
      {refund.transferReference && (
        <p className="mt-2 text-xs text-neutral-600">
          Transfer reference: {refund.transferReference}
          {refund.transferredAt ? ` · ${checkoutTime(refund.transferredAt)}` : ""}
        </p>
      )}
      {canWithdraw && (
        <div className="mt-4 border-t border-neutral-100 pt-4">
          {withdraw ? (
            <div className="flex flex-wrap items-center gap-2 rounded-lg bg-amber-50 p-3 text-xs">
              <span>
                Withdraw this refund request? You cannot submit another request for this order.
              </span>
              <button
                type="button"
                disabled={busy}
                className="font-semibold text-red-700"
                onClick={() => {
                  void mutate(() => refundApi.withdraw(refund.id));
                }}
              >
                Yes, withdraw
              </button>
              <button type="button" disabled={busy} onClick={() => setWithdraw(false)}>
                Keep request
              </button>
            </div>
          ) : (
            <button
              type="button"
              disabled={busy}
              className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-semibold text-red-700 transition-colors hover:border-red-300 hover:bg-red-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600 disabled:opacity-50"
              onClick={() => setWithdraw(true)}
            >
              Withdraw request
            </button>
          )}
        </div>
      )}
      <details className="mt-3 border-t border-neutral-100 pt-3">
        <summary className="flex cursor-pointer items-center gap-1 text-xs font-medium text-neutral-600">
          Refund history and evidence <ChevronDown size={13} />
        </summary>
        <ul className="mt-3 space-y-3">
          {refund.history.map((entry) => (
            <li key={entry.id} className="border-l-2 border-neutral-100 pl-3 text-xs">
              <p className="font-medium">
                {labels[entry.toStatus] ?? entry.toStatus} ·{" "}
                {entry.actorRole === "StoreOwner"
                  ? "Shop owner"
                  : entry.actorRole === "Customer"
                    ? "You"
                    : entry.actorName || entry.actorRole}
              </p>
              <p className="mt-1 text-neutral-500">
                {entry.actorName} · {entry.decision}
              </p>
              <p className="mt-1 text-neutral-500">{entry.note}</p>
              <p className="mt-1 text-neutral-400">{checkoutTime(entry.createdAt)} · GMT+7</p>
            </li>
          ))}
        </ul>
        {[...refund.evidenceUrls, ...refund.transferProofUrls].map((url, index) => {
          try {
            if (new URL(url).protocol !== "https:") return null;
          } catch {
            return null;
          }
          return (
            <a
              key={`${url}:${index}`}
              href={url}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-3 mr-4 inline-block text-xs text-emerald-700 underline"
            >
              {index < refund.evidenceUrls.length ? "Request evidence" : "Transfer proof"}{" "}
              {index + 1}
            </a>
          );
        })}
      </details>
    </article>
  );
}
