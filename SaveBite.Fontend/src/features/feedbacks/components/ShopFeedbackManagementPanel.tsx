import { useState } from "react";
import {
  MessageSquare,
  MessageSquareQuote,
  Package,
  Pencil,
  Send,
  Star,
  Trash2,
} from "lucide-react";
import type { FeedbackItem } from "../types/feedback.types";
import { Pagination } from "@/shared/components/pagination";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api";

interface ShopFeedbackManagementPanelProps {
  title: string;
  subtitle: string;
  feedbacks: FeedbackItem[];
  isLoading: boolean;
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  activeRating: number | null;
  onRatingChange: (rating: number | null) => void;
  activeReplyFilter: boolean | null;
  onReplyFilterChange: (hasReply: boolean | null) => void;
  isToday?: boolean;
  onTodayToggle?: () => void;
  onPageChange: (newPage: number) => void;
  onReply: (feedbackId: string, content: string) => Promise<void>;
  onEditReply: (feedbackId: string, content: string) => Promise<void>;
  onDeleteReply: (feedbackId: string) => Promise<void>;
  showShopName?: boolean;
}

export function ShopFeedbackManagementPanel({
  title,
  subtitle,
  feedbacks,
  isLoading,
  page,
  pageSize,
  totalItems,
  totalPages,
  activeRating,
  onRatingChange,
  activeReplyFilter,
  onReplyFilterChange,
  isToday = false,
  onTodayToggle,
  onPageChange,
  onReply,
  onEditReply,
  onDeleteReply,
  showShopName = false,
}: ShopFeedbackManagementPanelProps) {
  const [selectedImage, setSelectedImage] = useState<string | null>(null);

  // Reply modal state
  const [replyTarget, setReplyTarget] = useState<FeedbackItem | null>(null);
  const [replyContent, setReplyContent] = useState("");
  const [isSubmittingReply, setIsSubmittingReply] = useState(false);

  // Edit reply modal state
  const [editReplyTarget, setEditReplyTarget] = useState<FeedbackItem | null>(null);
  const [editReplyContent, setEditReplyContent] = useState("");
  const [isSubmittingEdit, setIsSubmittingEdit] = useState(false);

  // Delete reply modal state
  const [deleteReplyTarget, setDeleteReplyTarget] = useState<FeedbackItem | null>(null);
  const [isSubmittingDelete, setIsSubmittingDelete] = useState(false);

  const openReplyModal = (feedback: FeedbackItem) => {
    setReplyTarget(feedback);
    setReplyContent("");
  };

  const openEditReplyModal = (feedback: FeedbackItem) => {
    setEditReplyTarget(feedback);
    setEditReplyContent(feedback.reply?.content ?? "");
  };

  const handleReplySubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!replyTarget || !replyContent.trim()) return;
    try {
      setIsSubmittingReply(true);
      await onReply(replyTarget.id, replyContent.trim());
      toast.success("Reply submitted successfully.");
      setReplyTarget(null);
      setReplyContent("");
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to submit reply.");
    } finally {
      setIsSubmittingReply(false);
    }
  };

  const handleEditReplySubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editReplyTarget || !editReplyContent.trim()) return;
    try {
      setIsSubmittingEdit(true);
      await onEditReply(editReplyTarget.id, editReplyContent.trim());
      toast.success("Reply updated successfully.");
      setEditReplyTarget(null);
      setEditReplyContent("");
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to update reply.");
    } finally {
      setIsSubmittingEdit(false);
    }
  };

  const handleDeleteReplySubmit = async () => {
    if (!deleteReplyTarget) return;
    try {
      setIsSubmittingDelete(true);
      await onDeleteReply(deleteReplyTarget.id);
      toast.success("Reply deleted successfully.");
      setDeleteReplyTarget(null);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to delete reply.");
    } finally {
      setIsSubmittingDelete(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-xs sm:p-6">
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-neutral-100 pb-4">
          <div>
            <h1 className="text-xl font-bold text-neutral-900">{title}</h1>
            <p className="mt-1 text-xs text-neutral-500 sm:text-sm">{subtitle}</p>
          </div>
          <div className="flex items-center gap-2 rounded-xl bg-emerald-50 px-3.5 py-1.5 text-xs font-semibold text-emerald-800">
            <Star className="size-4 fill-amber-400 text-amber-500" />
            <span>Total Reviews: {totalItems}</span>
          </div>
        </div>

        {/* Filters */}
        <div className="mt-4 flex flex-wrap items-center gap-2">
          {/* Status Filter */}
          <button
            type="button"
            onClick={() => onReplyFilterChange(null)}
            className={`rounded-xl px-3.5 py-1.5 text-xs font-semibold transition shadow-2xs ${
              activeReplyFilter === null && !isToday
                ? "bg-emerald-600 text-white"
                : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
            }`}
          >
            All Reviews
          </button>
          <button
            type="button"
            onClick={() => onReplyFilterChange(false)}
            className={`rounded-xl px-3.5 py-1.5 text-xs font-semibold transition shadow-2xs ${
              activeReplyFilter === false
                ? "bg-emerald-600 text-white"
                : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
            }`}
          >
            Needs Reply
          </button>
          <button
            type="button"
            onClick={() => onReplyFilterChange(true)}
            className={`rounded-xl px-3.5 py-1.5 text-xs font-semibold transition shadow-2xs ${
              activeReplyFilter === true
                ? "bg-emerald-600 text-white"
                : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
            }`}
          >
            Replied
          </button>
          {onTodayToggle && (
            <button
              type="button"
              onClick={onTodayToggle}
              className={`rounded-xl px-3.5 py-1.5 text-xs font-semibold transition shadow-2xs ${
                isToday
                  ? "bg-emerald-600 text-white"
                  : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
              }`}
            >
              Today
            </button>
          )}

          <span className="h-5 w-px bg-neutral-200 mx-1 hidden sm:block" />

          {/* Star Filter */}
          <button
            type="button"
            onClick={() => onRatingChange(null)}
            className={`rounded-xl px-3 py-1.5 text-xs font-semibold transition shadow-2xs ${
              activeRating === null
                ? "bg-neutral-900 text-white"
                : "border border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-50"
            }`}
          >
            All Stars
          </button>
          {[5, 4, 3, 2, 1].map((stars) => (
            <button
              key={stars}
              type="button"
              onClick={() => onRatingChange(stars)}
              className={`inline-flex items-center gap-1 rounded-xl px-2.5 py-1.5 text-xs font-semibold transition shadow-2xs ${
                activeRating === stars
                  ? "bg-neutral-900 text-white"
                  : "border border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-50"
              }`}
            >
              <span>{stars}</span>
              <Star className="size-3 fill-amber-400 text-amber-500" />
            </button>
          ))}
        </div>
      </div>

      {/* Reviews list */}
      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="h-44 animate-pulse rounded-2xl border border-neutral-200 bg-white p-5"
            />
          ))}
        </div>
      ) : feedbacks.length > 0 ? (
        <div className="space-y-4">
          {feedbacks.map((item) => (
            <article
              key={item.id}
              className="rounded-2xl border border-neutral-200 bg-white p-5 shadow-xs transition hover:border-neutral-300"
            >
              {/* Header: Customer info & time */}
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="relative flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full border border-amber-200 bg-amber-50 text-amber-700">
                    {item.customerAvatarUrl ? (
                      <img
                        src={item.customerAvatarUrl}
                        alt={item.customerName || "Customer"}
                        className="size-full object-cover"
                      />
                    ) : (
                      <span className="text-xs font-bold">
                        {(item.customerName || "C").charAt(0).toUpperCase()}
                      </span>
                    )}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-bold text-neutral-900">
                        {item.customerName || "Customer"}
                      </span>
                      {showShopName && item.shopName && (
                        <span className="rounded bg-neutral-100 px-2 py-0.5 text-[11px] font-medium text-neutral-600">
                          {item.shopName}
                        </span>
                      )}
                      {item.orderCode && (
                        <span className="rounded bg-neutral-100 px-1.5 py-0.5 text-[10px] font-medium text-neutral-500">
                          #{item.orderCode}
                        </span>
                      )}
                    </div>
                    {/* Stars */}
                    <div className="mt-1 flex items-center gap-0.5">
                      {Array.from({ length: 5 }, (_, idx) => (
                        <Star
                          key={idx}
                          size={13}
                          className={
                            idx < item.rating
                              ? "fill-amber-400 text-amber-400"
                              : "fill-neutral-200 text-neutral-200"
                          }
                        />
                      ))}
                    </div>
                  </div>
                </div>

                <span className="text-xs text-neutral-400">
                  {new Date(item.createdAt).toLocaleDateString("en-GB", {
                    day: "2-digit",
                    month: "short",
                    year: "numeric",
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </span>
              </div>

              {/* Product Info tag */}
              <div className="mt-3 flex items-center gap-2.5 rounded-xl border border-neutral-100 bg-neutral-50/80 p-2 text-xs">
                {item.imageUrl ? (
                  <img
                    src={item.imageUrl}
                    alt={item.productName}
                    className="size-9 rounded-lg object-cover shrink-0 border border-neutral-200"
                  />
                ) : (
                  <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-neutral-100 text-neutral-400">
                    <Package size={16} />
                  </div>
                )}
                <div className="min-w-0">
                  <span className="block font-semibold text-neutral-800 truncate">
                    {item.productName}
                  </span>
                  {item.variantName && (
                    <span className="text-[11px] text-neutral-500">
                      {item.variantName}
                    </span>
                  )}
                </div>
              </div>

              {/* Comment text */}
              {item.comment ? (
                <p className="mt-3 text-xs sm:text-sm leading-relaxed text-neutral-700 whitespace-pre-line">
                  {item.comment}
                </p>
              ) : (
                <p className="mt-3 text-xs italic text-neutral-400">
                  Customer left no written comment.
                </p>
              )}

              {/* Attached review photos */}
              {item.images && item.images.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-2">
                  {item.images.map((img, idx) => (
                    <button
                      key={idx}
                      type="button"
                      onClick={() => setSelectedImage(img)}
                      className="group relative size-16 overflow-hidden rounded-xl border border-neutral-200 shadow-2xs transition hover:opacity-90"
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

              {/* Store Response Section */}
              {item.reply ? (
                <div className="mt-4 rounded-xl border border-emerald-100 bg-emerald-50/70 p-3.5 text-xs animate-fade-in">
                  <div className="flex items-center justify-between gap-2">
                    <div className="flex items-center gap-1.5 font-bold text-emerald-800">
                      <MessageSquareQuote size={14} className="text-emerald-600 shrink-0" />
                      <span>{item.reply.repliedByName || "Store Response"}</span>
                      <span className="text-neutral-400 font-normal">•</span>
                      <span className="text-[11px] font-normal text-neutral-500">
                        {new Date(item.reply.createdAt).toLocaleDateString("en-GB", {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                        })}
                      </span>
                    </div>

                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        onClick={() => openEditReplyModal(item)}
                        className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-semibold text-neutral-600 hover:bg-emerald-100/60 hover:text-emerald-800 transition"
                      >
                        <Pencil size={12} />
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeleteReplyTarget(item)}
                        className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-semibold text-red-600 hover:bg-red-50 transition"
                      >
                        <Trash2 size={12} />
                        Delete
                      </button>
                    </div>
                  </div>
                  <p className="mt-1.5 text-neutral-700 leading-relaxed whitespace-pre-line">
                    {item.reply.content}
                  </p>
                </div>
              ) : (
                <div className="mt-4 flex items-center justify-end border-t border-neutral-100 pt-3">
                  <button
                    type="button"
                    onClick={() => openReplyModal(item)}
                    className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 px-3.5 py-1.5 text-xs font-semibold text-white shadow-2xs hover:bg-emerald-700 transition"
                  >
                    <Send size={12} />
                    Reply to customer
                  </button>
                </div>
              )}
            </article>
          ))}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="pt-2">
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                totalItems={totalItems}
                pageSize={pageSize}
                itemLabel="reviews"
                onPageChange={onPageChange}
                showTotalItems={true}
              />
            </div>
          )}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-neutral-200 bg-white p-12 text-center shadow-xs">
          <div className="flex size-14 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
            <MessageSquare size={28} />
          </div>
          <h3 className="mt-4 text-base font-bold text-neutral-900">
            No reviews match your filters
          </h3>
          <p className="mt-1 text-xs text-neutral-500 max-w-sm">
            Try switching to another filter or check back once more customers review your products.
          </p>
        </div>
      )}

      {/* Lightbox / Zoom Image Modal */}
      {selectedImage && (
        <div
          role="dialog"
          aria-modal="true"
          onClick={() => setSelectedImage(null)}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-4 backdrop-blur-xs"
        >
          <div className="relative max-h-[90vh] max-w-[90vw] overflow-hidden rounded-2xl bg-neutral-900">
            <img
              src={selectedImage}
              alt="Zoomed review"
              className="max-h-[85vh] w-auto object-contain"
            />
            <button
              type="button"
              onClick={() => setSelectedImage(null)}
              className="absolute right-3 top-3 rounded-full bg-black/60 px-3 py-1 text-xs font-bold text-white transition hover:bg-black"
            >
              Close ✕
            </button>
          </div>
        </div>
      )}

      {/* Create Reply Modal */}
      <SharedModal
        isOpen={Boolean(replyTarget)}
        onClose={() => !isSubmittingReply && setReplyTarget(null)}
        title={`Reply to ${replyTarget?.customerName || "Customer"}`}
        maxWidthClass="max-w-lg"
      >
        <form onSubmit={handleReplySubmit} className="space-y-4">
          <div className="rounded-xl border border-neutral-100 bg-neutral-50 p-3 text-xs">
            <p className="font-semibold text-neutral-800">
              {replyTarget?.productName} ({replyTarget?.rating}/5 ⭐)
            </p>
            {replyTarget?.comment && (
              <p className="mt-1 text-neutral-600 italic">
                &ldquo;{replyTarget.comment}&rdquo;
              </p>
            )}
          </div>

          <div>
            <div className="mb-1 flex items-center justify-between">
              <label className="block text-xs font-medium text-neutral-700">
                Your Response
              </label>
              <span className="text-[11px] text-neutral-400">
                {replyContent.length}/2000
              </span>
            </div>
            <textarea
              rows={4}
              maxLength={2000}
              required
              disabled={isSubmittingReply}
              value={replyContent}
              onChange={(e) => setReplyContent(e.target.value)}
              placeholder="Thank the customer for their feedback and share any updates or care instructions..."
              className="w-full resize-none rounded-xl border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-900 outline-none focus:border-emerald-600 focus:ring-1 focus:ring-emerald-600"
            />
          </div>

          <div className="flex items-center justify-end gap-2 border-t border-neutral-100 pt-3">
            <button
              type="button"
              disabled={isSubmittingReply}
              onClick={() => setReplyTarget(null)}
              className="rounded-lg border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmittingReply || !replyContent.trim()}
              className="rounded-lg bg-emerald-600 px-5 py-2 text-xs font-semibold text-white shadow-xs hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmittingReply ? "Submitting..." : "Send response"}
            </button>
          </div>
        </form>
      </SharedModal>

      {/* Edit Reply Modal */}
      <SharedModal
        isOpen={Boolean(editReplyTarget)}
        onClose={() => !isSubmittingEdit && setEditReplyTarget(null)}
        title="Edit Response"
        maxWidthClass="max-w-lg"
      >
        <form onSubmit={handleEditReplySubmit} className="space-y-4">
          <div>
            <div className="mb-1 flex items-center justify-between">
              <label className="block text-xs font-medium text-neutral-700">
                Edit Response
              </label>
              <span className="text-[11px] text-neutral-400">
                {editReplyContent.length}/2000
              </span>
            </div>
            <textarea
              rows={4}
              maxLength={2000}
              required
              disabled={isSubmittingEdit}
              value={editReplyContent}
              onChange={(e) => setEditReplyContent(e.target.value)}
              placeholder="Update your response..."
              className="w-full resize-none rounded-xl border border-neutral-200 bg-white px-3 py-2 text-sm text-neutral-900 outline-none focus:border-emerald-600 focus:ring-1 focus:ring-emerald-600"
            />
          </div>

          <div className="flex items-center justify-end gap-2 border-t border-neutral-100 pt-3">
            <button
              type="button"
              disabled={isSubmittingEdit}
              onClick={() => setEditReplyTarget(null)}
              className="rounded-lg border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmittingEdit || !editReplyContent.trim()}
              className="rounded-lg bg-emerald-600 px-5 py-2 text-xs font-semibold text-white shadow-xs hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmittingEdit ? "Saving..." : "Save changes"}
            </button>
          </div>
        </form>
      </SharedModal>

      {/* Delete Reply Confirmation Modal */}
      <SharedModal
        isOpen={Boolean(deleteReplyTarget)}
        onClose={() => !isSubmittingDelete && setDeleteReplyTarget(null)}
        title="Delete Response?"
        maxWidthClass="max-w-md"
      >
        <div className="space-y-4">
          <p className="text-sm leading-relaxed text-neutral-600">
            Are you sure you want to delete your response to this review? The customer will no longer see your response.
          </p>

          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              disabled={isSubmittingDelete}
              onClick={() => setDeleteReplyTarget(null)}
              className="rounded-lg border border-neutral-200 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 hover:bg-neutral-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="button"
              disabled={isSubmittingDelete}
              onClick={handleDeleteReplySubmit}
              className="rounded-lg bg-red-600 px-4 py-2 text-xs font-semibold text-white hover:bg-red-700 disabled:opacity-50"
            >
              {isSubmittingDelete ? "Deleting..." : "Delete response"}
            </button>
          </div>
        </div>
      </SharedModal>
    </div>
  );
}
