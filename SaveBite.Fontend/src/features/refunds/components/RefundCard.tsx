import { CustomerActionNativeButton } from "@/shared/components/CustomerMutationControls";
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
  CustomerDetailsExpired: "Customer details deadline passed",
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
    <article className="space-y-3 rounded-lg border border-border-default p-4 sm:p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-body font-semibold">
          {money(refund.amount)}{" "}
          <span className="ml-2 text-caption font-normal text-text-secondary">
            {refund.type === "CustomerRequest" ? "Your request" : "Shop cancellation refund"}
          </span>
        </p>
        <RefundStatusBadge status={refund.status} overdue={refund.isOverdue} />
      </div>

      <RefundItems refund={refund} />
      <p className="mt-2 text-caption text-text-secondary">{refund.reason}</p>
      {refund.rejectReason && (
        <p className="mt-2 text-caption text-danger-strong">
          {refund.history.some((entry) => ["DeadlineExpired", "CustomerDetailsExpired"].includes(entry.decision))
            ? "Closed by system"
            : "Shop decision"}
          : {refund.rejectReason}
        </p>
      )}
      {refund.type === "ShopCancellation" && active(refund) && (
        <p className="mt-2 text-caption text-text-secondary">
          The shop must return the collected amount. No approval is required; confirm your bank
          account to receive it.
        </p>
      )}
      {refund.status === "PendingVerification" && (
        <p className="mt-2 text-caption text-text-secondary">
          Bank account confirmed. The shop is reviewing your selected refund request.
        </p>
      )}
      {refund.status === "AwaitingTransfer" && (
        <p className="mt-2 text-caption text-text-secondary">
          Your refund is ready for manual bank transfer by the shop.
        </p>
      )}
      {refund.reviewDeadlineAt && active(refund) && (
        <p
          className={`mt-2 text-caption ${refund.isOverdue ? "text-accent-700" : "text-text-secondary"}`}
        >
          {refund.isOverdue
            ? "The processing deadline has passed. This refund will be closed automatically."
            : `Shop processing deadline: ${checkoutTime(refund.reviewDeadlineAt)}`}
        </p>
      )}
      {needsBank && <RefundDestinationForm refund={refund} busy={busy} mutate={mutate} />}
      {!needsBank && refund.bankName && (
        <p className="mt-3 text-caption text-text-secondary">
          {refund.bankName} · {refund.accountNumber} · {refund.accountHolder}
        </p>
      )}
      {refund.transferReference && (
        <p className="mt-2 text-caption text-text-secondary">
          Transfer reference: {refund.transferReference}
          {refund.transferredAt ? ` · ${checkoutTime(refund.transferredAt)}` : ""}
        </p>
      )}
      {canWithdraw && (
        <div className="mt-4 border-t border-border-default pt-4">
          {withdraw ? (
            <div className="flex flex-wrap items-center gap-2 rounded-lg bg-accent-50 p-3 text-caption">
              <span>
                Withdraw this refund request? You cannot submit another request for this order.
              </span>
              <CustomerActionNativeButton
                type="button"
                disabled={busy}
                className="font-semibold text-danger-strong"
                onClick={() => {
                  void mutate(() => refundApi.withdraw(refund.id));
                }}
              >
                Yes, withdraw
              </CustomerActionNativeButton>
              <button type="button" disabled={busy} onClick={() => setWithdraw(false)}>
                Keep request
              </button>
            </div>
          ) : (
            <CustomerActionNativeButton
              type="button"
              disabled={busy}
              className="rounded-lg border border-danger-soft bg-danger-soft px-3 py-2 text-caption font-semibold text-danger-strong transition-colors hover:border-danger-soft hover:bg-danger-soft focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-danger-strong disabled:opacity-50"
              onClick={() => setWithdraw(true)}
            >
              Withdraw request
            </CustomerActionNativeButton>
          )}
        </div>
      )}
      <details className="mt-3 border-t border-border-default pt-3">
        <summary className="flex cursor-pointer items-center gap-1 text-caption font-medium text-text-secondary">
          Refund history and evidence <ChevronDown size={13} />
        </summary>
        <ul className="mt-3 space-y-3">
          {refund.history.map((entry) => (
            <li key={entry.id} className="border-l-2 border-border-default pl-3 text-caption">
              <p className="font-medium">
                {labels[entry.toStatus] ?? entry.toStatus} ·{" "}
                {entry.actorRole === "StoreOwner"
                  ? "Shop owner"
                  : entry.actorRole === "Customer"
                    ? "You"
                    : entry.actorName || entry.actorRole}
              </p>
              <p className="mt-1 text-text-secondary">
                {entry.actorName} · {entry.decision}
              </p>
              <p className="mt-1 text-text-secondary">{entry.note}</p>
              <p className="mt-1 text-text-muted">{checkoutTime(entry.createdAt)}</p>
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
              className="mt-3 mr-4 inline-block text-caption text-primary-700 underline"
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
