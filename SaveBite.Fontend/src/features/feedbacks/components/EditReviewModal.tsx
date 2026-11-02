import { useEffect, useState } from "react";
import { Package } from "lucide-react";
import { ErrorState, MediaImage, Skeleton } from "@/shared/components/ui";
import { ReviewEditor } from "./ReviewEditor";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { feedbackApi } from "../api/feedbackApi";
import type { FeedbackItem } from "../types/feedback.types";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";

export interface EditReviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  feedbackId: string | null;
  initialProductName?: string;
  initialVariantName?: string;
  initialImageUrl?: string | null;
  onUpdated?: (updatedItem: FeedbackItem) => void;
}

export function EditReviewModal(props: EditReviewModalProps) {
  return props.isOpen && props.feedbackId ? (
    <EditReviewContent key={props.feedbackId} {...props} />
  ) : null;
}

function EditReviewContent({
  isOpen,
  onClose,
  feedbackId,
  initialProductName,
  initialVariantName,
  initialImageUrl,
  onUpdated,
}: EditReviewModalProps) {
  const [feedback, setFeedback] = useState<FeedbackItem | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [images, setImages] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (!feedbackId) return;
    let active = true;
    feedbackApi
      .getFeedback(feedbackId)
      .then((data) => {
        if (!active) return;
        setFeedback(data);
        setRating(data.rating);
        setComment(data.comment ?? "");
        setImages(data.images ?? []);
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
  }, [isOpen, feedbackId, attempt]);

  const handleImageUpload = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    const remainingSlots = 5 - images.length;
    if (remainingSlots <= 0) {
      toast.info("You can attach up to 5 images.");
      return;
    }

    const filesToUpload = Array.from(files).slice(0, remainingSlots);
    try {
      setIsUploading(true);
      const uploadedUrls: string[] = [];
      for (const file of filesToUpload) {
        if (!file.type.startsWith("image/")) continue;
        if (file.size > 5 * 1024 * 1024) {
          toast.error(`${file.name} exceeds 5MB size limit.`);
          continue;
        }
        const url = await feedbackApi.uploadImage(file);
        uploadedUrls.push(url);
      }
      if (uploadedUrls.length > 0) {
        setImages((prev) => [...prev, ...uploadedUrls].slice(0, 5));
      }
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to upload image.");
    } finally {
      setIsUploading(false);
    }
  };

  const handleRemoveImage = (indexToRemove: number) => {
    setImages((prev) => prev.filter((_, idx) => idx !== indexToRemove));
  };

  const handleSave = async () => {
    if (!feedbackId) return;

    try {
      setSaveError("");
      setIsSaving(true);
      const updated = await feedbackApi.editFeedback(feedbackId, {
        rating,
        comment: comment.trim() || undefined,
        images,
      });

      toast.success("Review updated successfully.");
      onUpdated?.(updated);
      onClose();
    } catch (error) {
      setSaveError(getApiErrorMessage(error) || "Failed to update review.");
    } finally {
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
            <ReviewEditor
              rating={rating}
              comment={comment}
              images={images}
              saving={isSaving}
              uploading={isUploading}
              maxLength={1000}
              error={saveError}
              onRating={setRating}
              onComment={setComment}
              onRemove={handleRemoveImage}
              onUpload={handleImageUpload}
              onSave={async () => {
                await handleSave();
              }}
              onCancel={onClose}
              reply={feedback.reply}
            />
          </div>
        )
      )}
    </SharedModal>
  );
}
