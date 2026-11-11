import { isUploadCancelled } from "@/shared/uploads/uploadErrors";
import { UploadDraftChoice } from "@/shared/uploads/UploadDraftChoice";
import { UploadTaskGuard } from "@/shared/uploads/UploadTaskGuard";
import {
  CustomerActionFileInput,
  CustomerMutationForm,
  CustomerActionNativeButton,
} from "@/shared/components/CustomerMutationControls";
import { useEffect, useRef, useState } from "react";
import { Camera, CheckCircle2, Loader2, Package, Star, Store, X } from "lucide-react";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { CheckoutOrder } from "@/features/checkout/types/checkout.types";
import { feedbackApi } from "../api/feedbackApi";
import { StarRatingInput } from "./StarRatingInput";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";
import {
  getUploadDraft,
  removeUploadDraft,
  saveUploadDraft,
} from "@/shared/uploads/uploadDraftStore";
import { startOrResumeUploadDraft } from "@/shared/uploads/uploadDrafts";

interface ItemDraft {
  enabled: boolean;
  rating: number;
  comment: string;
  images: string[];
}

interface SavedReviewForm {
  kind: "create-review";
  orderItemId: string;
  itemDraft: ItemDraft;
  assetIds: string[];
}

interface RateOrderModalProps {
  isOpen: boolean;
  onClose: () => void;
  order: CheckoutOrder | null;
  focusOrderItemId?: string;
  uploadDraftId?: string;
  onSubmitted?: () => void;
}

function isItemDraft(value: unknown): value is ItemDraft {
  if (!value || typeof value !== "object") return false;
  const draft = value as Partial<ItemDraft>;
  return (
    typeof draft.enabled === "boolean" &&
    typeof draft.rating === "number" &&
    typeof draft.comment === "string" &&
    Array.isArray(draft.images) &&
    draft.images.every((image) => typeof image === "string")
  );
}

function RateOrderModalContent({
  isOpen,
  onClose,
  order,
  focusOrderItemId,
  uploadDraftId,
  onSubmitted,
}: RateOrderModalProps) {
  const userId = useAuthStore((state) => state.user?.id);
  const [drafts, setDrafts] = useState<Record<string, ItemDraft>>({});
  const [pendingPhotos, setPendingPhotos] = useState<
    Record<string, { id: string; file: File; url: string }[]>
  >({});
  const previewUrls = useRef(new Set<string>());
  useEffect(
    () => () => {
      for (const url of previewUrls.current) URL.revokeObjectURL(url);
    },
    [],
  );
  const [draftIds, setDraftIds] = useState<Record<string, string>>({});
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
    let active = true;
    const initialize = async () => {
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
      if (focusOrderItemId && !Object.values(nextDrafts).some((draft) => draft.enabled)) {
        for (const key of Object.keys(nextDrafts)) nextDrafts[key].enabled = true;
      }

      const nextDraftIds: Record<string, string> = {};
      const nextPending: Record<string, { id: string; file: File; url: string }[]> = {};
      if (uploadDraftId) {
        const saved = await getUploadDraft(uploadDraftId);
        const form = saved?.formData as Partial<SavedReviewForm> | undefined;
        if (
          saved &&
          saved.userId === userId &&
          saved.purpose === "feedback" &&
          form?.kind === "create-review" &&
          typeof form.orderItemId === "string" &&
          isItemDraft(form.itemDraft) &&
          nextDrafts[form.orderItemId]
        ) {
          const images = [...form.itemDraft.images];
          for (const file of saved.files) {
            if (
              form.assetIds?.includes(file.id) &&
              file.field === "review-image" &&
              file.url &&
              !images.includes(file.url)
            )
              images.push(file.url);
          }
          nextDrafts[form.orderItemId] = {
            ...form.itemDraft,
            enabled: true,
            images: images.slice(0, 5),
          };
          nextDraftIds[form.orderItemId] = saved.id;
          nextPending[form.orderItemId] = saved.files
            .filter((file) => form.assetIds?.includes(file.id) && !file.url)
            .map((entry) => {
              const url = URL.createObjectURL(entry.file);
              previewUrls.current.add(url);
              return { id: entry.id, file: entry.file, url };
            });
        } else {
          toast.error("This saved review is unavailable or has expired.");
        }
      }
      if (!active) return;
      setDrafts(nextDrafts);
      setDraftIds(nextDraftIds);
      setPendingPhotos(nextPending);
    };
    void initialize().catch((error) => toast.error(getApiErrorMessage(error)));
    return () => {
      active = false;
    };
  }, [order, isOpen, focusOrderItemId, uploadDraftId, userId]);

  if (!order) return null;

  const reviewableItems = order.items.filter(
    (item) => Boolean(item.orderItemId) && Boolean(item.canReview),
  );
  const selectedCount = Object.values(drafts).filter((d) => d.enabled).length;

  const updateDraft = (orderItemId: string, patch: Partial<ItemDraft>) => {
    const updated = {
      ...(drafts[orderItemId] ?? {
        enabled: true,
        rating: 5,
        comment: "",
        images: [],
      }),
      ...patch,
    };
    setDrafts((prev) => ({
      ...prev,
      [orderItemId]: updated,
    }));
    const draftId = draftIds[orderItemId];
    if (draftId) {
      void getUploadDraft(draftId)
        .then(async (saved) => {
          if (!saved) return;
          const form = saved.formData as Partial<SavedReviewForm>;
          saved.formData = {
            kind: "create-review",
            orderItemId,
            itemDraft: updated,
            assetIds: form.assetIds ?? [],
          } satisfies SavedReviewForm;
          await saveUploadDraft(saved, false);
        })
        .catch((error) => toast.error(getApiErrorMessage(error)));
    }
  };

  const handleImageUpload = async (orderItemId: string, files: FileList | null) => {
    if (!files || files.length === 0) return;

    const currentImages = drafts[orderItemId]?.images ?? [];
    const remainingSlots = 5 - currentImages.length - (pendingPhotos[orderItemId]?.length ?? 0);

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
    const validFiles = filesToUpload.filter((file) => {
      if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
        toast.error(`${file.name} is not a supported image file.`);
        return false;
      }
      if (file.size > 5 * 1024 * 1024) {
        toast.error(`${file.name} exceeds 5MB size limit.`);
        return false;
      }
      return true;
    });
    if (!validFiles.length) return;
    const photos = validFiles.map((file) => {
      const url = URL.createObjectURL(file);
      previewUrls.current.add(url);
      return { id: crypto.randomUUID(), file, url };
    });
    setPendingPhotos((prev) => ({
      ...prev,
      [orderItemId]: [...(prev[orderItemId] ?? []), ...photos],
    }));
    if (fileInputRefs.current[orderItemId]) fileInputRefs.current[orderItemId]!.value = "";
  };

  const handleRemoveImage = (orderItemId: string, indexToRemove: number) => {
    const currentImages = drafts[orderItemId]?.images ?? [];
    if (indexToRemove >= currentImages.length) {
      const index = indexToRemove - currentImages.length;
      const removed = pendingPhotos[orderItemId]?.[index];
      if (removed) {
        URL.revokeObjectURL(removed.url);
        previewUrls.current.delete(removed.url);
      }
      setPendingPhotos((prev) => ({
        ...prev,
        [orderItemId]: (prev[orderItemId] ?? []).filter((_, i) => i !== index),
      }));
      return;
    }
    const removedUrl = currentImages[indexToRemove];
    const updated = {
      ...(drafts[orderItemId] ?? {
        enabled: true,
        rating: 5,
        comment: "",
        images: [],
      }),
      images: currentImages.filter((_, idx) => idx !== indexToRemove),
    };
    setDrafts((prev) => ({ ...prev, [orderItemId]: updated }));
    const draftId = draftIds[orderItemId];
    if (draftId && removedUrl) {
      void getUploadDraft(draftId)
        .then(async (saved) => {
          if (!saved) return;
          const form = saved.formData as Partial<SavedReviewForm>;
          saved.formData = {
            kind: "create-review",
            orderItemId,
            itemDraft: updated,
            assetIds: (form.assetIds ?? []).filter(
              (assetId) =>
                !saved.files.some((file) => file.id === assetId && file.url === removedUrl),
            ),
          } satisfies SavedReviewForm;
          await saveUploadDraft(saved, false);
        })
        .catch((error) => toast.error(getApiErrorMessage(error)));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isSubmitting) return;
    const toSubmit = reviewableItems
      .map((item) => ({
        item,
        draft: item.orderItemId ? drafts[item.orderItemId] : undefined,
      }))
      .filter((entry): entry is { item: (typeof reviewableItems)[number]; draft: ItemDraft } =>
        Boolean(entry.item.orderItemId && entry.draft?.enabled),
      );

    if (toSubmit.length === 0) {
      toast.error("Please select at least one item to review.");
      return;
    }

    let modalClosed = false;
    const closeQueuedModal = () => {
      if (modalClosed) return;
      modalClosed = true;
      onClose();
    };
    try {
      setIsSubmitting(true);
      let submittedCount = 0;

      for (const { item, draft } of toSubmit) {
        let finalImages = draft.images;
        const itemId = item.orderItemId!;
        let draftId = draftIds[itemId];
        const previous = draftId ? await getUploadDraft(draftId) : undefined;
        if (draftId && !previous)
          throw new Error("This saved upload has expired. Please select the images again.");
        const selectedFiles = [
          ...(previous?.files
            .filter((file) => file.url && draft.images.includes(file.url))
            .map((file) => ({ id: file.id, field: file.field, file: file.file })) ?? []),
          ...(pendingPhotos[itemId] ?? []).map((photo) => ({
            id: photo.id,
            field: "review-image",
            file: photo.file,
          })),
        ];
        if (selectedFiles.length) {
          if (!userId) throw new Error("Sign in before submitting your review.");
          draftId ??= crypto.randomUUID();
          setDraftIds((prev) => ({ ...prev, [itemId]: draftId! }));
          setUploadingItemId(itemId);
          const saved = await startOrResumeUploadDraft(
            {
              draftId,
              onQueued: closeQueuedModal,
              userId,
              purpose: "feedback",
              targetId: itemId,
              title: `Review ${item.productName ?? "product"}`,
              path: APP_PATHS.ORDER_DETAIL.replace(":orderId", order.id),
              formData: {
                kind: "create-review",
                orderItemId: itemId,
                itemDraft: draft,
                assetIds: selectedFiles.map((file) => file.id),
              } satisfies SavedReviewForm,
              files: selectedFiles,
            },
            draftId,
          );
          finalImages = [
            ...new Set([...draft.images, ...saved.files.map((file) => file.url!)]),
          ].slice(0, 5);
          setDrafts((prev) => ({ ...prev, [itemId]: { ...draft, images: finalImages } }));
          setPendingPhotos((prev) => ({ ...prev, [itemId]: [] }));
          setUploadingItemId(null);
        }
        await feedbackApi.addFeedback({
          orderItemId: item.orderItemId!,
          rating: draft.rating,
          comment: draft.comment.trim() || undefined,
          images: finalImages.length > 0 ? finalImages : undefined,
        });
        submittedCount += 1;
        if (draftId) {
          try {
            await removeUploadDraft(draftId);
            setDraftIds((prev) => {
              const next = { ...prev };
              delete next[item.orderItemId!];
              return next;
            });
          } catch (cleanupError) {
            toast.error(
              `Review submitted, but saved upload cleanup failed: ${getApiErrorMessage(cleanupError)}`,
            );
          }
        }
      }

      toast.success(
        submittedCount === 1
          ? "Thank you! Your review has been submitted."
          : `Thank you! ${submittedCount} reviews have been submitted.`,
      );
      onSubmitted?.();
      if (!modalClosed) onClose();
    } catch (error) {
      if (isUploadCancelled(error)) {
        toast.info("Upload stopped. Your draft is saved; edit it or submit again when ready.");
        return;
      }
      toast.error(getApiErrorMessage(error) || "Failed to submit feedback.");
      // Refresh parent in case some items succeeded or 409 occurred
      onSubmitted?.();
    } finally {
      setUploadingItemId(null);
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
        <CustomerMutationForm onSubmit={handleSubmit} className="space-y-4">
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
              const displayImages = [
                ...(draft?.images ?? []),
                ...(itemId ? (pendingPhotos[itemId] ?? []).map((photo) => photo.url) : []),
              ];

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
                              onChange={(e) => updateDraft(itemId, { enabled: e.target.checked })}
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
                            We're sorry to hear about your experience. Please describe what went
                            wrong below so the store can improve!
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
                          onChange={(e) => updateDraft(itemId, { comment: e.target.value })}
                          placeholder="Share your experience with food quality, portion size, and packaging..."
                          className="w-full resize-none rounded-lg border border-border-default bg-bg-surface px-3 py-2 text-body text-text-primary outline-none focus:border-primary-600 focus:ring-1 focus:ring-primary-600"
                        />
                      </div>

                      {/* Review Photos (Up to 5 images) */}
                      <div>
                        <div className="mb-1.5 flex items-center justify-between">
                          <label className="block text-caption font-medium text-text-secondary">
                            Add photos{" "}
                            <span className="font-normal text-text-muted">(optional, max 5)</span>
                          </label>
                          <span className="text-caption text-text-muted">
                            {displayImages.length}/5 photos
                          </span>
                        </div>

                        <p className="mb-2 text-caption text-text-secondary">
                          Photos upload only when you submit your review.
                        </p>
                        <div className="flex flex-wrap items-center gap-2">
                          {displayImages.map((imgUrl, imgIdx) => (
                            <div
                              key={imgIdx}
                              className="relative size-16 overflow-hidden rounded-lg border border-border-default group"
                            >
                              <img
                                src={imgUrl}
                                alt={`Upload ${imgIdx + 1}`}
                                className="size-full object-cover"
                              />
                              <CustomerActionNativeButton
                                type="button"
                                onClick={() => handleRemoveImage(itemId, imgIdx)}
                                disabled={isSubmitting}
                                className="absolute right-1 top-1 flex size-5 items-center justify-center rounded-full bg-text-primary/60 text-text-inverse transition hover:bg-text-primary"
                                title="Remove image"
                              >
                                <X className="size-3" />
                              </CustomerActionNativeButton>
                            </div>
                          ))}

                          {displayImages.length < 5 && (
                            <CustomerActionNativeButton
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
                            </CustomerActionNativeButton>
                          )}
                        </div>

                        <CustomerActionFileInput
                          ref={(el) => {
                            fileInputRefs.current[itemId] = el;
                          }}
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
              <CustomerActionNativeButton
                type="submit"
                disabled={isSubmitting || selectedCount === 0 || Boolean(uploadingItemId)}
                className="rounded-lg bg-primary-600 px-5 py-2 text-caption font-semibold text-text-inverse shadow-xs hover:bg-primary-700 disabled:opacity-50"
              >
                {isSubmitting
                  ? "Submitting..."
                  : selectedCount > 1
                    ? `Submit ${selectedCount} reviews`
                    : "Submit review"}
              </CustomerActionNativeButton>
            )}
          </div>
        </CustomerMutationForm>
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
            You selected <strong>{photoWarning.selectedCount}</strong> photos, but only{" "}
            <strong>{photoWarning.maxAllowed}</strong> more photo
            {photoWarning.maxAllowed === 1 ? "" : "s"} can be added (maximum 5 photos per review).
          </p>
          <p className="text-caption text-text-muted">
            Please choose up to {photoWarning.maxAllowed} photo
            {photoWarning.maxAllowed === 1 ? "" : "s"}.
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

export function RateOrderModal(props: RateOrderModalProps) {
  if (!props.isOpen || !props.order) return null;
  const targetIds = props.focusOrderItemId
    ? [props.focusOrderItemId]
    : props.order.items.map((item) => item.orderItemId);
  return (
    <UploadTaskGuard
      key={`${props.order.id}:${props.focusOrderItemId ?? "all"}`}
      scope={{ purpose: "feedback", targetIds, operation: "create-review" }}
      onClose={props.onClose}
    >
      <UploadDraftChoice
        scope={{ purpose: "feedback", targetIds, operation: "create-review" }}
        selectedDraftId={props.uploadDraftId}
        onClose={props.onClose}
      >
        {(draftId) => (
          <RateOrderModalContent key={draftId ?? "new"} {...props} uploadDraftId={draftId} />
        )}
      </UploadDraftChoice>
    </UploadTaskGuard>
  );
}