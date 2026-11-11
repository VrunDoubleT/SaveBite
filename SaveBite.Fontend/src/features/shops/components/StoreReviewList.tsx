import { useState } from "react";
import { Star, Image as ImageIcon, MessageSquare } from "lucide-react";
import type { StoreReviewsSummary, StoreReview } from "@/features/shops/types/shop.types";
import {
  Avatar,
  Badge,
  Button,
  ButtonLink,
  EmptyState,
  MediaImage,
  Modal,
  Pagination,
  Skeleton,
} from "@/shared/components/ui";

interface StoreReviewListProps {
  summary: StoreReviewsSummary | null;
  isLoading: boolean;
  activeRatingFilter: number | null;
  onRatingFilterChange: (rating: number | null) => void;
  hasImagesOnly: boolean;
  onHasImagesFilterChange: (val: boolean) => void;
  currentPage: number;
  onPageChange: (page: number) => void;
}
const formatDate = (value: string) =>
  Number.isNaN(new Date(value).getTime())
    ? "Date unavailable"
    : new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric" }).format(
        new Date(value),
      );
function Stars({ rating }: { rating: number }) {
  return (
    <span
      className="inline-flex items-center gap-1"
      role="img"
      aria-label={`${rating} out of 5 stars`}
    >
      {[1, 2, 3, 4, 5].map((value) => (
        <Star
          key={value}
          className={`size-4 ${value <= Math.round(rating) ? "fill-accent-500 text-accent-700" : "text-neutral-300"}`}
          aria-hidden="true"
        />
      ))}
    </span>
  );
}

export function StoreReviewList({
  summary,
  isLoading,
  activeRatingFilter,
  onRatingFilterChange,
  hasImagesOnly,
  onHasImagesFilterChange,
  currentPage,
  onPageChange,
}: StoreReviewListProps) {
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const totalReviews = summary?.totalReviews ?? 0;
  const reviews = summary?.reviews?.items ?? [];
  const totalPages = summary?.reviews?.totalPages ?? 1;
  const withPhotosCount = reviews.filter((review) => review.images?.length).length;
  const visibleReviews = hasImagesOnly
    ? reviews.filter((review) => review.images?.length)
    : reviews;
  const clearFilters = () => {
    onRatingFilterChange(null);
    onHasImagesFilterChange(false);
  };

  if (isLoading && !summary)
    return (
      <div role="status" aria-label="Loading customer reviews" className="space-y-4">
        <Skeleton className="h-32" />
        {[0, 1].map((index) => (
          <div key={index} className="space-y-3 rounded-lg border border-border-default p-4">
            <div className="flex gap-3">
              <Skeleton className="size-10 rounded-full" />
              <Skeleton className="h-10 w-40" />
            </div>
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
          </div>
        ))}
      </div>
    );

  return (
    <div className="space-y-5">
      <div className="grid gap-5 rounded-lg border border-accent-100 bg-accent-50 p-4 sm:grid-cols-[auto_1fr] sm:gap-8 sm:p-6">
        <div className="space-y-2">
          <p className="text-caption font-medium text-accent-700">Store rating</p>
          <p className="text-3xl font-bold tabular-nums text-text-primary">
            {totalReviews ? summary?.averageRating.toFixed(1) : "—"}
            <span className="ml-1 text-body font-normal text-text-secondary">/ 5</span>
          </p>
          {totalReviews > 0 && <Stars rating={summary?.averageRating ?? 0} />}
          <p className="text-caption text-text-secondary">
            {totalReviews} customer review{totalReviews === 1 ? "" : "s"}
          </p>
        </div>
        <div className="space-y-2" aria-label="Rating breakdown">
          {[5, 4, 3, 2, 1].map((star) => {
            const count = summary?.ratingBreakdown[star] ?? 0;
            return (
              <div key={star} className="flex items-center gap-3 text-caption text-text-secondary">
                <span className="flex w-8 shrink-0 items-center gap-1">
                  {star}
                  <Star className="size-3 text-accent-700" aria-hidden="true" />
                </span>
                <div className="h-2 flex-1 overflow-hidden rounded-full bg-bg-surface">
                  <div
                    className="h-full rounded-full bg-accent-500"
                    style={{
                      width: `${totalReviews ? Math.min(100, (count / totalReviews) * 100) : 0}%`,
                    }}
                  />
                </div>
                <span className="w-6 text-right tabular-nums">{count}</span>
              </div>
            );
          })}
        </div>
      </div>
      <div className="space-y-2">
        <div role="group" aria-label="Filter customer reviews" className="flex flex-wrap gap-2">
          <Button
            variant="secondary"
            aria-pressed={activeRatingFilter === null && !hasImagesOnly}
            className="min-h-11 aria-pressed:border-primary-700 aria-pressed:bg-primary-50 aria-pressed:text-primary-700"
            onClick={clearFilters}
          >
            All ({totalReviews})
          </Button>
          {[5, 4, 3, 2, 1].map((star) => (
            <Button
              key={star}
              variant="secondary"
              aria-pressed={activeRatingFilter === star && !hasImagesOnly}
              className="min-h-11 aria-pressed:border-primary-700 aria-pressed:bg-primary-50 aria-pressed:text-primary-700"
              onClick={() => {
                onRatingFilterChange(star);
                onHasImagesFilterChange(false);
              }}
            >
              {star} {star === 1 ? "star" : "stars"} ({summary?.ratingBreakdown[star] ?? 0})
            </Button>
          ))}
          <Button
            variant="secondary"
            aria-pressed={hasImagesOnly}
            className="min-h-11 aria-pressed:border-secondary-700 aria-pressed:bg-secondary-50 aria-pressed:text-secondary-700"
            onClick={() => {
              onHasImagesFilterChange(!hasImagesOnly);
              onRatingFilterChange(null);
            }}
          >
            <ImageIcon className="size-4" aria-hidden="true" />
            Photos on this page ({withPhotosCount})
          </Button>
        </div>
        <p className="text-caption text-text-secondary">
          The photo filter applies to the current page of reviews.
        </p>
      </div>
      {isLoading ? (
        <div role="status" aria-label="Loading customer reviews" className="space-y-4">
          <Skeleton className="h-44" />
          <Skeleton className="h-44" />
        </div>
      ) : !visibleReviews.length ? (
        <EmptyState
          title={totalReviews ? "No reviews match this selection" : "No reviews yet"}
          description={
            totalReviews
              ? "Try another star rating, clear the photo filter, or browse another page."
              : "Customer experiences will appear here once reviews are shared."
          }
          icon={MessageSquare}
          action={
            activeRatingFilter !== null || hasImagesOnly ? (
              <Button variant="secondary" onClick={clearFilters}>
                Clear review filters
              </Button>
            ) : (
              <ButtonLink to="/">Browse deals</ButtonLink>
            )
          }
        />
      ) : (
        <div className="space-y-4">
          {visibleReviews.map((review) => (
            <ReviewItemCard key={review.id} review={review} onSelectImage={setSelectedImage} />
          ))}
        </div>
      )}
      {totalPages > 1 && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={onPageChange}
          totalItems={summary?.reviews.totalItems}
          pageSize={summary?.reviews.pageSize}
          itemLabel="reviews"
        />
      )}
      <Modal
        isOpen={selectedImage !== null}
        onClose={() => setSelectedImage(null)}
        title="Review photo"
        maxWidthClass="max-w-3xl"
      >
        {selectedImage && (
          <MediaImage
            src={selectedImage}
            alt="Customer review photo"
            className="h-96 max-h-[65dvh] w-full rounded-md [&>img]:object-contain"
            loading="eager"
          />
        )}
      </Modal>
    </div>
  );
}

function ReviewItemCard({
  review,
  onSelectImage,
}: {
  review: StoreReview;
  onSelectImage: (url: string) => void;
}) {
  return (
    <article className="space-y-4 rounded-lg border border-border-default bg-bg-surface p-4 sm:p-5">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar
            fullName={review.userFullName || "SaveBite customer"}
            avatarUrl={review.userAvatarUrl}
          />
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="font-semibold text-text-primary [overflow-wrap:anywhere]">
                {review.userFullName || "SaveBite customer"}
              </h3>
              {review.userTier && <Badge tone="primary">{review.userTier}</Badge>}
            </div>
            <div className="mt-1 flex items-center gap-2">
              <Stars rating={review.rating} />
              <span className="text-caption text-text-secondary">{review.rating}/5</span>
            </div>
          </div>
        </div>
        <time dateTime={review.createdAt} className="text-caption text-text-muted">
          {formatDate(review.createdAt)}
        </time>
      </header>
      {review.productName && (
        <div className="flex min-w-0 items-center gap-3 rounded-md bg-secondary-50 p-3">
          {review.productImageUrl && (
            <MediaImage
              src={review.productImageUrl}
              alt=""
              showFallbackLabel={false}
              className="size-10 shrink-0 rounded-md"
            />
          )}
          <div className="min-w-0">
            <p
              className="truncate text-body font-medium text-secondary-700"
              title={review.productName}
            >
              {review.productName}
            </p>
            {review.variantName && (
              <p className="truncate text-caption text-text-secondary" title={review.variantName}>
                {review.variantName}
              </p>
            )}
          </div>
        </div>
      )}
      <p className="whitespace-pre-line text-body leading-relaxed text-text-secondary [overflow-wrap:anywhere]">
        {review.comment || "No written comment."}
      </p>
      {Boolean(review.images?.length) && (
        <div className="flex flex-wrap gap-2">
          {review.images?.map((src, index) => (
            <Button
              key={`${src}-${index}`}
              variant="secondary"
              aria-label={`View review photo ${index + 1} by ${review.userFullName || "customer"}`}
              onClick={() => onSelectImage(src)}
              className="size-16 overflow-hidden !p-0 hover:border-primary-700"
            >
              <MediaImage src={src} alt="" showFallbackLabel={false} className="h-full w-full" />
            </Button>
          ))}
        </div>
      )}
      {review.reply && (
        <div className="space-y-2 rounded-md border border-primary-100 bg-primary-50 p-4">
          <div className="flex flex-wrap items-center gap-2 text-caption text-primary-700">
            <MessageSquare className="size-4" aria-hidden="true" />
            <span className="font-semibold">Store response</span>
            <time dateTime={review.reply.createdAt} className="text-text-secondary">
              {formatDate(review.reply.createdAt)}
            </time>
          </div>
          <p className="text-body leading-relaxed text-text-secondary [overflow-wrap:anywhere]">
            {review.reply.content}
          </p>
        </div>
      )}
    </article>
  );
}