import { UploadDraftChoice } from "@/shared/uploads/UploadDraftChoice";
import { UploadTaskGuard } from "@/shared/uploads/UploadTaskGuard";
import {
  CustomerMutationForm,
  CustomerActionButton,
} from "@/shared/components/CustomerMutationControls";
import { Button, Input, MediaImage } from "@/shared/components/ui";
import { authInputStyle } from "@/shared/components/ui/inputStyles";
import { Package } from "lucide-react";
import { useEffect, useState } from "react";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { formatCartMoney as money } from "@/features/cart/utils/formatCartMoney";
import type { Refund } from "../types/refund.types";
import { refundApi } from "../api/refundApi";
import { validateDistinctRefundPhotos } from "../utils/validateDistinctRefundPhotos";
import { RefundFileUpload } from "./RefundFileUpload";
import { useLocation } from "react-router-dom";
import { useAuthStore } from "@/shared/stores/authStore";
import {
  getUploadDraft,
  releaseUploadDraft,
  removeUploadDraft,
} from "@/shared/uploads/uploadDraftStore";
import { startOrResumeUploadDraft } from "@/shared/uploads/uploadDrafts";

// Keep request drafts local to the form; upload only after all selected items pass validation.
function RefundRequestFormContent({
  order,
  busy,
  mutate,
  onClose,
  onQueued,
}: {
  order: CheckoutOrder;
  busy: boolean;
  mutate: (action: () => Promise<Refund>) => Promise<void>;
  onClose: () => void;
  onQueued: () => void;
}) {
  const [selected, setSelected] = useState<Record<string, number>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [fieldErrors, setFieldErrors] = useState<{
    reason?: string;
    evidence?: string;
    items?: string;
  }>({});
  const [evidence, setEvidence] = useState<Record<string, File[]>>({});
  const [activeDraftId, setActiveDraftId] = useState<string>();
  const location = useLocation();
  const userId = useAuthStore((state) => state.user?.id);
  useEffect(() => {
    const draftId = (location.state as { uploadDraftId?: string } | null)?.uploadDraftId;
    if (!draftId || !userId) return;
    let active = true;
    void getUploadDraft(draftId).then((draft) => {
      if (!active || !draft || draft.userId !== userId) return;
      const saved = draft.formData as {
        orderId?: string;
        selected?: Record<string, number>;
        reasons?: Record<string, string>;
        evidence?: Record<string, File[]>;
      };
      if (saved.orderId !== order.id) return;
      setActiveDraftId(draft.id);
      setSelected(saved.selected ?? {});
      setReasons(saved.reasons ?? {});
      setEvidence(saved.evidence ?? {});
    });
    return () => {
      active = false;
    };
  }, [location.state, order.id, userId]);
  const selectedAmount = order.items.reduce(
    (total, item) => total + item.unitPrice * (selected[item.orderItemId] || 0),
    0,
  );
  return (
    <CustomerMutationForm
      noValidate
      className="min-w-0 space-y-5"
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
          if (!userId) throw new Error("Sign in before uploading refund evidence.");
          const files = choices.flatMap(([orderItemId]) =>
            (evidence[orderItemId] ?? []).map((file) => ({
              field: `evidence:${orderItemId}`,
              file,
            })),
          );
          const draftId = activeDraftId ?? crypto.randomUUID();
          setActiveDraftId(draftId);
          try {
            const draft = await startOrResumeUploadDraft(
              {
                draftId,
                userId,
                keepActiveUntilSubmitted: true,
                onQueued,
                purpose: "refund:request-evidence",
                targetId: order.id,
                title: `Refund request for order ${order.orderCode}`,
                path: location.pathname,
                formData: { orderId: order.id, selected, reasons, evidence },
                files,
              },
              draftId,
            );
            setActiveDraftId(draft.id);
            const items = choices.map(([orderItemId, quantity]) => {
              const evidenceUrls = draft.files
                .filter((file) => file.field === `evidence:${orderItemId}`)
                .map((file) => file.url)
                .filter((url): url is string => Boolean(url));
              if (evidenceUrls.length !== (evidence[orderItemId]?.length ?? 0))
                throw new Error("Some evidence photos are not uploaded yet. Retry the upload.");
              return {
                orderItemId,
                quantity,
                reason: reasons[orderItemId].trim(),
                evidenceUrls,
              };
            });
            const result = await refundApi.request(order.id, items);
            await removeUploadDraft(draft.id);
            setActiveDraftId(undefined);
            return result;
          } finally {
            releaseUploadDraft(draftId);
          }
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
                    <label className="flex flex-wrap items-center gap-3 text-body font-medium text-text-secondary">
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
      <div className="sticky bottom-0 z-10 flex flex-col gap-3 border-t border-border-default bg-bg-surface py-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-caption text-text-secondary">
            Selected refund · {Object.values(selected).filter((quantity) => quantity > 0).length}{" "}
            items
          </p>
          <p className="text-section font-bold tabular-nums text-primary-700">
            {money(selectedAmount)}
          </p>
        </div>
        <div className="flex flex-wrap gap-2 [&>button]:flex-1 sm:[&>button]:flex-none">
          <Button variant="secondary" disabled={busy} onClick={onClose} className="min-h-11">
            Cancel
          </Button>
          <CustomerActionButton type="submit" loading={busy} disabled={busy} className="min-h-11">
            {busy ? "Submitting…" : "Submit refund request"}
          </CustomerActionButton>
        </div>
      </div>
    </CustomerMutationForm>
  );
}

export function RefundRequestForm(props: Parameters<typeof RefundRequestFormContent>[0]) {
  return (
    <UploadTaskGuard scope={{ purpose: "refund:request-evidence", targetIds: [props.order.id] }}>
      <UploadDraftChoice
        scope={{ purpose: "refund:request-evidence", targetIds: [props.order.id] }}
      >
        {(draftId) => <RefundRequestFormContent key={draftId ?? "new"} {...props} />}
      </UploadDraftChoice>
    </UploadTaskGuard>
  );
}
