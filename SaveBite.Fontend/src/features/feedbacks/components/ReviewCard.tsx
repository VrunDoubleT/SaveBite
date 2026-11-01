import { useState } from "react";
import {
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
import { StarRatingInput } from "./StarRatingInput";

interface ReviewCardProps {
  review: FeedbackItem;
  onUpdate: (id: string, rating: number, comment?: string) => Promise<void>;
  onRequestDelete: (review: FeedbackItem) => void;
}

export function ReviewCard({
  review,
  onUpdate,
  onRequestDelete,
}: ReviewCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [rating, setRating] = useState(review.rating);
  const [comment, setComment] = useState(review.comment ?? "");
  const [isSaving, setIsSaving] = useState(false);

  const startEdit = () => {
    setRating(review.rating);
    setComment(review.comment ?? "");
    setIsEditing(true);
  };

  const cancelEdit = () => {
    setIsEditing(false);
    setRating(review.rating);
    setComment(review.comment ?? "");
  };

  const saveEdit = async () => {
    try {
      setIsSaving(true);
      await onUpdate(review.id, rating, comment.trim() || undefined);
      setIsEditing(false);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <article className="py-5 first:pt-2 last:pb-0">
      {/* Shop & Order info header */}
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-2.5">
          <span className="relative flex size-9 shrink-0 items-center justify-center overflow-hidden rounded-full bg-emerald-50 text-emerald-700">
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
              <strong className="truncate text-sm font-semibold text-neutral-900">
                {review.shopName || "Store"}
              </strong>
              {review.orderCode && review.orderId ? (
                <Link
                  to={APP_PATHS.ORDER_DETAIL.replace(
                    ":orderId",
                    encodeURIComponent(review.orderId),
                  )}
                  className="rounded bg-neutral-100 px-1.5 py-0.5 text-[11px] font-medium text-neutral-600 hover:bg-emerald-50 hover:text-emerald-700"
                >
                  #{review.orderCode}
                </Link>
              ) : review.orderCode ? (
                <span className="rounded bg-neutral-100 px-1.5 py-0.5 text-[11px] font-medium text-neutral-600">
                  #{review.orderCode}
                </span>
              ) : null}
            </div>
            <p className="text-xs text-neutral-500">
              Verified completed order
            </p>
          </div>
        </div>

        <span className="shrink-0 text-xs text-neutral-500">
          {new Date(review.createdAt).toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "short",
            year: "numeric",
          })}
        </span>
      </div>

      {/* Product row */}
      <div className="mt-3 flex items-center gap-3 rounded-xl bg-neutral-50 p-3">
        <span className="relative flex size-12 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-white text-neutral-400 shadow-2xs">
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
        <div className="min-w-0">
          <strong className="block truncate text-sm font-semibold text-neutral-900">
            {review.productName || "Purchased item"}
          </strong>
          {review.variantName && (
            <span className="mt-0.5 block text-xs text-neutral-500">
              {review.variantName}
            </span>
          )}
        </div>
      </div>

      {/* VIEW MODE */}
      {!isEditing && (
        <div className="mt-3">
          <div className="flex items-center gap-1">
            {Array.from({ length: 5 }, (_, index) => (
              <Star
                key={index}
                className="size-4 text-amber-400"
                fill={index < review.rating ? "currentColor" : "none"}
              />
            ))}
            <span className="ml-1.5 text-xs font-semibold text-neutral-700">
              {review.rating}/5
            </span>
          </div>

          {review.comment ? (
            <p className="mt-2 whitespace-pre-line text-sm leading-relaxed text-neutral-700">
              {review.comment}
            </p>
          ) : (
            <p className="mt-2 text-xs italic text-neutral-400">
              No written comment provided.
            </p>
          )}

          {review.reply && (
            <div className="mt-3 rounded-lg border-l-4 border-emerald-500 bg-emerald-50/70 px-4 py-3">
              <div className="flex items-center gap-1.5 text-xs font-semibold text-emerald-800">
                <MessageSquareQuote className="size-3.5" />
                <span>Response from shop</span>
                <span className="text-[11px] font-normal text-neutral-500">
                  ·{" "}
                  {new Date(review.reply.createdAt).toLocaleDateString("en-GB", {
                    day: "2-digit",
                    month: "short",
                    year: "numeric",
                  })}
                </span>
              </div>
              <p className="mt-1 text-xs leading-relaxed text-neutral-700">
                {review.reply.content}
              </p>
            </div>
          )}

          <div className="mt-3 flex items-center gap-2">
            <button
              type="button"
              onClick={startEdit}
              className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-700 transition hover:bg-neutral-50"
            >
              <Pencil className="size-3.5" />
              Edit review
            </button>

            <button
              type="button"
              onClick={() => onRequestDelete(review)}
              className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-red-600 transition hover:bg-red-50"
            >
              <Trash2 className="size-3.5" />
              Delete
            </button>
          </div>
        </div>
      )}

      {/* INLINE EDIT MODE */}
      {isEditing && (
        <div className="mt-3 rounded-xl border border-neutral-200 bg-neutral-50/70 p-4">
          <div className="mb-3 flex items-center justify-between">
            <div>
              <h3 className="text-sm font-semibold text-neutral-900">
                Edit your review
              </h3>
              <p className="text-xs text-neutral-500">
                Update your rating and comment for {review.productName}
              </p>
            </div>
            <button
              type="button"
              onClick={cancelEdit}
              aria-label="Cancel editing"
              className="rounded-full p-1 text-neutral-500 hover:bg-neutral-200"
            >
              <X className="size-4" />
            </button>
          </div>

          <div>
            <label className="mb-1.5 block text-xs font-medium text-neutral-700">
              Rating
            </label>
            <StarRatingInput
              value={rating}
              onChange={setRating}
              disabled={isSaving}
            />
          </div>

          <div className="mt-3">
            <div className="mb-1 flex items-center justify-between">
              <label className="text-xs font-medium text-neutral-700">
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
              className="w-full resize-none rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-900 outline-none focus:border-emerald-600 focus:ring-1 focus:ring-emerald-600"
            />
          </div>

          <div className="mt-3 flex gap-2">
            <button
              type="button"
              disabled={isSaving}
              onClick={saveEdit}
              className="rounded-lg bg-emerald-600 px-4 py-2 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSaving ? "Saving..." : "Save changes"}
            </button>
            <button
              type="button"
              disabled={isSaving}
              onClick={cancelEdit}
              className="rounded-lg border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-100 disabled:opacity-50"
            >
              Cancel
            </button>
          </div>
        </div>
      )}
    </article>
  );
}
