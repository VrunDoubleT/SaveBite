import { useEffect, useRef, useState } from "react";
import {
  Camera,
  CheckCircle2,
  Loader2,
  Package,
  Star,
  Store,
  X,
} from "lucide-react";
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
  images: string[];
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
  const [uploadingItemId, setUploadingItemId] = useState<string | null>(null);
  const fileInputRefs = useRef<Record<string, HTMLInputElement | null>>({});
  const [photoWarning, setPhotoWarning] = useState<{
    isOpen: boolean;
    selectedCount: number;
    maxAllowed: number;
  }>({ isOpen: false, selectedCount: 0, maxAllowed: 0 });

  useEffect(() => {
    if (!order || !isOpen) return;
    const nextDrafts: Record<string, ItemDraft> = {};
    for (const item of order.items) {
      if (item.orderItemId && item.canReview) {
        nextDrafts[item.orderItemId] = {
          enabled: focusOrderItemId ? item.orderItemId === focusOrderItemId : true,
          rating: 5,
          comment: "",
          images: [],
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
        ...(prev[orderItemId] ?? {
          enabled: true,
          rating: 5,
          comment: "",
          images: [],
        }),
        ...patch,
      },
    }));
  };

  const handleImageUpload = async (
    orderItemId: string,
    files: FileList | null,
  ) => {
    if (!files || files.length === 0) return;

    const currentImages = drafts[orderItemId]?.images ?? [];
    const remainingSlots = 5 - currentImages.length;

    if (files.length > remainingSlots) {
      setPhotoWarning({
        isOpen: true,
        selectedCount: files.length,
        maxAllowed: Math.max(0, remainingSlots),
      });
      if (fileInputRefs.current[orderItemId]) {
        fileInputRefs.current[orderItemId]!.value = "";
      }
      return;
    }

    if (remainingSlots <= 0) {
      toast.info("You can only upload a maximum of 5 images per review.");
      return;
    }

    const filesToUpload = Array.from(files).slice(0, remainingSlots);

    try {
      setUploadingItemId(orderItemId);
      const uploadedUrls: string[] = [];

      for (const file of filesToUpload) {
        if (!file.type.startsWith("image/")) {
          toast.error(`${file.name} is not an image file.`);
          continue;
        }
        if (file.size > 5 * 1024 * 1024) {
          toast.error(`${file.name} exceeds 5MB size limit.`);
          continue;
        }
        const url = await feedbackApi.uploadImage(file);
        uploadedUrls.push(url);
      }

      if (uploadedUrls.length > 0) {
        updateDraft(orderItemId, {
          images: [...currentImages, ...uploadedUrls].slice(0, 5),
        });
      }
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to upload image.");
    } finally {
      setUploadingItemId(null);
      if (fileInputRefs.current[orderItemId]) {
        fileInputRefs.current[orderItemId]!.value = "";
      }
    }
  };

  const handleRemoveImage = (orderItemId: string, indexToRemove: number) => {
    const currentImages = drafts[orderItemId]?.images ?? [];
    updateDraft(orderItemId, {
      images: currentImages.filter((_, idx) => idx !== indexToRemove),
    });
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
          images: draft.images.length > 0 ? draft.images : undefined,
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
    <>
    <SharedModal
      isOpen={isOpen}
      onClose={onClose}
      title={`Rate order #${order.orderCode}`}
      maxWidthClass="max-w-xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Shop header */}
        <div className="flex items-center gap-2.5 rounded-lg bg-neutral-50 px-3.5 py-2.5 text-caption text-text-secondary">
          <Store className="size-4 shrink-0 text-primary-700" />
          <span className="font-semibold text-text-primary">{order.shopName}</span>
          <span>·</span>
          <span>Completed order</span>
        </div>

        {/* Items list */}
        <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
          {order.items.map((item) => {
            const itemId = item.orderItemId;
            const isReviewable = Boolean(itemId && item.canReview);
            const draft = itemId ? drafts[itemId] : undefined;
            const isUploadingThisItem = uploadingItemId === itemId;

            return (
              <div
                key={itemId ?? item.flashDealVariantId}
                className="rounded-lg border border-border-default bg-bg-surface p-4 shadow-2xs"
              >
                <div className="flex items-start gap-3">
                  <span className="relative flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-neutral-100 text-text-muted">
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
                        <p className="text-body font-semibold text-text-primary">
                          {item.productName}
                        </p>
                        {item.variantName && (
                          <p className="mt-0.5 text-caption text-text-secondary">
                            {item.variantName}
                          </p>
                        )}
                      </div>

                      {isReviewable && reviewableItems.length > 1 && itemId && (
                        <label className="inline-flex cursor-pointer items-center gap-1.5 text-caption font-medium text-text-secondary">
                          <input
                            type="checkbox"
                            checked={draft?.enabled ?? true}
                            onChange={(e) =>
                              updateDraft(itemId, { enabled: e.target.checked })
                            }
                            className="size-4 rounded border-border-default text-primary-600 focus:ring-primary-500"
                          />
                          Rate this item
                        </label>
                      )}
                    </div>

                    {!isReviewable && (
                      <div className="mt-2 flex flex-wrap items-center gap-2">
                        {item.rating ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-primary-50 px-2.5 py-0.5 text-caption font-semibold text-primary-700">
                            <CheckCircle2 className="size-3.5" />
                            Reviewed
                            <Star className="ml-0.5 size-3 fill-accent-300 text-accent-300" />
                            {item.rating}/5
                          </span>
                        ) : (
                          <span className="inline-flex items-center rounded-full bg-neutral-100 px-2.5 py-0.5 text-caption font-medium text-text-secondary">
                            Already reviewed
                          </span>
                        )}
                        <Link
                          to={APP_PATHS.ACCOUNT_REVIEWS}
                          onClick={onClose}
                          className="text-caption font-semibold text-primary-700 underline underline-offset-2 hover:text-primary-800"
                        >
                          View in My reviews
                        </Link>
                      </div>
                    )}
                  </div>
                </div>

                {isReviewable && itemId && draft?.enabled && (
                  <div className="mt-3.5 space-y-3 border-t border-border-default pt-3">
                    <div>
                      <label className="mb-1 block text-caption font-medium text-text-secondary">
                        Your rating
                      </label>
                      <StarRatingInput
                        value={draft.rating}
                        onChange={(rating) => updateDraft(itemId, { rating })}
                        disabled={isSubmitting}
                      />
                      {draft.rating <= 2 && (
                        <p className="mt-2 rounded-md border border-amber-200 bg-amber-50 p-2 text-xs text-amber-800">
                          We're sorry to hear about your experience. Please describe what went wrong below so the store can improve!
                        </p>
                      )}
                    </div>

                    <div>
                      <div className="mb-1 flex items-center justify-between">
                        <label className="block text-caption font-medium text-text-secondary">
                          Comment <span className="font-normal text-text-muted">(optional)</span>
                        </label>
                        <span className="text-caption text-text-muted">
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
                        className="w-full resize-none rounded-lg border border-border-default bg-bg-surface px-3 py-2 text-body text-text-primary outline-none focus:border-primary-600 focus:ring-1 focus:ring-primary-600"
                      />
                    </div>

                    {/* Review Photos (Up to 5 images) */}
                    <div>
                      <div className="mb-1.5 flex items-center justify-between">
                        <label className="block text-caption font-medium text-text-secondary">
                          Add photos <span className="font-normal text-text-muted">(optional, max 5)</span>
                        </label>
                        <span className="text-caption text-text-muted">
                          {draft.images.length}/5 photos
                        </span>
                      </div>

                      <div className="flex flex-wrap items-center gap-2">
                        {draft.images.map((imgUrl, imgIdx) => (
                          <div
                            key={imgIdx}
                            className="relative size-16 overflow-hidden rounded-lg border border-border-default group"
                          >
                            <img
                              src={imgUrl}
                              alt={`Upload ${imgIdx + 1}`}
                              className="size-full object-cover"
                            />
                            <button
                              type="button"
                              onClick={() => handleRemoveImage(itemId, imgIdx)}
                              disabled={isSubmitting}
                              className="absolute right-1 top-1 flex size-5 items-center justify-center rounded-full bg-text-primary/60 text-text-inverse transition hover:bg-text-primary"
                              title="Remove image"
                            >
                              <X className="size-3" />
                            </button>
                          </div>
                        ))}

                        {draft.images.length < 5 && (
                          <button
                            type="button"
                            disabled={isSubmitting || isUploadingThisItem}
                            onClick={() => fileInputRefs.current[itemId]?.click()}
                            className="flex size-16 flex-col items-center justify-center gap-1 rounded-lg border border-dashed border-border-default bg-neutral-50 text-text-secondary transition hover:border-primary-500 hover:bg-primary-50/50 hover:text-primary-700 disabled:opacity-50"
                          >
                            {isUploadingThisItem ? (
                              <Loader2 className="size-5 animate-spin text-primary-600" />
                            ) : (
                              <>
                                <Camera className="size-5" />
                                <span className="text-caption font-medium">Add photo</span>
                              </>
                            )}
                          </button>
                        )}
                      </div>

                      <input
                        ref={(el) => {
                          fileInputRefs.current[itemId] = el;
                        }}
                        type="file"
                        accept="image/*"
                        multiple
                        className="hidden"
                        onChange={(e) => handleImageUpload(itemId, e.target.files)}
                      />
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Footer actions */}
        <div className="flex items-center justify-end gap-2.5 border-t border-border-default pt-3">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="rounded-lg border border-border-default bg-bg-surface px-4 py-2 text-caption font-semibold text-text-secondary hover:bg-neutral-50 disabled:opacity-50"
          >
            Cancel
          </button>
          {reviewableItems.length > 0 && (
            <button
              type="submit"
              disabled={isSubmitting || selectedCount === 0 || Boolean(uploadingItemId)}
              className="rounded-lg bg-primary-600 px-5 py-2 text-caption font-semibold text-text-inverse shadow-xs hover:bg-primary-700 disabled:opacity-50"
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

      {/* Warning popup when selecting > 5 images (FIX-38) */}
      <SharedModal
        isOpen={photoWarning.isOpen}
        onClose={() => setPhotoWarning({ isOpen: false, selectedCount: 0, maxAllowed: 0 })}
        title="Photo Limit Exceeded"
        maxWidthClass="max-w-md"
      >
        <div className="space-y-4">
          <p className="text-body text-text-secondary">
            You selected <strong>{photoWarning.selectedCount}</strong> photos, but only <strong>{photoWarning.maxAllowed}</strong> more photo{photoWarning.maxAllowed === 1 ? "" : "s"} can be added (maximum 5 photos per review).
          </p>
          <p className="text-caption text-text-muted">
            Please choose up to {photoWarning.maxAllowed} photo{photoWarning.maxAllowed === 1 ? "" : "s"}.
          </p>
          <div className="flex justify-end">
            <button
              type="button"
              onClick={() => setPhotoWarning({ isOpen: false, selectedCount: 0, maxAllowed: 0 })}
              className="rounded-lg bg-primary-600 px-4 py-2 text-xs font-semibold text-white shadow-2xs hover:bg-primary-700 transition"
            >
              Understand
            </button>
          </div>
        </div>
      </SharedModal>
    </>
  );
}
