import { CustomerActionButton } from "@/shared/components/CustomerMutationControls";
import { useRef, useState } from "react";
import {
  CheckCircle2,
  MessageSquareQuote,
  Package,
  Pencil,
  Star,
  Store,
  Trash2,
} from "lucide-react";
import { Link } from "react-router-dom";
import { Badge, MediaImage, Modal } from "@/shared/components/ui";
import { APP_PATHS } from "@/app/router/paths";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import type { FeedbackItem } from "../types/feedback.types";
import { EditReviewModal } from "./EditReviewModal";

interface ReviewCardProps {
  review: FeedbackItem;
  onUpdated: (review: FeedbackItem) => void;
  onRequestDelete: (review: FeedbackItem) => void;
  isHighlighted?: boolean;
}

export function ReviewCard({
  review,
  onUpdated,
  onRequestDelete,
  isHighlighted = false,
}: ReviewCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const editButton = useRef<HTMLButtonElement>(null);
  const [previewImage, setPreviewImage] = useState<string | null>(null);

  const startEdit = () => setIsEditing(true);
  const cancelEdit = () => {
    setIsEditing(false);
    requestAnimationFrame(() => editButton.current?.focus());
  };

  return (
    <article
      id={`feedback-${review.id}`}
      className={`min-w-0 overflow-hidden rounded-lg border bg-bg-surface shadow-sm scroll-mt-20 ${isHighlighted ? "border-primary-500 ring-4 ring-primary-100" : "border-border-default"}`}
    >
      <header className="flex flex-wrap items-center justify-between gap-3 border-b border-border-default bg-primary-50/40 p-4 sm:p-5">
        <div className="flex min-w-0 items-center gap-3">
          <MediaImage
            src={review.shopLogoUrl ?? undefined}
            alt=""
            fallbackIcon={Store}
            showFallbackLabel={false}
            className="size-10 shrink-0 rounded-md"
          />
          <div className="min-w-0">
            <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
              {review.shopName || "Store"}
            </h2>
            {review.orderCode && (
              <p className="mt-1 text-caption text-text-secondary">
                Order{" "}
                {review.orderId ? (
                  <Link
                    to={APP_PATHS.ORDER_DETAIL.replace(
                      ":orderId",
                      encodeURIComponent(review.orderId),
                    )}
                    className="font-semibold text-primary-700 hover:underline"
                  >
                    {review.orderCode}
                  </Link>
                ) : (
                  review.orderCode
                )}
              </p>
            )}
          </div>
        </div>
        <time dateTime={review.createdAt} className="text-caption text-text-secondary">
          {checkoutTime(review.createdAt)} · GMT+7
        </time>
      </header>
      <div className="space-y-4 p-4 sm:p-5">
        <div className="flex items-start gap-3">
          <MediaImage
            src={review.imageUrl ?? undefined}
            alt={review.productName}
            fallbackIcon={Package}
            showFallbackLabel={false}
            className="size-12 shrink-0 rounded-md"
          />
          <div className="min-w-0">
            <h3 className="text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
              {review.productName || "Purchased item"}
            </h3>
            {review.variantName && (
              <p className="mt-1 text-caption text-text-secondary [overflow-wrap:anywhere]">
                {review.variantName}
              </p>
            )}
            <Badge tone="primary" className="mt-2 !px-2">
              <CheckCircle2 size={13} aria-hidden="true" />
              Verified completed order
            </Badge>
          </div>
        </div>
        <>
          <div className="flex flex-wrap items-center gap-2">
            <div className="flex gap-0.5" aria-label={`${review.rating} out of 5 stars`}>
              {Array.from({ length: 5 }, (_, index) => (
                <Star
                  key={index}
                  size={18}
                  aria-hidden="true"
                  className={
                    index < review.rating ? "fill-accent-300 text-accent-700" : "text-neutral-300"
                  }
                />
              ))}
            </div>
            <span className="text-body font-semibold text-text-primary">{review.rating}/5</span>
            {typeof review.editCount === "number" && review.editCount > 0 && (
              <span className="rounded bg-neutral-100 px-2 py-0.5 text-caption font-medium text-text-secondary">
                Edited {review.editCount}/3 times
              </span>
            )}
          </div>
          <p className="whitespace-pre-wrap text-body leading-relaxed text-text-secondary [overflow-wrap:anywhere]">
            {review.comment || "No written comment provided."}
          </p>
          {!!review.images?.length && (
            <div className="flex flex-wrap gap-2">
              {review.images.map((url, index) => (
                <button
                  key={`${url}:${index}`}
                  type="button"
                  aria-label={`View review photo ${index + 1}`}
                  onClick={() => setPreviewImage(url)}
                  className="overflow-hidden rounded-md border border-border-default"
                >
                  <MediaImage
                    src={url}
                    alt={`Review photo ${index + 1}`}
                    showFallbackLabel={false}
                    className="size-16"
                  />
                </button>
              ))}
            </div>
          )}
          {review.reply && (
            <section className="rounded-md border border-primary-100 bg-primary-50/50 p-3">
              <p className="flex flex-wrap items-center gap-2 text-caption font-semibold text-primary-800">
                <MessageSquareQuote size={16} aria-hidden="true" />
                Response from shop{" "}
                {review.reply.repliedByName && (
                  <span className="font-normal">· {review.reply.repliedByName}</span>
                )}
              </p>
              <p className="mt-2 whitespace-pre-wrap text-body text-text-primary [overflow-wrap:anywhere]">
                {review.reply.content}
              </p>
              <p className="mt-2 text-caption text-text-secondary">
                {checkoutTime(review.reply.createdAt)} · GMT+7
              </p>
            </section>
          )}
          <div className="flex flex-wrap justify-end gap-2 border-t border-border-default pt-4">
            <CustomerActionButton
              variant="primary"
              ref={editButton}
              onClick={startEdit}
              disabled={review.canEdit === false || (review.editCount ?? 0) >= 3}
              title={
                review.canEdit === false || (review.editCount ?? 0) >= 3
                  ? "This review cannot be edited (limit of 3 edits or 30 days reached)"
                  : undefined
              }
              className="min-h-11 disabled:opacity-50"
            >
              <Pencil size={16} aria-hidden="true" />
              Edit review
            </CustomerActionButton>
            <CustomerActionButton
              variant="ghost"
              onClick={() => onRequestDelete(review)}
              className="min-h-11 !text-danger-strong hover:!bg-danger-soft"
            >
              <Trash2 size={16} aria-hidden="true" />
              Delete
            </CustomerActionButton>
          </div>
        </>
      </div>
      <EditReviewModal
        isOpen={isEditing}
        feedbackId={review.id}
        orderId={review.orderId}
        initialProductName={review.productName}
        initialVariantName={review.variantName ?? undefined}
        initialImageUrl={review.imageUrl}
        onClose={cancelEdit}
        onUpdated={onUpdated}
      />
      <Modal
        isOpen={!!previewImage}
        onClose={() => setPreviewImage(null)}
        title="Review photo"
        maxWidthClass="max-w-4xl"
      >
        {previewImage && (
          <img
            src={previewImage}
            alt="Review photo enlarged"
            className="mx-auto max-h-[70vh] max-w-full rounded-md object-contain"
          />
        )}
      </Modal>
    </article>
  );
}