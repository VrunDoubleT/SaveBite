import { useEffect, useState } from "react";
import { MessageSquareText, RefreshCw } from "lucide-react";
import { Button, Card, ErrorState } from "@/shared/components/ui";
import { shopApi } from "../api/shopApi";
import type { StoreReviewsSummary } from "../types/shop.types";
import { StoreReviewList } from "./StoreReviewList";

type ReviewResult = { key: string; summary: StoreReviewsSummary | null; error: boolean };

export function DealStoreReviews({
  shopId,
  shopName,
  onSummary,
}: {
  shopId: string;
  shopName: string;
  onSummary?: (summary: StoreReviewsSummary) => void;
}) {
  const [query, setQuery] = useState<{ rating: number | null; page: number }>({
    rating: null,
    page: 1,
  });
  const [hasImagesOnly, setHasImagesOnly] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState<ReviewResult | null>(null);
  const { rating, page } = query;
  const key = `${shopId}:${rating}:${page}:${attempt}`;
  const isLoading = result?.key !== key;
  useEffect(() => {
    let active = true;
    shopApi
      .getStoreReviews(shopId, { rating: rating ?? undefined, page, pageSize: 4 })
      .then((summary) => {
        if (active) {
          setResult({ key, summary, error: false });
          onSummary?.(summary);
        }
      })
      .catch(() => {
        if (active) setResult({ key, summary: null, error: true });
      });
    return () => {
      active = false;
    };
  }, [shopId, rating, page, key, onSummary]);

  return (
    <Card
      id="reviews"
      aria-labelledby="customer-reviews-heading"
      className="scroll-mt-24 space-y-6"
    >
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0">
          <h2
            id="customer-reviews-heading"
            className="flex items-center gap-2 text-section font-semibold text-text-primary"
          >
            <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary-50 text-primary-700">
              <MessageSquareText className="size-5" aria-hidden="true" />
            </span>
            Customer reviews
          </h2>
          <p className="mt-2 text-body text-text-secondary [overflow-wrap:anywhere]">
            What customers say about {shopName}. These reviews cover all products at this store.
          </p>
        </div>
        <Button
          variant="secondary"
          className="min-h-11"
          aria-label="Refresh reviews"
          loading={isLoading}
          onClick={() => setAttempt((value) => value + 1)}
        >
          {!isLoading && <RefreshCw className="size-4" aria-hidden="true" />}Refresh
        </Button>
      </header>
      {!isLoading && result?.error ? (
        <ErrorState
          title="Couldn't load customer reviews"
          description="You can still choose your deal above. Try loading reviews again."
          onRetry={() => setAttempt((value) => value + 1)}
        />
      ) : (
        <StoreReviewList
          summary={result?.summary ?? null}
          isLoading={isLoading}
          activeRatingFilter={rating}
          onRatingFilterChange={(next) =>
            setQuery((current) => (current.rating === next ? current : { rating: next, page: 1 }))
          }
          hasImagesOnly={hasImagesOnly}
          onHasImagesFilterChange={setHasImagesOnly}
          currentPage={page}
          onPageChange={(next) => setQuery((current) => ({ ...current, page: next }))}
        />
      )}
    </Card>
  );
}
