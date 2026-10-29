import { useEffect, useRef, useState } from "react";
import {
  Camera,
  Loader2,
  MessageSquareQuote,
  Package,
  Store,
  Trash2,
  X,
} from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { StarRatingInput } from "./StarRatingInput";
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

export function EditReviewModal({
  isOpen,
  onClose,
  feedbackId,
  initialProductName,
  initialVariantName,
  initialImageUrl,
  onUpdated,
}: EditReviewModalProps) {
  const [feedback, setFeedback] = useState<FeedbackItem | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [images, setImages] = useState<string[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [previewImage, setPreviewImage] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!isOpen || !feedbackId) {
      setFeedback(null);
      setLoadError(null);
      return;
    }

    let active = true;
    setIsLoading(true);
    setLoadError(null);

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
  }, [isOpen, feedbackId]);

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
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleRemoveImage = (indexToRemove: number) => {
    setImages((prev) => prev.filter((_, idx) => idx !== indexToRemove));
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!feedbackId) return;

    try {
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
      toast.error(getApiErrorMessage(error) || "Failed to update review.");
    } finally {
      setIsSaving(false);
    }
  };

  const productName = feedback?.productName || initialProductName || "Product";
  const variantName = feedback?.variantName || initialVariantName;
  const imageUrl = feedback?.imageUrl || initialImageUrl;

  return (
    <>
      <SharedModal
        isOpen={isOpen}
        onClose={onClose}
        title="Edit your review"
        maxWidthClass="max-w-xl"
      >
        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-12 text-neutral-500">
            <Loader2 className="size-8 animate-spin text-emerald-600" />
            <p className="mt-3 text-xs sm:text-sm">Loading review...</p>
          </div>
        ) : loadError ? (
          <div className="py-8 text-center text-sm text-red-600">
            <p>{loadError}</p>
            <button
              type="button"
              onClick={onClose}
              className="mt-4 rounded-xl border border-neutral-200 px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50"
            >
              Close
            </button>
          </div>
        ) : (
          <form onSubmit={handleSave} className="space-y-4">
            {/* Store header if available */}
            {feedback?.shopName && (
              <div className="flex items-center gap-2.5 rounded-xl bg-neutral-50 px-3.5 py-2.5 text-xs text-neutral-600">
                <Store className="size-4 shrink-0 text-emerald-700" />
                <span className="font-semibold text-neutral-900">
                  {feedback.shopName}
                </span>
                {feedback.orderCode && (
                  <>
                    <span>·</span>
                    <span>Order #{feedback.orderCode}</span>
                  </>
                )}
              </div>
            )}

            {/* Product item info */}
            <div className="flex items-start gap-3 rounded-xl border border-neutral-200 bg-white p-3.5 shadow-2xs">
              <span className="relative flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-neutral-100 text-neutral-400">
                <Package size={22} />
                {imageUrl && (
                  <img
                    src={imageUrl}
                    alt={productName}
                    onError={(ev) => {
                      ev.currentTarget.style.display = "none";
                    }}
                    className="absolute inset-0 size-full object-cover"
                  />
                )}
              </span>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-semibold text-neutral-900">{productName}</p>
                {variantName && (
                  <p className="text-xs text-neutral-500">{variantName}</p>
                )}
              </div>
            </div>

            {/* Star Rating */}
            <div className="rounded-xl border border-neutral-200 bg-neutral-50/50 p-4">
              <label className="block text-xs font-bold text-neutral-700 uppercase tracking-wider mb-2">
                Your Rating
              </label>
              <StarRatingInput value={rating} onChange={setRating} size="lg" />
            </div>

            {/* Comment */}
            <div>
              <div className="flex items-center justify-between">
                <label
                  htmlFor="edit-review-comment"
                  className="block text-xs font-semibold text-neutral-700"
                >
                  Your feedback
                </label>
                <span className="text-[11px] text-neutral-400">
                  {comment.length} / 1000
                </span>
              </div>
              <textarea
                id="edit-review-comment"
                value={comment}
                onChange={(e) => setComment(e.target.value.slice(0, 1000))}
                rows={3}
                placeholder="Share your thoughts about this product, freshness, flavor..."
                className="mt-1.5 w-full rounded-xl border border-neutral-200 p-3 text-xs text-neutral-800 placeholder:text-neutral-400 focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              />
            </div>

            {/* Image Upload */}
            <div>
              <div className="flex items-center justify-between">
                <label className="block text-xs font-semibold text-neutral-700">
                  Attached Photos ({images.length}/5)
                </label>
                {images.length < 5 && (
                  <button
                    type="button"
                    disabled={isUploading}
                    onClick={() => fileInputRef.current?.click()}
                    className="inline-flex items-center gap-1 text-xs font-semibold text-emerald-600 hover:text-emerald-700 disabled:opacity-50"
                  >
                    {isUploading ? (
                      <Loader2 className="size-3.5 animate-spin" />
                    ) : (
                      <Camera className="size-3.5" />
                    )}
                    Upload photos
                  </button>
                )}
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                multiple
                className="hidden"
                onChange={(e) => void handleImageUpload(e.target.files)}
              />

              {images.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-2">
                  {images.map((imgUrl, idx) => (
                    <div
                      key={imgUrl}
                      className="group relative size-16 overflow-hidden rounded-lg border border-neutral-200 bg-neutral-100"
                    >
                      <button
                        type="button"
                        onClick={() => setPreviewImage(imgUrl)}
                        className="size-full cursor-pointer"
                        title="View photo"
                      >
                        <img
                          src={imgUrl}
                          alt={`Review photo ${idx + 1}`}
                          className="size-full object-cover transition group-hover:scale-105"
                        />
                      </button>
                      <button
                        type="button"
                        onClick={() => handleRemoveImage(idx)}
                        className="absolute right-1 top-1 rounded-full bg-neutral-900/70 p-1 text-white shadow-xs transition hover:bg-neutral-900"
                        title="Remove photo"
                      >
                        <Trash2 className="size-3" />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Store reply (read-only) */}
            {feedback?.reply && (
              <div className="rounded-xl border border-emerald-100 bg-emerald-50/60 p-3.5">
                <div className="flex items-center gap-2 text-xs font-bold text-emerald-900">
                  <MessageSquareQuote className="size-4 text-emerald-700" />
                  <span>Store Response</span>
                  {feedback.reply.repliedByName && (
                    <span className="text-[11px] font-normal text-emerald-800">
                      ({feedback.reply.repliedByName})
                    </span>
                  )}
                </div>
                <p className="mt-1.5 whitespace-pre-wrap text-xs text-emerald-950">
                  {feedback.reply.content}
                </p>
              </div>
            )}

            {/* Modal actions */}
            <div className="flex items-center justify-end gap-2 border-t border-neutral-100 pt-3">
              <button
                type="button"
                onClick={onClose}
                disabled={isSaving}
                className="rounded-xl border border-neutral-200 px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50 disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSaving || isUploading}
                className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 px-4 py-2 text-xs font-semibold text-white shadow-xs hover:bg-emerald-700 disabled:opacity-50"
              >
                {isSaving && <Loader2 className="size-3.5 animate-spin" />}
                Save changes
              </button>
            </div>
          </form>
        )}
      </SharedModal>

      {/* Image zoom preview modal */}
      {previewImage && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-60 flex items-center justify-center bg-black/80 p-4"
          onClick={() => setPreviewImage(null)}
        >
          <button
            type="button"
            onClick={() => setPreviewImage(null)}
            className="absolute right-4 top-4 rounded-full bg-white/20 p-2 text-white hover:bg-white/30"
          >
            <X className="size-5" />
          </button>
          <img
            src={previewImage}
            alt="Review enlarged view"
            className="max-h-[85vh] max-w-[90vw] rounded-xl object-contain shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          />
        </div>
      )}
    </>
  );
}
