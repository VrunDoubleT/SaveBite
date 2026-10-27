import { RefundProgress } from "./RefundStatusBadge";
import { RefundStatusBadge } from "./RefundStatusBadge";
import { RefundDestinationForm } from "./RefundDestinationForm";
import { uploadRefundFiles } from "../api/uploadRefundFiles";
import { RefundFileUpload } from "./RefundFileUpload";
import { useEffect, useRef, useState } from "react";
import { RotateCcw, ChevronDown } from "lucide-react";
import { getApiErrorMessage } from "@/shared/api";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { subscribeCustomerOrderEvents } from "@/features/orders/realtime/subscribeCustomerOrderEvents";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import { refundApi } from "../api/refundApi";
import type { Refund } from "../types/refund.types";

const active = (refund: Refund) => ["AwaitingCustomerDetails", "PendingVerification", "AwaitingTransfer", "Overdue"].includes(refund.status);
const labels: Record<string, string> = {
  Overdue: "Overdue · Admin required", AwaitingCustomerDetails: "Bank details required", PendingVerification: "Waiting for shop review",
  AwaitingTransfer: "Waiting for bank transfer", Rejected: "Request declined", Transferred: "Refund transferred",
  Completed: "Refund completed", Withdrawn: "Request withdrawn",
};
const input = "mt-1 w-full rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm disabled:bg-neutral-50";
const button = "rounded-lg bg-emerald-700 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-800 disabled:opacity-50";

export function CustomerOrderRefunds({ order }: { order: CheckoutOrder }) {
  const [refunds, setRefunds] = useState<Refund[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [fieldErrors, setFieldErrors] = useState<{ reason?: string; evidence?: string }>({});
  const [evidence, setEvidence] = useState<File[]>([]);
  const [now, setNow] = useState(Date.now);
  const operation = useRef(false);
  const revision = useRef(0);
  const alive = useRef(true);
  const realtime = useRealtimeSubscriptions();
  useEffect(() => {
    alive.current = true;
    const abort = new AbortController();
    let reading = false;
    let dirty = false;
    async function refresh() {
      dirty = true;
      if (reading) return;
      reading = true;
      try {
        while (dirty && !abort.signal.aborted) {
          dirty = false;
          const current = revision.current;
          const items = await refundApi.forOrder(order.id, abort.signal);
          if (!abort.signal.aborted && !operation.current && current === revision.current) { setRefunds(items); setLoaded(true); }
        }
      } catch (e) { if (!abort.signal.aborted) setError(getApiErrorMessage(e)); }
      finally { reading = false; }
    }
    void refresh();
    const off = subscribeCustomerOrderEvents(realtime, () => { void refresh(); }, order.id);
    const timer = window.setInterval(() => setNow(Date.now()), 30_000);
    return () => { alive.current = false; abort.abort(); off(); window.clearInterval(timer); };
  }, [order.id, realtime]);

  async function mutate(action: () => Promise<Refund>) {
    if (operation.current) return;
    operation.current = true; revision.current++; setBusy(true); setError("");
    try {
      const changed = await action();
      if (!alive.current) return;
      setRefunds((items) => [changed, ...items.filter((item) => item.id !== changed.id)]);
      setLoaded(true); setOpen(false); setReason(""); setEvidence([]);
    } catch (e) { if (alive.current) setError(getApiErrorMessage(e)); }
    finally { operation.current = false; if (alive.current) setBusy(false); }
  }
  const completed = order.status === "Completed";
  const deadline = order.completedAt ? Date.parse(order.completedAt) + 6 * 60 * 60 * 1000 : null;
  const remaining = Math.max(0, (order.paidAmount ?? 0) - refunds.reduce((total, refund) => total + refund.transferredAmount, 0));
  const hasActive = refunds.some(active);
  const eligible = loaded && completed && deadline !== null && now <= deadline && remaining > 0 && !hasActive;
  if (!completed && !refunds.length && !error) return null;

  return (
    <section aria-label="Order refunds" className="rounded-xl border border-neutral-200 bg-white p-4 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="flex items-center gap-2 text-sm font-semibold text-neutral-900"><RotateCcw size={16} />Refunds</h2>
          {completed && <p className="mt-1 text-xs text-neutral-500">Request a full refund within 6 hours of order completion.</p>}
        </div>
        {eligible && <button type="button" className={button} disabled={busy} onClick={() => setOpen((value) => !value)}>Request refund</button>}
      </div>
      {error && <p role="alert" className="mt-3 rounded-lg bg-red-50 p-3 text-xs text-red-700">{error}</p>}
      {!loaded && !error && <p className="mt-3 text-xs text-neutral-500">Loading refund eligibility…</p>}
      {completed && deadline !== null && <p className="mt-2 text-xs text-neutral-500">Request deadline: {checkoutTime(new Date(deadline).toISOString())} · GMT+7</p>}
      {loaded && completed && !hasActive && !eligible && <p className="mt-2 text-xs text-neutral-600">{remaining <= 0 ? "There is no paid balance left to refund." : deadline === null ? "The completion time is unavailable. Contact support to check refund eligibility." : "The 6-hour refund request window has closed."}</p>}
      {open && eligible && (
        <form noValidate className="mt-4 space-y-3 rounded-lg border border-neutral-200 bg-neutral-50 p-3" onSubmit={(event) => {
          event.preventDefault();
          const errors = {
            reason: !reason.trim() ? "Enter a reason for your refund request." : reason.trim().length > 1000 ? "Use no more than 1,000 characters." : undefined,
            evidence: evidence.length < 1 || evidence.length > 10 ? "Choose between 1 and 10 evidence images." : undefined,
          };
          setFieldErrors(errors);
          if (errors.reason || errors.evidence) return;
          void mutate(async () => {
            const images = await uploadRefundFiles(order.id, evidence, "request-evidence");
            return refundApi.request(order.id, reason.trim(), images.map((image) => image.url));
          });
        }}>
          <p className="text-sm font-semibold">Full refund request · {money(remaining)}</p>
          <p className="text-xs text-neutral-500">The shop will review your request after you confirm your bank details by email. Partial refunds are not available.</p>
          <label className="block text-xs font-medium">Reason<textarea required maxLength={1000} rows={3} aria-invalid={!!fieldErrors.reason} aria-describedby={fieldErrors.reason ? "refund-reason-error" : undefined} className={`${input} ${fieldErrors.reason ? "border-red-500" : ""}`} value={reason} disabled={busy} onChange={(event) => { setReason(event.target.value); setFieldErrors((old) => ({ ...old, reason: undefined })); }} />{fieldErrors.reason && <p id="refund-reason-error" role="alert" className="mt-1 text-xs text-red-600">{fieldErrors.reason}</p>}</label>
          <div><p className="mb-2 text-xs font-medium">Evidence photos <span className="font-normal text-neutral-400">(1–10 images required)</span></p><RefundFileUpload purpose="request-evidence" max={10} files={evidence} onChange={(files) => { setEvidence(files); setFieldErrors((old) => ({ ...old, evidence: undefined })); }} disabled={busy} />{fieldErrors.evidence && <p role="alert" className="mt-1 text-xs text-red-600">{fieldErrors.evidence}</p>}</div>
          <div className="flex gap-2"><button className={button} disabled={busy}>{busy ? "Submitting…" : "Submit refund request"}</button><button type="button" className="px-3 py-2 text-xs text-neutral-600" disabled={busy} onClick={() => setOpen(false)}>Close</button></div>
        </form>
      )}
      <div className="mt-3 space-y-3">{refunds.map((refund) => <RefundCard key={refund.id} refund={refund} busy={busy} mutate={mutate} />)}</div>
    </section>
  );
}

export function RefundCard({ refund, busy, mutate, detailed = false }: { detailed?: boolean; refund: Refund; busy: boolean; mutate: (action: () => Promise<Refund>) => Promise<void> }) {
  const [withdraw, setWithdraw] = useState(false);
  const needsBank = refund.status === "AwaitingCustomerDetails";
  const canWithdraw = ["AwaitingCustomerDetails", "PendingVerification"].includes(refund.status);
  return (
    <article className="space-y-3 rounded-xl border border-neutral-200 p-4 sm:p-5">
      <div className="flex flex-wrap items-center justify-between gap-2"><p className="text-sm font-semibold">{money(refund.amount)} <span className="ml-2 text-xs font-normal text-neutral-500">{refund.type === "CustomerRequest" ? "Your request" : "Shop cancellation refund"}</span></p><RefundStatusBadge status={refund.status} overdue={refund.isOverdue} /></div>
      {detailed && <><RefundProgress refund={refund} /><dl className="grid gap-3 rounded-lg bg-neutral-50 p-3 text-xs sm:grid-cols-3"><div><dt className="text-neutral-500">Refund type</dt><dd className="mt-1 font-semibold">{refund.type === "CustomerRequest" ? "Customer request" : "Required shop refund"}</dd></div><div><dt className="text-neutral-500">Transferred amount</dt><dd className="mt-1 font-semibold text-emerald-700">{money(refund.transferredAmount)}</dd></div><div><dt className="text-neutral-500">Destination confirmed</dt><dd className="mt-1 font-semibold">{refund.destinationConfirmedAt ? `${checkoutTime(refund.destinationConfirmedAt)} · GMT+7` : "Not confirmed yet"}</dd></div></dl></>}
      <p className="mt-2 text-xs text-neutral-600">{refund.reason}</p>
      {refund.rejectReason && <p className="mt-2 text-xs text-red-700">{refund.history.some((entry) => entry.decision === "DeadlineExpired") ? "Closed by system" : "Shop decision"}: {refund.rejectReason}</p>}
      {refund.type === "ShopCancellation" && active(refund) && <p className="mt-2 text-xs text-neutral-500">The shop must return the collected amount. No approval is required; confirm your bank account to receive it.</p>}
      {refund.status === "PendingVerification" && <p className="mt-2 text-xs text-neutral-500">Bank account confirmed. The shop is reviewing your full refund request.</p>}
      {refund.status === "AwaitingTransfer" && <p className="mt-2 text-xs text-neutral-500">Your refund is ready for manual bank transfer by the shop.</p>}
      {refund.reviewDeadlineAt && active(refund) && <p className={`mt-2 text-xs ${refund.isOverdue ? "text-amber-700" : "text-neutral-500"}`}>{refund.isOverdue ? "The processing deadline has passed. This refund will be closed automatically." : `Shop processing deadline: ${checkoutTime(refund.reviewDeadlineAt)} · GMT+7`}</p>}
      {needsBank && <RefundDestinationForm refund={refund} busy={busy} mutate={mutate} />}
      {!needsBank && refund.bankName && <p className="mt-3 text-xs text-neutral-500">{refund.bankName} · {refund.accountNumber} · {refund.accountHolder}</p>}
      {refund.transferReference && <p className="mt-2 text-xs text-neutral-600">Transfer reference: {refund.transferReference}{refund.transferredAt ? ` · ${checkoutTime(refund.transferredAt)}` : ""}</p>}
      {canWithdraw && <div className="mt-4 border-t border-neutral-100 pt-4">{withdraw ? <div className="flex flex-wrap items-center gap-2 rounded-lg bg-amber-50 p-3 text-xs"><span>Withdraw this refund request? The shop will no longer process it.</span><button type="button" disabled={busy} className="font-semibold text-red-700" onClick={() => { void mutate(() => refundApi.withdraw(refund.id)); }}>Yes, withdraw</button><button type="button" disabled={busy} onClick={() => setWithdraw(false)}>Keep request</button></div> : <button type="button" disabled={busy} className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-semibold text-red-700 transition-colors hover:border-red-300 hover:bg-red-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600 disabled:opacity-50" onClick={() => setWithdraw(true)}>Withdraw request</button>}</div>}
      <details open={detailed || undefined} className="mt-3 border-t border-neutral-100 pt-3"><summary className="flex cursor-pointer items-center gap-1 text-xs font-medium text-neutral-600">Refund history and evidence <ChevronDown size={13} /></summary><ul className="mt-3 space-y-3">{refund.history.map((entry) => <li key={entry.id} className="border-l-2 border-neutral-100 pl-3 text-xs"><p className="font-medium">{labels[entry.toStatus] ?? entry.toStatus} · {entry.actorRole === "StoreOwner" ? "Shop owner" : entry.actorRole === "Customer" ? "You" : entry.actorName || entry.actorRole}</p><p className="mt-1 text-neutral-500">{entry.actorName} · {entry.decision}</p><p className="mt-1 text-neutral-500">{entry.note}</p><p className="mt-1 text-neutral-400">{checkoutTime(entry.createdAt)} · GMT+7</p></li>)}</ul>{[...refund.evidenceUrls, ...refund.transferProofUrls].map((url, index) => { try { if (new URL(url).protocol !== "https:") return null; } catch { return null; } return <a key={`${url}:${index}`} href={url} target="_blank" rel="noopener noreferrer" className="mt-3 mr-4 inline-block text-xs text-emerald-700 underline">{detailed && <img src={url} alt={index < refund.evidenceUrls.length ? "Request evidence" : "Transfer proof"} loading="lazy" className="mb-2 size-24 rounded-lg border border-neutral-200 object-cover" />}{index < refund.evidenceUrls.length ? "Request evidence" : "Transfer proof"} {index + 1}</a>; })}</details>
    </article>
  );
}
