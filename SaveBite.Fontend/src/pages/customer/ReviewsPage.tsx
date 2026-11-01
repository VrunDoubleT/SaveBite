import { useCallback, useEffect, useState } from "react";
import { feedbackApi } from "@/features/feedbacks/api/feedbackApi";
import type { FeedbackItem } from "@/features/feedbacks/types/feedback.types";
import { ReviewsPanel } from "@/features/feedbacks/components/ReviewsPanel";
import { Pagination } from "@/shared/components/pagination";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";

export function ReviewsPage() {
  const [reviews, setReviews] = useState<FeedbackItem[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);

  const fetchReviews = useCallback(
    async (targetPage = 1) => {
      try {
        setIsLoading(true);
        const data = await feedbackApi.getHistory(targetPage, pageSize);
        setReviews(data.items);
        setPage(data.page);
        setTotalPages(data.totalPages);
        setTotalItems(data.totalItems);
      } catch (error) {
        toast.error(
          getApiErrorMessage(error) || "Failed to load your review history.",
        );
      } finally {
        setIsLoading(false);
      }
    },
    [pageSize],
  );

  useEffect(() => {
    void fetchReviews(page);
  }, [fetchReviews, page]);

  const handleUpdate = async (id: string, rating: number, comment?: string) => {
    try {
      const updated = await feedbackApi.editFeedback(id, { rating, comment });
      setReviews((prev) =>
        prev.map((item) => (item.id === id ? { ...item, ...updated } : item)),
      );
      toast.success("Review updated successfully.");
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to update review.");
      throw error;
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await feedbackApi.deleteFeedback(id);
      setReviews((prev) => prev.filter((item) => item.id !== id));
      setTotalItems((prev) => Math.max(0, prev - 1));
      toast.success("Review deleted successfully.");
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to delete review.");
      throw error;
    }
  };

  return (
    <div className="space-y-6">
      <ReviewsPanel
        reviews={reviews}
        isLoading={isLoading}
        onUpdate={handleUpdate}
        onDelete={handleDelete}
      />

      {totalPages > 1 && (
        <div className="flex justify-center pt-2">
          <Pagination
            currentPage={page}
            totalPages={totalPages}
            totalItems={totalItems}
            pageSize={pageSize}
            itemLabel="reviews"
            onPageChange={(newPage) => setPage(newPage)}
          />
        </div>
      )}
    </div>
  );
}
