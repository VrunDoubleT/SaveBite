import { Button, Input, MediaImage } from "@/shared/components/ui";
import { authInputStyle } from "@/shared/components/ui/inputStyles";
import { Package } from "lucide-react";
import { useState } from "react";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import type { Refund } from "../types/refund.types";
import { refundApi } from "../api/refundApi";
import { uploadRefundFiles } from "../api/uploadRefundFiles";
import { validateDistinctRefundPhotos } from "../utils/validateDistinctRefundPhotos";
import { RefundFileUpload } from "./RefundFileUpload";

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
      className="space-y-5"
      aria-label="Refund request"
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
        if (errors.reason || errors.evidence || errors.items) {
          const form = event.currentTarget;
          requestAnimationFrame(() => {
            const error = form.querySelector<HTMLElement>('[role="alert"]');
            error?.scrollIntoView({ block: "center", behavior: "instant" });
          });
          return;
        }
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
      <p
        role="note"
        className="rounded-md border border-accent-100 bg-accent-50 p-3 text-body text-accent-700"
      >
        Only one refund request is allowed per order. Select all affected items now. You cannot
        submit another request after withdrawal, rejection or expiry.
      </p>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-section font-semibold text-text-primary">Select affected items</h3>
        <p className="text-caption text-text-secondary">
          Choose quantity, describe the issue and add photos.
        </p>
      </div>
      {fieldErrors.items && (
        <p role="alert" className="text-body text-danger-strong">
          {fieldErrors.items}
        </p>
      )}
      <fieldset disabled={busy} className="min-w-0 space-y-4 disabled:opacity-60">
        {order.items.map((item, index) => {
          const id = item.orderItemId;
          const quantity = selected[id] || 0;
          const photoCount = evidence[id]?.length || 0;
          const reasonError = !!fieldErrors.reason && !(reasons[id] || "").trim();
          const photoError =
            !!fieldErrors.evidence && (photoCount < quantity || photoCount > quantity * 2);
          return (
            <section
              key={id}
              aria-label={`Refund item ${item.productName}`}
              className={`min-w-0 overflow-hidden rounded-lg border ${quantity ? "border-primary-200" : "border-border-default"}`}
            >
              <div
                className={`flex items-start gap-3 p-4 ${quantity ? "bg-primary-50/50" : "bg-bg-surface"}`}
              >
                <input
                  id={`refund-item-${index}`}
                  type="checkbox"
                  checked={!!quantity}
                  onChange={(event) =>
                    setSelected((old) => ({ ...old, [id]: event.target.checked ? 1 : 0 }))
                  }
                  className="mt-3 size-5 shrink-0 accent-primary-700"
                />
                <MediaImage
                  src={item.imageUrl}
                  alt=""
                  fallbackIcon={Package}
                  showFallbackLabel={false}
                  className="size-12 shrink-0 rounded-md"
                />
                <label
                  htmlFor={`refund-item-${index}`}
                  className="min-w-0 flex-1 cursor-pointer py-1"
                >
                  <span className="block text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
                    {item.productName}
                  </span>
                  {item.variantName && (
                    <span className="mt-1 block text-caption text-text-secondary [overflow-wrap:anywhere]">
                      {item.variantName}
                    </span>
                  )}
                  <span className="mt-1 block text-caption text-text-secondary">
                    Purchased: {item.quantity} · {money(item.unitPrice)} each
                  </span>
                </label>
              </div>
              {!!quantity && (
                <div className="space-y-4 border-t border-border-default p-4 sm:p-5">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <label className="flex items-center gap-3 text-body font-medium text-text-secondary">
                      Refund quantity
                      <Input
                        variant="auth"
                        aria-label={`Refund quantity for ${item.productName}`}
                        type="number"
                        min={1}
                        max={item.quantity}
                        step={1}
                        value={quantity}
                        className="!w-24 min-h-11"
                        onChange={(event) =>
                          setSelected((old) => ({
                            ...old,
                            [id]: Math.min(
                              item.quantity,
                              Math.max(1, Math.trunc(Number(event.target.value)) || 1),
                            ),
                          }))
                        }
                      />
                    </label>
                    <p className="text-body font-semibold tabular-nums text-primary-700">
                      {money(item.unitPrice * quantity)}
                    </p>
                  </div>
                  <div>
                    <div className="mb-2 flex items-center justify-between gap-2">
                      <label
                        htmlFor={`refund-reason-${index}`}
                        className="text-body font-medium text-text-primary"
                      >
                        Reason for refund <span className="text-danger-strong">*</span>
                      </label>
                      <span className="text-caption text-text-muted">
                        {(reasons[id] || "").length}/1000
                      </span>
                    </div>
                    <textarea
                      id={`refund-reason-${index}`}
                      required
                      maxLength={1000}
                      rows={3}
                      value={reasons[id] || ""}
                      aria-invalid={reasonError}
                      aria-describedby={reasonError ? `refund-reason-error-${index}` : undefined}
                      className={`${authInputStyle} resize-y text-body`}
                      onChange={(event) =>
                        setReasons((old) => ({ ...old, [id]: event.target.value }))
                      }
                      placeholder="Explain the issue with this product."
                    />
                    {reasonError && (
                      <p
                        id={`refund-reason-error-${index}`}
                        role="alert"
                        className="mt-1 text-caption text-danger-strong"
                      >
                        Enter a reason for this item.
                      </p>
                    )}
                  </div>
                  <div className="space-y-2">
                    <h4 className="text-body font-medium text-text-primary">
                      Evidence photos <span className="text-danger-strong">*</span>
                    </h4>
                    <p
                      className={`text-caption ${photoError ? "text-danger-strong" : "text-text-secondary"}`}
                    >
                      {photoCount} selected · Add {quantity}–{quantity * 2} different photos for{" "}
                      {quantity} portions.
                    </p>
                    <RefundFileUpload
                      purpose="request-evidence"
                      max={quantity * 2}
                      files={evidence[id] || []}
                      onChange={(files) => setEvidence((old) => ({ ...old, [id]: files }))}
                      disabled={busy}
                    />
                    {photoError && (
                      <p role="alert" className="text-caption text-danger-strong">
                        Add 1–2 different photos per refunded portion.
                      </p>
                    )}
                  </div>
                </div>
              )}
            </section>
          );
        })}
      </fieldset>
      <div className="sticky bottom-0 z-10 flex flex-wrap items-center justify-between gap-3 border-t border-border-default bg-bg-surface py-4">
        <div>
          <p className="text-caption text-text-secondary">
            Selected refund · {Object.values(selected).filter((quantity) => quantity > 0).length}{" "}
            items
          </p>
          <p className="text-section font-bold tabular-nums text-primary-700">
            {money(selectedAmount)}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" disabled={busy} onClick={onClose} className="min-h-11">
            Cancel
          </Button>
          <Button type="submit" loading={busy} disabled={busy} className="min-h-11">
            {busy ? "Submitting…" : "Submit refund request"}
          </Button>
        </div>
      </div>
    </form>
  );
}
