import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useEffect, useRef } from "react";
import { Flame, Search, X } from "lucide-react";
import { DealGrid } from "@/features/flash-deals/components/DealGrid";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

import { Button, IconButton, Input } from "@/shared/components/ui";

interface DealSectionProps {
  deals: FlashDeal[];
  isLoading: boolean;
  showSkeleton?: boolean;
  emptyMessage?: string;
  onRefresh: () => void;
  hasOlder?: boolean;
  isLoadingOlder?: boolean;
  onLoadOlder?: () => void;
  searchKeyword?: string;
  onSearchChange?: (kw: string) => void;
  onClearSearch?: () => void;
  selectedCategories?: string[];
  onClearCategory?: () => void;
  locationAddress?: string | null;
  locationMode?: "gps" | "saved_address" | "default" | null;
}

export function DealSection({
  deals,
  isLoading,
  showSkeleton = true,
  emptyMessage,
  onRefresh,
  hasOlder = false,
  isLoadingOlder = false,
  onLoadOlder,
  searchKeyword = "",
  onSearchChange,
  onClearSearch,
  selectedCategories = [],
  onClearCategory,
  locationAddress,
  locationMode,
}: DealSectionProps) {
  const observerRef = useRef<HTMLDivElement | null>(null);
  const isTriggeringRef = useRef(false);

  const isCategoryMismatch = Boolean(
    !isLoading &&
    deals.length === 0 &&
    searchKeyword.trim().length > 0 &&
    selectedCategories.length > 0,
  );

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
            onChange={(event) => onSearchChange?.(event.target.value.normalize("NFC"))}
            placeholder="Search food or a favorite bite"
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
        {isCategoryMismatch && (
          <div className="mt-3 flex flex-wrap items-center justify-between gap-2 rounded-lg border border-amber-300 bg-amber-50 px-3.5 py-2.5 text-caption text-amber-900 shadow-sm animate-in fade-in duration-200">
            <div className="flex items-center gap-2">
              <span className="text-base" aria-hidden="true">
                ⚠️
              </span>
              <span>
                No items match keyword <strong>“{searchKeyword}”</strong> in category{" "}
                <strong>{selectedCategories.map((c) => `“${c}”`).join(", ")}</strong>.
              </span>
            </div>
            {onClearCategory && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onClearCategory}
                className="h-8 px-2 text-xs font-semibold text-amber-900 hover:bg-amber-100"
              >
                Clear category filter
              </Button>
            )}
          </div>
        )}
      </div>
      <DealGrid
        deals={deals}
        isLoading={isLoading && deals.length === 0}
        showSkeleton={showSkeleton}
        isCategoryMismatch={isCategoryMismatch}
        emptyMessage={
          isCategoryMismatch
            ? `Keyword “${searchKeyword}” has no items in category ${selectedCategories.map((c) => `“${c}”`).join(", ")} within the current radius. You can clear the category filter or try another keyword.`
            : searchKeyword
              ? `Nothing matches “${searchKeyword}”. Try another food or store, or clear your filters.`
              : emptyMessage
        }
        onRefresh={onRefresh}
        onClearSearch={searchKeyword ? onClearSearch : undefined}
        onClearCategory={selectedCategories.length > 0 ? onClearCategory : undefined}
      />
      {isLoadingOlder &&
        (showSkeleton ? (
          <LoadingSkeleton variant="cards" label="Loading more deals" />
        ) : (
          <p role="status" className="text-sm text-text-secondary">
            Loading more deals…
          </p>
        ))}
      {!isLoading && deals.length > 0 && (
        <div className="flex justify-center border-t border-border-default pt-5">
          {hasOlder ? (
            <div ref={observerRef}>
              <Button
                variant="secondary"
                className="min-h-11"
                disabled={isLoadingOlder}
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
