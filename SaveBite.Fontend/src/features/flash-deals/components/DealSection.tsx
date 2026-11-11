import { useEffect, useRef } from "react";
import { Flame, RefreshCw, Sparkles, Search, X } from "lucide-react";
import { DealGrid } from "@/features/flash-deals/components/DealGrid";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

import { Badge, Button, IconButton, Input } from "@/shared/components/ui";

interface DealSectionProps {
  deals: FlashDeal[];
  isLoading: boolean;
  emptyMessage?: string;
  onRefresh: () => void;
  hasOlder?: boolean;
  isLoadingOlder?: boolean;
  onLoadOlder?: () => void;
  newDealsCount?: number;
  onApplyNewDeals?: () => void;
  searchKeyword?: string;
  onSearchChange?: (kw: string) => void;
  onClearSearch?: () => void;
  locationAddress?: string | null;
  locationMode?: "gps" | "saved_address" | "default" | null;
}

export function DealSection({
  deals,
  isLoading,
  emptyMessage,
  onRefresh,
  hasOlder = false,
  isLoadingOlder = false,
  onLoadOlder,
  newDealsCount = 0,
  onApplyNewDeals,
  searchKeyword = "",
  onSearchChange,
  onClearSearch,
  locationAddress,
  locationMode,
}: DealSectionProps) {
  const observerRef = useRef<HTMLDivElement | null>(null);
  const isTriggeringRef = useRef(false);

  useEffect(() => {
    isTriggeringRef.current = false;
  }, [deals.length, hasOlder]);

  // Automatic infinite scroll when scrolling to bottom
  useEffect(() => {
    if (!hasOlder || isLoadingOlder || isLoading || !onLoadOlder) return;

    const target = observerRef.current;
    if (!target) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting && !isTriggeringRef.current && !isLoadingOlder) {
          isTriggeringRef.current = true;
          onLoadOlder();
        }
      },
      {
        root: null,
        rootMargin: "80px",
        threshold: 0.1,
      },
    );

    observer.observe(target);

    return () => {
      observer.disconnect();
    };
  }, [hasOlder, isLoadingOlder, isLoading, onLoadOlder]);

  return (
    <section aria-labelledby="flash-deals-heading" className="min-w-0 space-y-5">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <h2
            id="flash-deals-heading"
            className="flex items-center gap-2 text-section font-semibold text-text-primary"
          >
            <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-accent-100 text-accent-700">
              <Flame className="size-5" aria-hidden="true" />
            </span>
            Today's flash deals
          </h2>
          <p role="status" className="text-body text-text-secondary">
            {isLoading ? "Finding deals near you…" : `${deals.length} deals loaded`}
            {locationAddress && (
              <span className="sr-only">
                {" "}
                near {locationAddress} (
                {locationMode === "saved_address"
                  ? "saved address"
                  : locationMode === "default"
                    ? "default location"
                    : "GPS"}
                )
              </span>
            )}
          </p>
        </div>
        <Button
          variant="secondary"
          aria-label="Refresh deals"
          className="min-h-11"
          onClick={onRefresh}
          loading={isLoading}
        >
          {!isLoading && <RefreshCw className="size-4" aria-hidden="true" />}
          <span className="hidden sm:inline">Refresh</span>
        </Button>
      </header>
      <div>
        <label htmlFor="flash-deal-search" className="sr-only">
          Search flash deals
        </label>
        <div className="relative">
          <Search
            className="pointer-events-none absolute left-4 top-4 size-5 text-text-muted"
            aria-hidden="true"
          />
          <Input
            id="flash-deal-search"
            type="search"
            value={searchKeyword}
            onChange={(event) => onSearchChange?.(event.target.value)}
            placeholder="Search food, stores, or a favorite bite"
            className="min-h-12 pl-12 pr-12 [&::-webkit-search-cancel-button]:appearance-none"
          />
          {searchKeyword && (
            <IconButton
              aria-label="Clear search"
              onClick={onClearSearch}
              className="absolute right-1 top-1"
            >
              <X className="size-4" aria-hidden="true" />
            </IconButton>
          )}
        </div>
        {searchKeyword && (
          <p className="mt-2 break-words text-caption text-text-secondary">
            Results for <strong className="text-text-primary">“{searchKeyword}”</strong>
          </p>
        )}
      </div>
      {newDealsCount > 0 && onApplyNewDeals && (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-primary-200 bg-primary-50 p-4">
          <p role="status" className="flex items-center gap-2 text-body text-primary-700">
            <Sparkles className="size-4 shrink-0" aria-hidden="true" />
            <Badge tone="primary">{newDealsCount} new deals</Badge>Ready to discover
          </p>
          <Button onClick={onApplyNewDeals}>Show new deals</Button>
        </div>
      )}
      <DealGrid
        deals={deals}
        isLoading={isLoading}
        emptyMessage={
          searchKeyword
            ? `Nothing matches “${searchKeyword}”. Try another food or store, or clear your filters.`
            : emptyMessage
        }
        onRefresh={onRefresh}
        onClearSearch={searchKeyword ? onClearSearch : undefined}
      />
      {!isLoading && deals.length > 0 && (
        <div className="flex justify-center border-t border-border-default pt-5">
          {hasOlder ? (
            <div ref={observerRef}>
              <Button
                variant="secondary"
                className="min-h-11"
                loading={isLoadingOlder}
                onClick={onLoadOlder}
              >
                {isLoadingOlder ? "Loading more deals…" : "Load more deals"}
              </Button>
            </div>
          ) : (
            <p className="text-caption text-text-secondary">
              You're all caught up. {deals.length} deals loaded.
            </p>
          )}
        </div>
      )}
    </section>
  );
}
