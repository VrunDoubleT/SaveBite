import { useEffect, useState } from "react";
import { CheckCircle2, Package, Star, Store } from "lucide-react";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { feedbackApi } from "../api/feedbackApi";
import { StarRatingInput } from "./StarRatingInput";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";

interface ItemDraft {
  enabled: boolean;
  rating: number;
  comment: string;
}

interface RateOrderModalProps {
  isOpen: boolean;
  onClose: () => void;
  order: CheckoutOrder | null;
  focusOrderItemId?: string;
  onSubmitted?: () => void;
}

export function RateOrderModal({
  isOpen,
  onClose,
  order,
  focusOrderItemId,
  onSubmitted,
}: RateOrderModalProps) {
  const [drafts, setDrafts] = useState<Record<string, ItemDraft>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!order || !isOpen) return;
    const nextDrafts: Record<string, ItemDraft> = {};
    for (const item of order.items) {
      if (item.orderItemId && item.canReview) {
        nextDrafts[item.orderItemId] = {
          enabled: focusOrderItemId ? item.orderItemId === focusOrderItemId : true,
          rating: 5,
          comment: "",
        };
      }
    }
    // If focusOrderItemId wasn't reviewable, enable all reviewable items
    if (
      focusOrderItemId &&
      !Object.values(nextDrafts).some((d) => d.enabled)
    ) {
      for (const key of Object.keys(nextDrafts)) {
        nextDrafts[key].enabled = true;
      }
    }
    setDrafts(nextDrafts);
  }, [order, isOpen, focusOrderItemId]);

  if (!order) return null;

  const reviewableItems = order.items.filter(
    (item) => Boolean(item.orderItemId) && Boolean(item.canReview),
  );
  const selectedCount = Object.values(drafts).filter((d) => d.enabled).length;

  const updateDraft = (orderItemId: string, patch: Partial<ItemDraft>) => {
    setDrafts((prev) => ({
      ...prev,
      [orderItemId]: {
        ...(prev[orderItemId] ?? { enabled: true, rating: 5, comment: "" }),
        ...patch,
      },
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const toSubmit = reviewableItems
      .map((item) => ({
        item,
        draft: item.orderItemId ? drafts[item.orderItemId] : undefined,
      }))
      .filter(
        (entry): entry is { item: (typeof reviewableItems)[number]; draft: ItemDraft } =>
          Boolean(entry.item.orderItemId && entry.draft?.enabled),
      );

    if (toSubmit.length === 0) {
      toast.error("Please select at least one item to review.");
      return;
    }

    try {
      setIsSubmitting(true);
      let submittedCount = 0;

      for (const { item, draft } of toSubmit) {
        await feedbackApi.addFeedback({
          orderItemId: item.orderItemId!,
          rating: draft.rating,
          comment: draft.comment.trim() || undefined,
        });
        submittedCount += 1;
      }

      toast.success(
        submittedCount === 1
          ? "Thank you! Your review has been submitted."
          : `Thank you! ${submittedCount} reviews have been submitted.`,
      );
      onSubmitted?.();
      onClose();
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to submit feedback.");
      // Refresh parent in case some items succeeded or 409 occurred
      onSubmitted?.();
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <SharedModal
      isOpen={isOpen}
      onClose={onClose}
      title={`Rate order #${order.orderCode}`}
      maxWidthClass="max-w-xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Shop header */}
        <div className="flex items-center gap-2.5 rounded-xl bg-neutral-50 px-3.5 py-2.5 text-xs text-neutral-600">
          <Store className="size-4 shrink-0 text-emerald-700" />
          <span className="font-semibold text-neutral-900">{order.shopName}</span>
          <span>·</span>
          <span>Completed order</span>
        </div>

        {/* Items list */}
        <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
          {order.items.map((item) => {
            const itemId = item.orderItemId;
            const isReviewable = Boolean(itemId && item.canReview);
            const draft = itemId ? drafts[itemId] : undefined;

            return (
              <div
                key={itemId ?? item.flashDealVariantId}
                className="rounded-xl border border-neutral-200 bg-white p-4 shadow-2xs"
              >
                <div className="flex items-start gap-3">
                  <span className="relative flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-neutral-100 text-neutral-400">
                    <Package size={20} />
                    {item.imageUrl && (
                      <img
                        src={item.imageUrl}
                        alt={item.productName}
                        onError={(ev) => {
                          ev.currentTarget.style.display = "none";
                        }}
                        className="absolute inset-0 size-full object-cover"
                      />
                    )}
                  </span>

                  <div className="min-w-0 flex-1">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <p className="text-sm font-semibold text-neutral-900">
                          {item.productName}
                        </p>
                        {item.variantName && (
                          <p className="mt-0.5 text-xs text-neutral-500">
                            {item.variantName}
                          </p>
                        )}
                      </div>

                      {isReviewable && reviewableItems.length > 1 && itemId && (
                        <label className="inline-flex cursor-pointer items-center gap-1.5 text-xs font-medium text-neutral-600">
                          <input
                            type="checkbox"
                            checked={draft?.enabled ?? true}
                            onChange={(e) =>
                              updateDraft(itemId, { enabled: e.target.checked })
                            }
                            className="size-4 rounded border-neutral-300 text-emerald-600 focus:ring-emerald-500"
                          />
                          Rate this item
                        </label>
                      )}
                    </div>

                    {!isReviewable && (
                      <div className="mt-2 flex flex-wrap items-center gap-2">
                        {item.rating ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-700">
                            <CheckCircle2 className="size-3.5" />
                            Reviewed
                            <Star className="ml-0.5 size-3 fill-amber-400 text-amber-400" />
                            {item.rating}/5
                          </span>
                        ) : (
                          <span className="inline-flex items-center rounded-full bg-neutral-100 px-2.5 py-0.5 text-xs font-medium text-neutral-600">
                            Already reviewed
                          </span>
                        )}
                        <Link
                          to={APP_PATHS.ACCOUNT_REVIEWS}
                          onClick={onClose}
                          className="text-xs font-semibold text-emerald-700 underline underline-offset-2 hover:text-emerald-800"
                        >
                          View in My reviews
                        </Link>
                      </div>
                    )}
                  </div>
                </div>

                {isReviewable && itemId && draft?.enabled && (
                  <div className="mt-3.5 space-y-3 border-t border-neutral-100 pt-3">
                    <div>
                      <label className="mb-1 block text-xs font-medium text-neutral-700">
                        Your rating
                      </label>
                      <StarRatingInput
                        value={draft.rating}
                        onChange={(rating) => updateDraft(itemId, { rating })}
                        disabled={isSubmitting}
                      />
                    </div>

                    <div>
                      <div className="mb-1 flex items-center justify-between">
                        <label className="block text-xs font-medium text-neutral-700">
                          Comment <span className="font-normal text-neutral-400">(optional)</span>
                        </label>
                        <span className="text-[11px] text-neutral-400">
                          {draft.comment.length}/2000
                        </span>
                      </div>
                      <textarea
                        rows={3}
                        maxLength={2000}
                        disabled={isSubmitting}
                        value={draft.comment}
                        onChange={(e) =>
                          updateDraft(itemId, { comment: e.target.value })
                        }
                        placeholder="Share your experience with food quality, portion size, and packaging..."
                        className="w-full resize-none rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-900 outline-none focus:border-emerald-600 focus:ring-1 focus:ring-emerald-600"
                      />
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Footer actions */}
        <div className="flex items-center justify-end gap-2.5 border-t border-neutral-100 pt-3">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="rounded-lg border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50 disabled:opacity-50"
          >
            Cancel
          </button>
          {reviewableItems.length > 0 && (
            <button
              type="submit"
              disabled={isSubmitting || selectedCount === 0}
              className="rounded-lg bg-emerald-600 px-5 py-2 text-xs font-semibold text-white shadow-xs hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting
                ? "Submitting..."
                : selectedCount > 1
                  ? `Submit ${selectedCount} reviews`
                  : "Submit review"}
            </button>
          )}
        </div>
      </form>
    </SharedModal>
  );
}
