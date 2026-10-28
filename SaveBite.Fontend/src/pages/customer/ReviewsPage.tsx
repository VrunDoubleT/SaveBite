import { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { feedbackApi } from "@/features/feedbacks/api/feedbackApi";
import type { FeedbackItem } from "@/features/feedbacks/types/feedback.types";
import { ReviewsPanel } from "@/features/feedbacks/components/ReviewsPanel";
import { Pagination } from "@/shared/components/pagination";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";

export function ReviewsPage() {
  const [searchParams] = useSearchParams();
  const feedbackIdParam = searchParams.get("feedbackId");

  const [reviews, setReviews] = useState<FeedbackItem[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [highlightedId, setHighlightedId] = useState<string | null>(feedbackIdParam);
  const initialDeepLinkHandled = useRef(false);

  const fetchReviews = useCallback(
    async (targetPage = 1, targetFeedbackId?: string) => {
      try {
        setIsLoading(true);
        const data = await feedbackApi.getHistory(
          targetPage,
          pageSize,
          targetFeedbackId,
        );
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
    // If arriving with a feedbackId, let backend locate the exact page
    if (feedbackIdParam && !initialDeepLinkHandled.current) {
      initialDeepLinkHandled.current = true;
      void fetchReviews(1, feedbackIdParam);
    } else {
      void fetchReviews(page);
    }
  }, [fetchReviews, page, feedbackIdParam]);

  // Scroll and pulse highlight when target review appears on screen
  useEffect(() => {
    if (feedbackIdParam && reviews.some((r) => r.id === feedbackIdParam)) {
      setHighlightedId(feedbackIdParam);
      const timer = setTimeout(() => {
        const el = document.getElementById(`feedback-${feedbackIdParam}`);
        if (el) {
          el.scrollIntoView({ behavior: "smooth", block: "center" });
        }
      }, 200);

      const clearTimer = setTimeout(() => {
        setHighlightedId(null);
      }, 3500);

      return () => {
        clearTimeout(timer);
        clearTimeout(clearTimer);
      };
    }
  }, [feedbackIdParam, reviews]);

  const handleUpdate = async (
    id: string,
    rating: number,
    comment?: string,
    images?: string[],
  ) => {
    try {
      const updated = await feedbackApi.editFeedback(id, {
        rating,
        comment,
        images,
      });
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
        highlightedId={highlightedId}
      />

      {totalPages > 1 && (
        <div className="flex justify-center pt-2">
          <Pagination
            currentPage={page}
            totalPages={totalPages}
            totalItems={totalItems}
            pageSize={pageSize}
            itemLabel="reviews"
            onPageChange={(newPage) => {
              setHighlightedId(null);
              setPage(newPage);
            }}
          />
        </div>
      )}
    </div>
  );
}
