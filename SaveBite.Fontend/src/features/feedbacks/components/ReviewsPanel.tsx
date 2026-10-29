import { useState } from "react";
import { ArrowRight, MessageSquareText } from "lucide-react";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { FeedbackItem } from "../types/feedback.types";
import { ReviewCard } from "./ReviewCard";

interface ReviewsPanelProps {
  reviews: FeedbackItem[];
  onDelete: (id: string) => Promise<void>;
  onUpdate: (
    id: string,
    rating: number,
    comment?: string,
    images?: string[],
  ) => Promise<void>;
  isLoading?: boolean;
  highlightedId?: string | null;
}

export function ReviewsPanel({
  reviews,
  onDelete,
  onUpdate,
  isLoading = false,
  highlightedId,
}: ReviewsPanelProps) {
  const [reviewToDelete, setReviewToDelete] = useState<FeedbackItem | null>(
    null,
  );
  const [isDeleting, setIsDeleting] = useState(false);

  const confirmDelete = async () => {
    if (!reviewToDelete) return;
    try {
      setIsDeleting(true);
      await onDelete(reviewToDelete.id);
      setReviewToDelete(null);
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="space-y-4">
      {/* Header Card */}
      <div className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-xs sm:p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-xl font-bold text-neutral-900">My reviews</h2>
            <p className="mt-1 text-xs text-neutral-500 sm:text-sm">
              Reviews you&apos;ve written for completed orders.
            </p>
          </div>
          {reviews.length > 0 && (
            <div className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-800">
              <MessageSquareText className="size-4 text-emerald-600" />
              <span>
                {reviews.length} {reviews.length === 1 ? "review" : "reviews"} on this page
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Reviews Cards List */}
      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((item) => (
            <div
              key={item}
              className="h-44 animate-pulse rounded-2xl border border-neutral-200 bg-white p-5 shadow-xs"
            />
          ))}
        </div>
      ) : reviews.length > 0 ? (
        <div className="space-y-4">
          {reviews.map((review) => (
            <ReviewCard
              key={review.id}
              review={review}
              onUpdate={onUpdate}
              onRequestDelete={setReviewToDelete}
              isHighlighted={review.id === highlightedId}
            />
          ))}
        </div>
      ) : (
        <div className="rounded-2xl border border-neutral-200 bg-white py-12 px-6 text-center shadow-xs">
          <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-700">
            <MessageSquareText className="size-6" />
          </span>
          <h3 className="mt-4 text-base font-bold text-neutral-900">
            You haven&apos;t written any reviews yet
          </h3>
          <p className="mx-auto mt-1 max-w-md text-sm text-neutral-500">
            Once an order is picked up and completed, you can rate its products
            from your order history.
          </p>
          <Link
            to={`${APP_PATHS.ACCOUNT_ORDERS}?status=Completed`}
            className="mt-5 inline-flex items-center gap-2 rounded-full bg-emerald-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-700 shadow-xs"
          >
            Go to completed orders
            <ArrowRight size={16} />
          </Link>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      <SharedModal
        isOpen={Boolean(reviewToDelete)}
        onClose={() => !isDeleting && setReviewToDelete(null)}
        title="Delete review"
        maxWidthClass="max-w-md"
      >
        <div className="space-y-3">
          <p className="text-sm text-neutral-600">
            Are you sure you want to delete your review for{" "}
            <span className="font-semibold text-neutral-900">
              {reviewToDelete?.productName}
            </span>
            ? This action cannot be undone.
          </p>
          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              disabled={isDeleting}
              onClick={() => setReviewToDelete(null)}
              className="rounded-lg border border-neutral-200 px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50"
            >
              Cancel
            </button>
            <button
              type="button"
              disabled={isDeleting}
              onClick={confirmDelete}
              className="rounded-lg bg-red-600 px-4 py-2 text-xs font-semibold text-white hover:bg-red-700 disabled:opacity-50"
            >
              {isDeleting ? "Deleting..." : "Delete review"}
            </button>
          </div>
        </div>
      </SharedModal>
    </div>
  );
}
