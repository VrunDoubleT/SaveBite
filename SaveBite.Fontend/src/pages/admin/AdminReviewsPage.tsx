import { useCallback, useEffect, useState } from "react";
import { feedbackApi } from "@/features/feedbacks/api/feedbackApi";
import { ShopFeedbackManagementPanel } from "@/features/feedbacks/components/ShopFeedbackManagementPanel";
import type { FeedbackItem } from "@/features/feedbacks/types/feedback.types";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";

export function AdminReviewsPage() {
  const [feedbacks, setFeedbacks] = useState<FeedbackItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [activeRating, setActiveRating] = useState<number | null>(null);
  const [activeReplyFilter, setActiveReplyFilter] = useState<boolean | null>(null);
  const [isToday, setIsToday] = useState(false);

  const fetchFeedbacks = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await feedbackApi.getAdminFeedbacks({
        page,
        pageSize,
        rating: activeRating ?? undefined,
        hasReply: activeReplyFilter ?? undefined,
        isToday: isToday || undefined,
      });

      setFeedbacks(response.items);
      setTotalItems(response.totalItems);
      setTotalPages(response.totalPages);
    } catch (error) {
      toast.error(getApiErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, activeRating, activeReplyFilter, isToday]);

  useEffect(() => {
    void fetchFeedbacks();
  }, [fetchFeedbacks]);

  const handleRatingChange = (rating: number | null) => {
    setActiveRating(rating);
    setPage(1);
  };

  const handleReplyFilterChange = (hasReply: boolean | null) => {
    setActiveReplyFilter(hasReply);
    if (hasReply === null) {
      setIsToday(false);
    }
    setPage(1);
  };

  const handleTodayToggle = () => {
    setIsToday((prev) => !prev);
    setPage(1);
  };

  const handlePageChange = (newPage: number) => {
    setPage(newPage);
  };

  const handleReply = async (feedbackId: string, content: string) => {
    const reply = await feedbackApi.replyFeedback(feedbackId, { content });
    setFeedbacks((prev) =>
      prev.map((item) => (item.id === feedbackId ? { ...item, reply } : item)),
    );
  };

  const handleEditReply = async (feedbackId: string, content: string) => {
    const reply = await feedbackApi.editReply(feedbackId, { content });
    setFeedbacks((prev) =>
      prev.map((item) => (item.id === feedbackId ? { ...item, reply } : item)),
    );
  };

  const handleDeleteReply = async (feedbackId: string) => {
    await feedbackApi.deleteReply(feedbackId);
    setFeedbacks((prev) =>
      prev.map((item) =>
        item.id === feedbackId ? { ...item, reply: undefined } : item,
      ),
    );
  };

  return (
    <div className="py-2">
      <ShopFeedbackManagementPanel
        title="Platform Feedbacks & Reviews"
        subtitle="Overview of customer reviews across all registered stores and response moderation."
        feedbacks={feedbacks}
        isLoading={isLoading}
        page={page}
        pageSize={pageSize}
        totalItems={totalItems}
        totalPages={totalPages}
        activeRating={activeRating}
        onRatingChange={handleRatingChange}
        activeReplyFilter={activeReplyFilter}
        onReplyFilterChange={handleReplyFilterChange}
        isToday={isToday}
        onTodayToggle={handleTodayToggle}
        onPageChange={handlePageChange}
        onReply={handleReply}
        onEditReply={handleEditReply}
        onDeleteReply={handleDeleteReply}
        showShopName={true}
      />
    </div>
  );
}

export default AdminReviewsPage;
