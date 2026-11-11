import { isUploadCancelled } from "@/shared/uploads/uploadErrors";
import { UploadDraftChoice } from "@/shared/uploads/UploadDraftChoice";
import { UploadTaskGuard } from "@/shared/uploads/UploadTaskGuard";
import { useEffect, useRef, useState } from "react";
import { Package } from "lucide-react";
import { ErrorState, MediaImage, Skeleton } from "@/shared/components/ui";
import { ReviewEditor } from "./ReviewEditor";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { feedbackApi } from "../api/feedbackApi";
import type { FeedbackItem } from "../types/feedback.types";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";
import {
  getUploadDraft,
  removeUploadDraft,
  saveUploadDraft,
} from "@/shared/uploads/uploadDraftStore";
import { startOrResumeUploadDraft } from "@/shared/uploads/uploadDrafts";
import { APP_PATHS } from "@/app/router/paths";
import { useLocation } from "react-router-dom";

export interface EditReviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  feedbackId: string | null;
  initialProductName?: string;
  initialVariantName?: string;
  initialImageUrl?: string | null;
  orderId?: string;
  uploadDraftId?: string;
  onUpdated?: (updatedItem: FeedbackItem) => void;
}

interface SavedEditReviewForm {
  kind: "edit-review";
  feedbackId: string;
  rating: number;
  comment: string;
  images: string[];
  assetIds: string[];
}

export function EditReviewModal(props: EditReviewModalProps) {
  return props.isOpen && props.feedbackId ? (
    <UploadTaskGuard
      key={`${props.feedbackId}:${props.uploadDraftId ?? ""}`}
      scope={{ purpose: "feedback", targetIds: [props.feedbackId], operation: "edit-review" }}
      onClose={props.onClose}
    >
      <UploadDraftChoice
        scope={{ purpose: "feedback", targetIds: [props.feedbackId!], operation: "edit-review" }}
        selectedDraftId={props.uploadDraftId}
        onClose={props.onClose}
      >
        {(draftId) => (
          <EditReviewContent key={draftId ?? "new"} {...props} uploadDraftId={draftId} />
        )}
      </UploadDraftChoice>
    </UploadTaskGuard>
  ) : null;
}

function EditReviewContent({
  isOpen,
  onClose,
  feedbackId,
  initialProductName,
  initialVariantName,
  initialImageUrl,
  orderId,
  uploadDraftId,
  onUpdated,
}: EditReviewModalProps) {
  const userId = useAuthStore((state) => state.user?.id);
  const location = useLocation();
  const [feedback, setFeedback] = useState<FeedbackItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [images, setImages] = useState<string[]>([]);
  const [pendingPhotos, setPendingPhotos] = useState<{ id: string; file: File; url: string }[]>([]);
  const previews = useRef(new Set<string>());
  const draftWrites = useRef<Promise<void>>(Promise.resolve());
  useEffect(
    () => () => {
      for (const url of previews.current) URL.revokeObjectURL(url);
    },
    [],
  );
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [attempt, setAttempt] = useState(0);
  const [activeDraftId, setActiveDraftId] = useState<string>();

  useEffect(() => {
    if (!feedbackId) return;
    let active = true;
    feedbackApi
      .getFeedback(feedbackId)
      .then(async (data) => {
        if (!active) return;
        setFeedback(data);
        let nextRating = data.rating;
        let nextComment = data.comment ?? "";
        let nextImages = data.images ?? [];
        if (uploadDraftId) {
          const saved = await getUploadDraft(uploadDraftId);
          if (!active) return;
          const form = saved?.formData as Partial<SavedEditReviewForm> | undefined;
          if (
            saved &&
            saved.userId === userId &&
            saved.purpose === "feedback" &&
            form?.kind === "edit-review" &&
            form.feedbackId === feedbackId &&
            typeof form.rating === "number" &&
            typeof form.comment === "string" &&
            Array.isArray(form.images) &&
            form.images.every((url) => typeof url === "string")
          ) {
            nextRating = form.rating;
            nextComment = form.comment;
            // The draft selection is authoritative, including an empty retained-image list.
            nextImages = [...new Set(form.images.filter((url) => !url.startsWith("blob:")))];
            const selectedIds = new Set(form.assetIds ?? []);
            const seen = new Set<string>();
            const selected = saved.files.filter((file) => {
              if (!selectedIds.has(file.id) || file.field !== "review-image") return false;
              const key = `${file.file.name}:${file.file.size}:${file.file.lastModified}`;
              if (seen.has(key)) return false;
              seen.add(key);
              return true;
            });
            for (const file of selected) {
              if (file.url && !nextImages.includes(file.url)) nextImages.push(file.url);
            }
            nextImages = nextImages.slice(0, 5);
            for (const url of previews.current) URL.revokeObjectURL(url);
            previews.current.clear();
            setPendingPhotos(
              selected
                .filter((file) => !file.url)
                .slice(0, 5 - nextImages.length)
                .map((file) => {
                  const url = URL.createObjectURL(file.file);
                  previews.current.add(url);
                  return { id: file.id, file: file.file, url };
                }),
            );
            setActiveDraftId(saved.id);
          } else {
            toast.error("This saved review is unavailable or has expired.");
          }
        }
        if (!active) return;
        setRating(nextRating);
        setComment(nextComment);
        setImages(nextImages.slice(0, 5));
      })
      .catch((err) => {
        if (!active) return;
        const msg = getApiErrorMessage(err) || "Failed to load review details.";
        setLoadError(msg);
        toast.error(msg);
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });

    return () => {
      active = false;
    };
  }, [isOpen, feedbackId, attempt, uploadDraftId, userId]);

  const persistForm = async (
    nextRating: number,
    nextComment: string,
    nextImages: string[],
    draftId = activeDraftId,
  ) => {
    if (!draftId || !feedbackId) return;
    const write = draftWrites.current
      .catch(() => {})
      .then(async () => {
        const saved = await getUploadDraft(draftId);
        if (!saved) return;
        const form = saved.formData as Partial<SavedEditReviewForm>;
        saved.formData = {
          kind: "edit-review",
          feedbackId,
          rating: nextRating,
          comment: nextComment,
          images: nextImages,
          assetIds: form.assetIds ?? [],
        } satisfies SavedEditReviewForm;
        await saveUploadDraft(saved, false);
      });
    draftWrites.current = write;
    await write;
  };

  const updateRating = (value: number) => {
    setRating(value);
    void persistForm(value, comment, images).catch((error) =>
      toast.error(getApiErrorMessage(error)),
    );
  };

  const updateComment = (value: string) => {
    setComment(value);
    void persistForm(rating, value, images).catch((error) =>
      toast.error(getApiErrorMessage(error)),
    );
  };

  const handleImageUpload = async (files: FileList | null) => {
    if (!files || files.length === 0 || !feedbackId) return;
    const remainingSlots = 5 - images.length - pendingPhotos.length;
    if (remainingSlots <= 0) {
      toast.info("You can attach up to 5 images.");
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
    const selected = validFiles.map((file) => {
      const url = URL.createObjectURL(file);
      previews.current.add(url);
      return { id: crypto.randomUUID(), file, url };
    });
    setPendingPhotos((current) => [...current, ...selected]);
  };

  const handleRemoveImage = (indexToRemove: number) => {
    if (indexToRemove >= images.length) {
      const index = indexToRemove - images.length;
      const photo = pendingPhotos[index];
      if (photo) {
        URL.revokeObjectURL(photo.url);
        previews.current.delete(photo.url);
      }
      setPendingPhotos((current) => current.filter((_, i) => i !== index));
      if (activeDraftId && photo) {
        draftWrites.current = draftWrites.current
          .catch(() => {})
          .then(async () => {
            const saved = await getUploadDraft(activeDraftId);
            if (!saved) return;
            const form = saved.formData as SavedEditReviewForm;
            saved.formData = { ...form, assetIds: form.assetIds.filter((id) => id !== photo.id) };
            await saveUploadDraft(saved, false);
          });
      }
      return;
    }
    const removedUrl = images[indexToRemove];
    const nextImages = images.filter((_, idx) => idx !== indexToRemove);
    setImages(nextImages);
    if (activeDraftId && removedUrl) {
      const write = draftWrites.current
        .catch(() => {})
        .then(async () => {
          const saved = await getUploadDraft(activeDraftId);
          if (!saved) return;
          const form = saved.formData as Partial<SavedEditReviewForm>;
          saved.formData = {
            kind: "edit-review",
            feedbackId,
            rating,
            comment,
            images: nextImages,
            assetIds: (form.assetIds ?? []).filter(
              (assetId) =>
                !saved.files.some((file) => file.id === assetId && file.url === removedUrl),
            ),
          };
          await saveUploadDraft(saved, false);
        });
      draftWrites.current = write;
      void write.catch((error) => toast.error(getApiErrorMessage(error)));
    }
  };

  const handleSave = async () => {
    if (!feedbackId || isSaving) return;
    let submittedDraftId = activeDraftId;
    let modalClosed = false;

    try {
      setSaveError("");
      setIsSaving(true);
      await draftWrites.current;
      let finalImages = images;
      const previous = activeDraftId ? await getUploadDraft(activeDraftId) : undefined;
      if (activeDraftId && !previous)
        throw new Error("This saved upload has expired. Please select the images again.");
      const files = [
        ...(previous?.files
          .filter((file) => file.url && images.includes(file.url))
          .map((file) => ({ id: file.id, field: file.field, file: file.file })) ?? []),
        ...pendingPhotos.map((photo) => ({
          id: photo.id,
          field: "review-image",
          file: photo.file,
        })),
      ];
      if (files.length) {
        if (!userId) throw new Error("Sign in before saving your review.");
        const draftId = activeDraftId ?? crypto.randomUUID();
        setActiveDraftId(draftId);
        setIsUploading(true);
        const saved = await startOrResumeUploadDraft(
          {
            draftId,
            userId,
            purpose: "feedback",
            targetId: feedbackId,
            title: `Edit review for ${feedback?.productName ?? "product"}`,
            path:
              orderId || feedback?.orderId
                ? APP_PATHS.ORDER_DETAIL.replace(":orderId", orderId ?? feedback?.orderId ?? "")
                : location.pathname,
            formData: {
              kind: "edit-review",
              feedbackId,
              rating,
              comment,
              images,
              assetIds: files.map((file) => file.id),
            } satisfies SavedEditReviewForm,
            files,
            onQueued: () => {
              modalClosed = true;
              onClose();
            },
          },
          draftId,
        );
        finalImages = [...new Set([...images, ...saved.files.map((file) => file.url!)])].slice(
          0,
          5,
        );
        setImages(finalImages);
        setPendingPhotos([]);
        submittedDraftId = saved.id;
      }
      const updated = await feedbackApi.editFeedback(feedbackId, {
        rating,
        comment: comment.trim() || undefined,
        images: finalImages,
      });

      toast.success("Review updated successfully.");
      if (submittedDraftId) {
        try {
          await removeUploadDraft(submittedDraftId);
          setActiveDraftId(undefined);
        } catch (cleanupError) {
          toast.error(
            `Review updated, but saved upload cleanup failed: ${getApiErrorMessage(cleanupError)}`,
          );
        }
      }
      onUpdated?.(updated);
      if (!modalClosed) onClose();
    } catch (error) {
      if (isUploadCancelled(error))
        toast.info("Upload stopped. Your draft is saved; edit it or submit again when ready.");
      else setSaveError(getApiErrorMessage(error) || "Failed to update review.");
    } finally {
      setIsUploading(false);
      setIsSaving(false);
    }
  };

  const productName = feedback?.productName || initialProductName || "Product";
  const variantName = feedback?.variantName || initialVariantName;
  const imageUrl = feedback?.imageUrl || initialImageUrl;
  const busy = isSaving || isUploading;
  return (
    <SharedModal
      isOpen={isOpen}
      onClose={() => {
        if (!busy) onClose();
      }}
      closeOnClickOutside={!busy}
      title="Edit your review"
      maxWidthClass="max-w-2xl"
    >
      {isLoading ? (
        <div role="status" aria-label="Loading review" className="space-y-4">
          <Skeleton className="h-20" />
          <Skeleton className="h-64" />
        </div>
      ) : loadError ? (
        <ErrorState
          title="Couldn't load review"
          description={loadError}
          onRetry={() => {
            setIsLoading(true);
            setLoadError(null);
            setAttempt((value) => value + 1);
          }}
        />
      ) : (
        feedback && (
          <div className="space-y-5">
            <div className="rounded-md bg-primary-50/50 p-3 text-caption text-text-secondary">
              {feedback.shopName}
              {feedback.orderCode && ` · Order ${feedback.orderCode}`}
            </div>
            <div className="flex items-start gap-3">
              <MediaImage
                src={imageUrl ?? undefined}
                alt={productName}
                fallbackIcon={Package}
                showFallbackLabel={false}
                className="size-16 shrink-0 rounded-md"
              />
              <div className="min-w-0">
                <p className="text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {productName}
                </p>
                {variantName && (
                  <p className="mt-1 text-caption text-text-secondary [overflow-wrap:anywhere]">
                    {variantName}
                  </p>
                )}
              </div>
            </div>
            {/* Edit Count and 30-day notice (FIX-28) */}
            <div className="flex items-center justify-between rounded-lg bg-neutral-50 px-3 py-2 text-xs text-text-secondary">
              <span>
                Edits used: <strong>{feedback.editCount ?? 0}/3</strong>
              </span>
              <span>
                {Math.max(0, 3 - (feedback.editCount ?? 0))} edit(s) remaining (within 30 days)
              </span>
            </div>

            <ReviewEditor
              rating={rating}
              comment={comment}
              images={[...images, ...pendingPhotos.map((photo) => photo.url)]}
              saving={isSaving}
              uploading={isUploading}
              maxLength={1000}
              error={saveError}
              onRating={updateRating}
              onComment={updateComment}
              onRemove={handleRemoveImage}
              onUpload={handleImageUpload}
              onSave={async () => {
                await handleSave();
              }}
              onCancel={onClose}
              reply={feedback.reply}
              editCount={feedback.editCount}
              canEdit={feedback.canEdit}
            />
          </div>
        )
      )}
    </SharedModal>
  );
}