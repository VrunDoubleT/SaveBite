import { useRef, useState } from "react";
import {
  Calendar,
  Camera,
  CheckCircle2,
  Loader2,
  MessageSquareQuote,
  Package,
  Pencil,
  Star,
  Store,
  Trash2,
  X,
} from "lucide-react";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import type { FeedbackItem } from "../types/feedback.types";
import { feedbackApi } from "../api/feedbackApi";
import { StarRatingInput } from "./StarRatingInput";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";

const RATING_LABELS: Record<number, string> = {
  5: "Excellent",
  4: "Good",
  3: "Okay",
  2: "Poor",
  1: "Terrible",
};

interface ReviewCardProps {
  review: FeedbackItem;
  onUpdate: (
    id: string,
    rating: number,
    comment?: string,
    images?: string[],
  ) => Promise<void>;
  onRequestDelete: (review: FeedbackItem) => void;
  isHighlighted?: boolean;
}

export function ReviewCard({
  review,
  onUpdate,
  onRequestDelete,
  isHighlighted = false,
}: ReviewCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [rating, setRating] = useState(review.rating);
  const [comment, setComment] = useState(review.comment ?? "");
  const [images, setImages] = useState<string[]>(review.images ?? []);
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [previewImage, setPreviewImage] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const startEdit = () => {
    setRating(review.rating);
    setComment(review.comment ?? "");
    setImages(review.images ?? []);
    setIsEditing(true);
  };

  const cancelEdit = () => {
    setIsEditing(false);
    setRating(review.rating);
    setComment(review.comment ?? "");
    setImages(review.images ?? []);
  };

  const handleImageUpload = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    const remainingSlots = 5 - images.length;
    if (remainingSlots <= 0) {
      toast.info("You can only attach up to 5 images.");
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

  const saveEdit = async () => {
    try {
      setIsSaving(true);
      await onUpdate(review.id, rating, comment.trim() || undefined, images);
      setIsEditing(false);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <article
      id={`feedback-${review.id}`}
      className={`rounded-2xl border bg-white p-5 sm:p-6 shadow-xs transition-all duration-300 hover:border-neutral-300 hover:shadow-sm ${
        isHighlighted
          ? "border-emerald-500 ring-4 ring-emerald-500/20 bg-emerald-50/15 shadow-md"
          : "border-neutral-200"
      }`}
    >
      {/* Shop & Order info header */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-neutral-100 pb-3.5">
        <div className="flex min-w-0 items-center gap-3">
          <span className="relative flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full border border-neutral-200 bg-emerald-50 text-emerald-700">
            <Store className="size-4" />
            {review.shopLogoUrl && (
              <img
                src={review.shopLogoUrl}
                alt={review.shopName || "Shop"}
                onError={(ev) => {
                  ev.currentTarget.style.display = "none";
                }}
                className="absolute inset-0 size-full object-cover"
              />
            )}
          </span>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <strong className="truncate text-sm font-bold text-neutral-900">
                {review.shopName || "Store"}
              </strong>
              {review.orderCode && review.orderId ? (
                <Link
                  to={APP_PATHS.ORDER_DETAIL.replace(
                    ":orderId",
                    encodeURIComponent(review.orderId),
                  )}
                  className="inline-flex items-center gap-1 rounded-md bg-neutral-100 px-2 py-0.5 text-xs font-semibold text-neutral-700 hover:bg-emerald-50 hover:text-emerald-700 transition-colors"
                  title="View order receipt"
                >
                  #{review.orderCode}
                </Link>
              ) : review.orderCode ? (
                <span className="inline-flex items-center gap-1 rounded-md bg-neutral-100 px-2 py-0.5 text-xs font-semibold text-neutral-700">
                  #{review.orderCode}
                </span>
              ) : null}
            </div>
            <p className="flex items-center gap-1 text-xs font-medium text-emerald-700 mt-0.5">
              <CheckCircle2 size={13} className="shrink-0" />
              Verified completed order
            </p>
          </div>
        </div>

        <div className="flex items-center gap-1.5 rounded-lg border border-neutral-100 bg-neutral-50 px-2.5 py-1 text-xs font-medium text-neutral-500">
          <Calendar size={13} className="shrink-0 text-neutral-400" />
          <time dateTime={review.createdAt}>
            {new Date(review.createdAt).toLocaleDateString("en-GB", {
              day: "2-digit",
              month: "short",
              year: "numeric",
            })}
          </time>
        </div>
      </div>

      {/* Product row */}
      <div className="mt-3.5 flex items-center gap-3.5 rounded-xl border border-neutral-100 bg-neutral-50/80 p-3">
        <span className="relative flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-neutral-200 bg-white text-neutral-400 shadow-2xs">
          <Package className="size-5" />
          {review.imageUrl && (
            <img
              src={review.imageUrl}
              alt={review.productName}
              onError={(ev) => {
                ev.currentTarget.style.display = "none";
              }}
              className="absolute inset-0 size-full object-cover"
            />
          )}
        </span>
        <div className="min-w-0 flex-1">
          <strong className="block truncate text-sm font-bold text-neutral-900">
            {review.productName || "Purchased item"}
          </strong>
          {review.variantName && (
            <span className="mt-1 inline-block rounded border border-neutral-200 bg-white px-2 py-0.5 text-[11px] font-medium text-neutral-600">
              {review.variantName}
            </span>
          )}
        </div>
      </div>

      {/* VIEW MODE */}
      {!isEditing && (
        <div className="mt-3.5">
          {/* Star rating & status text */}
          <div className="flex items-center gap-2">
            <div className="flex items-center gap-0.5">
              {Array.from({ length: 5 }, (_, index) => (
                <Star
                  key={index}
                  className="size-4.5 text-amber-400"
                  fill={index < review.rating ? "currentColor" : "none"}
                />
              ))}
            </div>
            <span className="text-xs font-bold text-neutral-900">
              {review.rating}/5
            </span>
            <span className="text-xs font-medium text-neutral-300">·</span>
            <span className="text-xs font-semibold text-emerald-700">
              {RATING_LABELS[review.rating] || "Rated"}
            </span>
          </div>

          {/* Comment text */}
          {review.comment ? (
            <p className="mt-2.5 text-sm leading-relaxed text-neutral-800 font-normal whitespace-pre-line">
              {review.comment}
            </p>
          ) : (
            <p className="mt-2 text-xs italic text-neutral-400">
              No written comment provided.
            </p>
          )}

          {/* Customer review photos */}
          {review.images && review.images.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-2.5">
              {review.images.map((img, idx) => (
                <button
                  key={idx}
                  type="button"
                  onClick={() => setPreviewImage(img)}
                  className="group relative size-16 overflow-hidden rounded-xl border border-neutral-200 shadow-2xs transition hover:scale-105 hover:border-emerald-500 cursor-pointer"
                  title="View full image"
                >
                  <img
                    src={img}
                    alt={`Review photo ${idx + 1}`}
                    className="size-full object-cover transition duration-200 group-hover:scale-105"
                  />
                </button>
              ))}
            </div>
          )}

          {/* Store response */}
          {review.reply && (
            <div className="mt-3.5 rounded-xl border border-emerald-200/80 bg-emerald-50/60 p-3.5">
              <div className="flex items-center gap-2 text-xs font-bold text-emerald-900">
                <MessageSquareQuote className="size-4 text-emerald-700 shrink-0" />
                <span>Response from shop</span>
                {review.reply.repliedByName && (
                  <span className="font-normal text-emerald-800">
                    ({review.reply.repliedByName})
                  </span>
                )}
                <span className="text-[11px] font-normal text-emerald-700/80">
                  ·{" "}
                  {new Date(review.reply.createdAt).toLocaleDateString("en-GB", {
                    day: "2-digit",
                    month: "short",
                    year: "numeric",
                  })}
                </span>
              </div>
              <p className="mt-1.5 text-xs leading-relaxed text-emerald-950 font-medium whitespace-pre-wrap">
                {review.reply.content}
              </p>
            </div>
          )}

          {/* Bottom actions */}
          <div className="mt-4 flex items-center justify-end gap-2 border-t border-neutral-100 pt-3">
            <button
              type="button"
              onClick={startEdit}
              className="inline-flex items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3.5 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 hover:border-neutral-300"
            >
              <Pencil className="size-3.5 text-neutral-500" />
              Edit review
            </button>

            <button
              type="button"
              onClick={() => onRequestDelete(review)}
              className="inline-flex items-center gap-1.5 rounded-xl border border-red-100 bg-red-50/70 px-3.5 py-1.5 text-xs font-semibold text-red-600 transition hover:bg-red-100/80"
            >
              <Trash2 className="size-3.5" />
              Delete
            </button>
          </div>
        </div>
      )}

      {/* INLINE EDIT MODE */}
      {isEditing && (
        <div className="mt-3.5 rounded-xl border border-emerald-200 bg-neutral-50/80 p-4 sm:p-5">
          <div className="mb-3.5 flex items-center justify-between border-b border-neutral-200/60 pb-3">
            <div>
              <h3 className="text-sm font-bold text-neutral-900">
                Edit your review
              </h3>
              <p className="text-xs text-neutral-500">
                Update your rating, comment, and photos for {review.productName}
              </p>
            </div>
            <button
              type="button"
              onClick={cancelEdit}
              aria-label="Cancel editing"
              className="rounded-full p-1.5 text-neutral-400 hover:bg-neutral-200 hover:text-neutral-700 transition"
            >
              <X className="size-4" />
            </button>
          </div>

          <div>
            <label className="mb-1.5 block text-xs font-bold text-neutral-700 uppercase tracking-wider">
              Rating
            </label>
            <StarRatingInput
              value={rating}
              onChange={setRating}
              disabled={isSaving}
              size="lg"
            />
          </div>

          <div className="mt-3.5">
            <div className="mb-1 flex items-center justify-between">
              <label className="text-xs font-semibold text-neutral-700">
                Review comment
              </label>
              <span className="text-[11px] text-neutral-400">
                {comment.length}/2000
              </span>
            </div>
            <textarea
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              rows={3}
              maxLength={2000}
              disabled={isSaving}
              placeholder="Share your updated experience..."
              className="w-full resize-none rounded-xl border border-neutral-200 bg-white p-3 text-xs sm:text-sm text-neutral-900 outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-500/20"
            />
          </div>

          {/* Edit photos */}
          <div className="mt-3.5">
            <div className="mb-2 flex items-center justify-between">
              <label className="block text-xs font-semibold text-neutral-700">
                Photos <span className="font-normal text-neutral-400">(max 5)</span>
              </label>
              <span className="text-[11px] text-neutral-400">
                {images.length}/5 photos
              </span>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              {images.map((imgUrl, imgIdx) => (
                <div
                  key={imgIdx}
                  className="group relative size-16 overflow-hidden rounded-xl border border-neutral-200"
                >
                  <img
                    src={imgUrl}
                    alt={`Photo ${imgIdx + 1}`}
                    className="size-full object-cover"
                  />
                  <button
                    type="button"
                    onClick={() => handleRemoveImage(imgIdx)}
                    disabled={isSaving}
                    className="absolute right-1 top-1 flex size-5 items-center justify-center rounded-full bg-black/60 text-white transition hover:bg-black"
                    title="Remove image"
                  >
                    <X className="size-3" />
                  </button>
                </div>
              ))}

              {images.length < 5 && (
                <button
                  type="button"
                  disabled={isSaving || isUploading}
                  onClick={() => fileInputRef.current?.click()}
                  className="flex size-16 flex-col items-center justify-center gap-1 rounded-xl border border-dashed border-neutral-300 bg-white text-neutral-500 transition hover:border-emerald-500 hover:text-emerald-700 disabled:opacity-50 cursor-pointer"
                >
                  {isUploading ? (
                    <Loader2 className="size-5 animate-spin text-emerald-600" />
                  ) : (
                    <>
                      <Camera className="size-5" />
                      <span className="text-[10px] font-medium">Add photo</span>
                    </>
                  )}
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
          </div>

          <div className="mt-4 flex justify-end gap-2 border-t border-neutral-200/60 pt-3">
            <button
              type="button"
              disabled={isSaving || isUploading}
              onClick={cancelEdit}
              className="rounded-xl border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-100 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="button"
              disabled={isSaving || isUploading}
              onClick={saveEdit}
              className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 px-4 py-2 text-xs font-semibold text-white shadow-xs hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSaving && <Loader2 className="size-3.5 animate-spin" />}
              Save changes
            </button>
          </div>
        </div>
      )}

      {/* Lightbox / Zoom Image Modal */}
      {previewImage && (
        <div
          role="dialog"
          aria-modal="true"
          onClick={() => setPreviewImage(null)}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4 backdrop-blur-xs"
        >
          <div className="relative max-h-[90vh] max-w-[90vw] overflow-hidden rounded-2xl bg-neutral-900">
            <img
              src={previewImage}
              alt="Zoomed review"
              className="max-h-[85vh] w-auto object-contain"
            />
            <button
              type="button"
              onClick={() => setPreviewImage(null)}
              className="absolute right-3 top-3 rounded-full bg-black/60 px-3 py-1 text-xs font-bold text-white transition hover:bg-black cursor-pointer"
            >
              Close ✕
            </button>
          </div>
        </div>
      )}
    </article>
  );
}
