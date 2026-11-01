import { useState } from "react";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import type { Refund } from "../types/refund.types";
import { refundApi } from "../api/refundApi";
import { uploadRefundFiles } from "../api/uploadRefundFiles";
import { validateDistinctRefundPhotos } from "../utils/validateDistinctRefundPhotos";
import { RefundFileUpload } from "./RefundFileUpload";

const input =
  "mt-1 w-full rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm disabled:bg-neutral-50";
const button =
  "rounded-lg bg-emerald-700 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-800 disabled:opacity-50";

// Keep request drafts local to the form; upload only after all selected items pass validation.
export function RefundRequestForm({
  order,
  busy,
  mutate,
  onClose,
}: {
  order: CheckoutOrder;
  busy: boolean;
  mutate: (action: () => Promise<Refund>) => Promise<void>;
  onClose: () => void;
}) {
  const [selected, setSelected] = useState<Record<string, number>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [fieldErrors, setFieldErrors] = useState<{
    reason?: string;
    evidence?: string;
    items?: string;
  }>({});
  const [evidence, setEvidence] = useState<Record<string, File[]>>({});
  const selectedAmount = order.items.reduce(
    (total, item) => total + item.unitPrice * (selected[item.orderItemId] || 0),
    0,
  );
  return (
    <form
      noValidate
      className="mt-4 space-y-3 rounded-lg border border-neutral-200 bg-neutral-50 p-3"
      onSubmit={(event) => {
        event.preventDefault();
        const errors = {
          items: Object.values(selected).some((quantity) => quantity > 0)
            ? undefined
            : "Select at least one item and quantity.",
          reason: Object.entries(selected).some(
            ([id, quantity]) =>
              quantity > 0 &&
              (!(reasons[id] || "").trim() || (reasons[id] || "").trim().length > 1000),
          )
            ? "Enter a reason for each selected item (up to 1,000 characters)."
            : undefined,
          evidence: Object.entries(selected).some(
            ([id, quantity]) =>
              quantity > 0 &&
              ((evidence[id]?.length || 0) < quantity ||
                (evidence[id]?.length || 0) > quantity * 2),
          )
            ? "Each selected item needs 1–2 different photos per refunded portion."
            : undefined,
        };
        setFieldErrors(errors);
        if (errors.reason || errors.evidence || errors.items) return;
        void mutate(async () => {
          const choices = Object.entries(selected).filter(([, quantity]) => quantity > 0);
          await validateDistinctRefundPhotos(choices.flatMap(([id]) => evidence[id] || []));
          const items = [];
          for (const [orderItemId, quantity] of choices) {
            const images = await uploadRefundFiles(
              order.id,
              evidence[orderItemId] || [],
              "request-evidence",
            );
            items.push({
              orderItemId,
              quantity,
              reason: reasons[orderItemId].trim(),
              evidenceUrls: images.map((image) => image.url),
            });
          }
          return refundApi.request(order.id, items);
        });
      }}
    >
      <p className="text-sm font-semibold">Selected refund · {money(selectedAmount)}</p>
      <p
        role="note"
        className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-xs font-medium text-amber-900"
      >
        Only one refund request is allowed per order. Select all affected items now. You cannot
        submit another request after withdrawal, rejection or expiry.
      </p>
      <div className="space-y-2">
        {order.items.map((item) => (
          <label
            key={item.orderItemId}
            className="flex items-center gap-3 rounded-lg border border-neutral-200 bg-white p-3 text-sm"
          >
            <input
              type="checkbox"
              disabled={busy}
              checked={!!selected[item.orderItemId]}
              onChange={(event) =>
                setSelected((old) => ({ ...old, [item.orderItemId]: event.target.checked ? 1 : 0 }))
              }
            />
            <span className="min-w-0 flex-1">
              {item.productName}
              <span className="block text-xs text-neutral-500">
                {item.variantName} · Purchased: {item.quantity} · {money(item.unitPrice)} each
              </span>
            </span>
            {!!selected[item.orderItemId] && (
              <input
                aria-label={`Refund quantity for ${item.productName}`}
                type="number"
                min={1}
                max={item.quantity}
                disabled={busy}
                value={selected[item.orderItemId]}
                className="w-16 rounded border border-neutral-300 p-2"
                onChange={(event) =>
                  setSelected((old) => ({
                    ...old,
                    [item.orderItemId]: Math.min(
                      item.quantity,
                      Math.max(1, Number(event.target.value) || 1),
                    ),
                  }))
                }
              />
            )}
          </label>
        ))}
      </div>
      {fieldErrors.items && (
        <p role="alert" className="text-xs text-red-600">
          {fieldErrors.items}
        </p>
      )}

      {order.items
        .filter((item) => selected[item.orderItemId] > 0)
        .map((item) => (
          <div key={item.orderItemId} className="rounded-lg border border-neutral-200 bg-white p-3">
            <h4 className="text-sm font-semibold">{item.productName}</h4>
            <label className="mt-3 block text-xs font-medium">
              Reason for refund <span className="text-red-600">*</span>
              <textarea
                required
                maxLength={1000}
                rows={3}
                value={reasons[item.orderItemId] || ""}
                disabled={busy}
                aria-invalid={!!fieldErrors.reason && !(reasons[item.orderItemId] || "").trim()}
                className={`${input} ${fieldErrors.reason && !(reasons[item.orderItemId] || "").trim() ? "border-red-500" : ""}`}
                onChange={(event) =>
                  setReasons((old) => ({ ...old, [item.orderItemId]: event.target.value }))
                }
                placeholder="Explain the issue with this product."
              />
              {fieldErrors.reason && !(reasons[item.orderItemId] || "").trim() && (
                <p role="alert" className="mt-1 text-xs text-red-600">
                  Enter a reason for this item.
                </p>
              )}
            </label>
            <p className="mt-3 text-xs font-medium">Evidence photos</p>
            <p
              className={`my-2 text-xs ${(evidence[item.orderItemId]?.length || 0) < selected[item.orderItemId] || (evidence[item.orderItemId]?.length || 0) > selected[item.orderItemId] * 2 ? "text-red-600" : "text-emerald-700"}`}
            >
              {evidence[item.orderItemId]?.length || 0} photos selected ·{" "}
              {selected[item.orderItemId]}–{selected[item.orderItemId] * 2} different photos
              required for {selected[item.orderItemId]} portions.
            </p>
            <RefundFileUpload
              purpose="request-evidence"
              max={selected[item.orderItemId] * 2}
              files={evidence[item.orderItemId] || []}
              onChange={(files) => {
                setEvidence((old) => ({ ...old, [item.orderItemId]: files }));
                setFieldErrors((old) => ({ ...old, evidence: undefined }));
              }}
              disabled={busy}
            />
          </div>
        ))}
      {fieldErrors.evidence && (
        <p role="alert" className="text-xs text-red-600">
          {fieldErrors.evidence}
        </p>
      )}
      <div className="flex gap-2">
        <button className={button} disabled={busy}>
          {busy ? "Submitting…" : "Submit refund request"}
        </button>
        <button
          type="button"
          className="px-3 py-2 text-xs text-neutral-600"
          disabled={busy}
          onClick={() => onClose()}
        >
          Close
        </button>
      </div>
    </form>
  );
}
