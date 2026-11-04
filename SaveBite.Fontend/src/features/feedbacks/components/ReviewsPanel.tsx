import { useState } from "react";
import { MessageSquareText } from "lucide-react";
import { Button, ButtonLink, Card, EmptyState, Skeleton } from "@/shared/components/ui";
import { getApiErrorMessage } from "@/shared/api";
import { APP_PATHS } from "@/app/router/paths";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import type { FeedbackItem } from "../types/feedback.types";
import { ReviewCard } from "./ReviewCard";

interface ReviewsPanelProps {
  reviews: FeedbackItem[];
  onDelete: (id: string) => Promise<void>;
  onUpdate: (id: string, rating: number, comment?: string, images?: string[]) => Promise<void>;
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
  const [reviewToDelete, setReviewToDelete] = useState<FeedbackItem | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState("");

  const confirmDelete = async () => {
    if (!reviewToDelete) return;
    try {
      setIsDeleting(true);
      await onDelete(reviewToDelete.id);
      setReviewToDelete(null);
    } catch (error) {
      setDeleteError(getApiErrorMessage(error));
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="space-y-4">
      {reviews.length > 0 && (
        <p className="text-caption text-text-secondary">
          {reviews.length} {reviews.length === 1 ? "review" : "reviews"} on this page
        </p>
      )}

      {/* Reviews Cards List */}
      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((item) => (
            <Card padding="compact" key={item} className="space-y-4">
              <Skeleton className="h-12 w-2/3" />
              <Skeleton className="h-20" />
              <Skeleton className="h-10 w-1/3" />
            </Card>
          ))}
        </div>
      ) : reviews.length > 0 ? (
        <div className="space-y-4">
          {reviews.map((review) => (
            <ReviewCard
              key={review.id}
              review={review}
              onUpdate={onUpdate}
              onRequestDelete={(review) => {
                setDeleteError("");
                setReviewToDelete(review);
              }}
              isHighlighted={review.id === highlightedId}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={MessageSquareText}
          title="You haven't written any reviews yet"
          description="Once an order is picked up and completed, you can rate its products from your order history."
          action={
            <ButtonLink to={`${APP_PATHS.ACCOUNT_ORDERS}?status=Completed`}>
              Go to completed orders
            </ButtonLink>
          }
        />
      )}

      {/* Delete Confirmation Modal */}
      <SharedModal
        isOpen={Boolean(reviewToDelete)}
        onClose={() => !isDeleting && setReviewToDelete(null)}
        title="Delete review"
        maxWidthClass="max-w-md"
      >
        <div className="space-y-3">
          <p className="text-sm text-text-secondary">
            Are you sure you want to delete your review for{" "}
            <span className="font-semibold text-text-primary">{reviewToDelete?.productName}</span>?
            This action cannot be undone.
          </p>
          {deleteError && (
            <p role="alert" className="rounded-md bg-danger-soft p-3 text-body text-danger-strong">
              {deleteError}
            </p>
          )}
          <div className="flex flex-wrap justify-end gap-2 pt-2">
            <Button
              variant="secondary"
              type="button"
              disabled={isDeleting}
              onClick={() => setReviewToDelete(null)}
              className="min-h-11"
            >
              Cancel
            </Button>
            <Button
              variant="danger"
              loading={isDeleting}
              type="button"
              disabled={isDeleting}
              onClick={confirmDelete}
              className="min-h-11"
            >
              {isDeleting ? "Deleting..." : "Delete review"}
            </Button>
          </div>
        </div>
      </SharedModal>
    </div>
  );
}
