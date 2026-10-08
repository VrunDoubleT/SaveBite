import { useState } from "react";
import { Star, Image as ImageIcon, MessageSquare } from "lucide-react";
import type { StoreReviewsSummary, StoreReview } from "@/features/shops/types/shop.types";
import { Pagination } from "@/shared/components/pagination";

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

// Format relative date in English (e.g. "1mo ago", "2d ago", "Just now")
const formatRelativeTime = (isoString?: string) => {
  if (!isoString) return "Recently";
  const date = new Date(isoString);
  const diffSec = Math.floor((Date.now() - date.getTime()) / 1000);
  if (diffSec < 60) return "Just now";
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHour = Math.floor(diffMin / 60);
  if (diffHour < 24) return `${diffHour}h ago`;
  const diffDay = Math.floor(diffHour / 24);
  if (diffDay < 30) return `${diffDay}d ago`;
  const diffMonth = Math.floor(diffDay / 30);
  if (diffMonth < 12) return `${diffMonth}mo ago`;
  const diffYear = Math.floor(diffMonth / 12);
  return `${diffYear}y ago`;
};

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
  const breakdown = summary?.ratingBreakdown ?? {};
  const reviews = summary?.reviews?.items ?? [];
  const totalPages = summary?.reviews?.totalPages ?? 1;
  const totalItems = summary?.reviews?.totalItems ?? 0;
  const pageSize = summary?.reviews?.pageSize ?? 10;

  // Filter reviews client-side for image filter if needed
  const filteredReviews = reviews.filter((r) => {
    if (hasImagesOnly) {
      return (r.images && r.images.length > 0) || Boolean(r.productImageUrl);
    }
    return true;
  });

  return (
    <div className="space-y-6">
      {/* 1. RATING FILTER PILLS (Matching Mockup Image 2) */}
      <div className="flex flex-wrap items-center gap-2 border-b border-neutral-200/80 pb-5">
        {/* All (N) */}
        <button
          type="button"
          onClick={() => {
            onRatingFilterChange(null);
            if (hasImagesOnly) onHasImagesFilterChange(false);
          }}
          className={`rounded-xl px-3.5 py-1.5 text-xs font-bold transition-all shadow-2xs ${
            activeRatingFilter === null && !hasImagesOnly
              ? "bg-emerald-600 text-white shadow-emerald-600/20"
              : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
          }`}
        >
          All ({totalReviews})
        </button>

        {/* 5 Stars down to 1 Star */}
        {[5, 4, 3, 2, 1].map((star) => {
          const count = breakdown[star] ?? 0;
          const isActive = activeRatingFilter === star && !hasImagesOnly;
          return (
            <button
              key={star}
              type="button"
              onClick={() => {
                onRatingFilterChange(star);
                if (hasImagesOnly) onHasImagesFilterChange(false);
              }}
              className={`rounded-xl px-3.5 py-1.5 text-xs font-semibold transition-all shadow-2xs ${
                isActive
                  ? "bg-emerald-600 text-white shadow-emerald-600/20"
                  : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
              }`}
            >
              {star} {star === 1 ? "Star" : "Stars"} ({count})
            </button>
          );
        })}

        {/* With Photos (N) */}
        <button
          type="button"
          onClick={() => {
            onHasImagesFilterChange(!hasImagesOnly);
            onRatingFilterChange(null);
          }}
          className={`flex items-center gap-1.5 rounded-xl px-3.5 py-1.5 text-xs font-semibold transition-all shadow-2xs ${
            hasImagesOnly
              ? "bg-emerald-600 text-white shadow-emerald-600/20"
              : "border border-neutral-200 bg-white text-neutral-700 hover:bg-neutral-50"
          }`}
        >
          <ImageIcon size={13} />
          <span>With Photos ({totalReviews > 0 ? 1 : 0})</span>
        </button>
      </div>

      {/* 2. LOADING STATE */}
      {isLoading && (
        <div className="space-y-4">
          {[1, 2].map((i) => (
            <div
              key={i}
              className="animate-pulse rounded-2xl border border-neutral-200 bg-white p-5"
            >
              <div className="flex items-center gap-3">
                <div className="h-10 w-10 rounded-full bg-neutral-200" />
                <div className="space-y-1.5">
                  <div className="h-4 w-32 rounded bg-neutral-200" />
                  <div className="h-3 w-20 rounded bg-neutral-200" />
                </div>
              </div>
              <div className="mt-4 h-4 w-full rounded bg-neutral-200" />
              <div className="mt-2 h-4 w-3/4 rounded bg-neutral-200" />
            </div>
          ))}
        </div>
      )}

      {/* 3. EMPTY STATE */}
      {!isLoading && filteredReviews.length === 0 && (
        <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-neutral-200 bg-white p-12 text-center shadow-xs">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-amber-50 text-amber-500">
            <Star size={28} className="fill-amber-400 text-amber-400" />
          </div>
          <h4 className="mt-4 text-base font-bold text-neutral-900">
            No reviews found
          </h4>
          <p className="mt-1 text-xs text-neutral-500 max-w-sm">
            There are no reviews matching this filter. Try selecting a different rating!
          </p>
        </div>
      )}

      {/* 4. REVIEWS LIST */}
      {!isLoading && filteredReviews.length > 0 && (
        <div className="space-y-4">
          {filteredReviews.map((rev) => (
            <ReviewItemCard
              key={rev.id}
              review={rev}
              onSelectImage={setSelectedImage}
            />
          ))}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="pt-4">
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                onPageChange={onPageChange}
                totalItems={totalItems}
                pageSize={pageSize}
                itemLabel="reviews"
                showTotalItems={true}
              />
            </div>
          )}
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
    </div>
  );
}

// Subcomponent: Individual Review Card matching Image 2
function ReviewItemCard({
  review,
  onSelectImage,
}: {
  review: StoreReview;
  onSelectImage: (url: string) => void;
}) {
  const defaultAvatar =
    "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80";

  return (
    <article className="rounded-2xl border border-neutral-200/90 bg-white p-5 shadow-2xs transition-shadow hover:shadow-xs">
      {/* Top Header: Avatar + Name + Tier + Time */}
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-3">
          {/* User Avatar */}
          <div className="relative flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-full border border-amber-300 bg-amber-50">
            <img
              src={review.userAvatarUrl || defaultAvatar}
              alt={review.userFullName}
              className="h-full w-full object-cover"
              onError={(e) => {
                e.currentTarget.src = defaultAvatar;
              }}
            />
          </div>

          <div>
            <div className="flex items-center gap-2">
              <span className="text-sm font-bold text-neutral-900">
                {review.userFullName || "SaveBite Customer"}
              </span>

              {/* Customer Tier Badge: Gold */}
              <span className="rounded-sm bg-amber-100 px-1.5 py-0.5 text-[10px] font-bold text-amber-800">
                {review.userTier || "Gold"}
              </span>
            </div>

            {/* Stars Row */}
            <div className="mt-1 flex items-center gap-0.5">
              {[...Array(5)].map((_, i) => (
                <Star
                  key={i}
                  size={13}
                  className={
                    i < review.rating
                      ? "fill-amber-400 text-amber-400"
                      : "fill-neutral-200 text-neutral-200"
                  }
                />
              ))}
            </div>
          </div>
        </div>

        {/* Time ago */}
        <span className="text-xs text-neutral-400">
          {formatRelativeTime(review.createdAt)}
        </span>
      </div>

      {/* Purchased product info tag */}
      {review.productName && (
        <div className="mt-3 flex items-center gap-2.5 rounded-xl border border-neutral-100 bg-neutral-50/70 p-2 text-xs">
          {review.productImageUrl && (
            <img
              src={review.productImageUrl}
              alt={review.productName}
              className="h-9 w-9 rounded-lg object-cover shrink-0 border border-neutral-200"
            />
          )}
          <div className="min-w-0">
            <span className="block font-semibold text-neutral-800 truncate">
              {review.productName}
            </span>
            <span className="text-[11px] text-neutral-500">
              {review.variantName || "Standard Size"}
            </span>
          </div>
        </div>
      )}

      {/* Review Comment Text */}
      {review.comment && (
        <p className="mt-3 text-xs sm:text-sm leading-relaxed text-neutral-700">
          {review.comment}
        </p>
      )}

      {/* Attached Photos */}
      {((review.images && review.images.length > 0) || review.productImageUrl) && (
        <div className="mt-3 flex flex-wrap gap-2">
          {review.images && review.images.length > 0 ? (
            review.images.map((img, idx) => (
              <button
                key={idx}
                type="button"
                onClick={() => onSelectImage(img)}
                className="group relative h-16 w-16 overflow-hidden rounded-xl border border-neutral-200 shadow-2xs transition hover:opacity-90"
              >
                <img
                  src={img}
                  alt={`Review photo ${idx + 1}`}
                  className="h-full w-full object-cover transition duration-200 group-hover:scale-105"
                />
              </button>
            ))
          ) : review.productImageUrl ? (
            <button
              type="button"
              onClick={() => onSelectImage(review.productImageUrl!)}
              className="group relative h-16 w-16 overflow-hidden rounded-xl border border-neutral-200 shadow-2xs transition hover:opacity-90"
            >
              <img
                src={review.productImageUrl}
                alt="Product snapshot"
                className="h-full w-full object-cover transition duration-200 group-hover:scale-105"
              />
            </button>
          ) : null}
        </div>
      )}

      {/* Store Reply Box (Matching Image 2 light-green container) */}
      {review.reply && (
        <div className="mt-3.5 rounded-xl border border-emerald-100 bg-emerald-50/70 p-3 text-xs animate-fade-in">
          <div className="flex items-center gap-1.5 font-bold text-emerald-800">
            <MessageSquare size={13} className="text-emerald-600 shrink-0" />
            <span>Store Response</span>
            <span className="text-neutral-400 font-normal">•</span>
            <span className="text-neutral-500 font-normal text-[11px]">
              {formatRelativeTime(review.reply.createdAt)}
            </span>
          </div>
          <p className="mt-1 text-neutral-700 leading-relaxed">
            {review.reply.content}
          </p>
        </div>
      )}
    </article>
  );
}
