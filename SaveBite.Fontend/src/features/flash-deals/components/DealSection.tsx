import { useEffect, useRef } from "react";
import { Flame, Loader2, RefreshCw, Sparkles, Search, X, MapPin } from "lucide-react";
import { DealGrid } from "@/features/flash-deals/components/DealGrid";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

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

  // Automatic infinite scroll when scrolling to bottom
  useEffect(() => {
    if (!hasOlder || isLoadingOlder || isLoading || !onLoadOlder) return;

    const target = observerRef.current;
    if (!target) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) {
          onLoadOlder();
        }
      },
      {
        root: null,
        rootMargin: "250px",
        threshold: 0.1,
      }
    );

    observer.observe(target);

    return () => {
      observer.disconnect();
    };
  }, [hasOlder, isLoadingOlder, isLoading, onLoadOlder]);

  return (
    <section className="flex-1">
      {/* Header Bar */}
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-amber-100 text-amber-600">
              <Flame size={18} />
            </span>
            <h2 className="text-2xl font-bold tracking-tight text-neutral-900">
              Today's Flash Deals
            </h2>
          </div>
          <div className="mt-1 flex flex-wrap items-center gap-2 text-xs sm:text-sm text-neutral-500">
            <span>
              {isLoading
                ? "Searching for deals near you..."
                : `${deals.length} active deals available near you`}
            </span>
            {locationAddress && (
              <>
                <span className="text-neutral-300">•</span>
                <span className="inline-flex items-center gap-1 rounded-md border border-emerald-200/90 bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-800">
                  <MapPin size={11} className="text-emerald-600 shrink-0" />
                  <span>Synced with map:</span>
                  <strong className="max-w-[200px] truncate text-neutral-800 sm:max-w-[280px]">
                    {locationAddress}
                  </strong>
                  <span className="text-[10px] font-bold text-emerald-700">
                    ({locationMode === "saved_address" ? "Home" : "GPS"})
                  </span>
                </span>
              </>
            )}
          </div>
        </div>

        <button
          type="button"
          onClick={onRefresh}
          disabled={isLoading}
          className="inline-flex items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-50 cursor-pointer"
        >
          <RefreshCw size={13} className={isLoading ? "animate-spin text-emerald-600" : ""} />
          Refresh
        </button>
      </div>

      {/* Flash Deals Search Bar */}
      <div className="mb-5">
        <div className="relative flex items-center">
          <Search className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400" size={17} />
          <input
            type="text"
            value={searchKeyword}
            onChange={(e) => onSearchChange?.(e.target.value)}
            placeholder="Search flash deals by food name, shop, or description..."
            className="w-full rounded-xl border border-neutral-200 bg-white py-2.5 pl-10 pr-10 text-sm text-neutral-800 placeholder-neutral-400 shadow-2xs transition focus:border-emerald-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
          />
          {searchKeyword && (
            <button
              type="button"
              onClick={onClearSearch}
              className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full p-1 text-neutral-400 transition hover:bg-neutral-100 hover:text-neutral-700 cursor-pointer"
              title="Clear search"
            >
              <X size={15} />
            </button>
          )}
        </div>
        {searchKeyword && (
          <div className="mt-2 flex items-center justify-between px-1 text-xs text-neutral-500">
            <span>
              Searching for: <strong className="text-emerald-700">"{searchKeyword}"</strong> ({deals.length} result{deals.length === 1 ? "" : "s"})
            </span>
            <button
              type="button"
              onClick={onClearSearch}
              className="font-medium text-emerald-600 hover:text-emerald-800 hover:underline cursor-pointer"
            >
              Clear search
            </button>
          </div>
        )}
      </div>

      {/* Cursor 2: New Deals notification badge */}
      {newDealsCount > 0 && onApplyNewDeals && (
        <div className="mb-5 flex items-center justify-between rounded-xl border border-emerald-200 bg-emerald-50/80 px-4 py-2.5 shadow-2xs animate-in fade-in slide-in-from-top-2 duration-300">
          <div className="flex items-center gap-2 text-xs font-medium text-emerald-800">
            <Sparkles size={16} className="text-emerald-600 animate-pulse" />
            <span>
              <strong>{newDealsCount}</strong> new flash deals just dropped!
            </span>
          </div>
          <button
            type="button"
            onClick={onApplyNewDeals}
            className="rounded-lg bg-emerald-600 px-3 py-1 text-xs font-semibold text-white shadow-2xs transition hover:bg-emerald-700 cursor-pointer"
          >
            Update now
          </button>
        </div>
      )}

      {/* Deals Grid */}
      <DealGrid
        deals={deals}
        isLoading={isLoading}
        emptyMessage={
          searchKeyword
            ? `No flash deals match "${searchKeyword}". Try adjusting your search query or filters.`
            : emptyMessage
        }
        onRefresh={onRefresh}
        onClearSearch={searchKeyword ? onClearSearch : undefined}
      />

      {/* Skeleton placeholders while loading older deals via infinite scroll */}
      {isLoadingOlder && (
        <div className="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div
              key={i}
              className="flex flex-col overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-2xs"
            >
              <div className="relative aspect-[4/3] w-full bg-neutral-100 animate-pulse">
                <div className="absolute top-3 left-3 h-5 w-12 rounded-full bg-neutral-200" />
                <div className="absolute top-3 right-3 h-5 w-20 rounded-full bg-neutral-200" />
              </div>
              <div className="flex flex-1 flex-col gap-2.5 p-4 animate-pulse">
                <div className="h-4 w-16 rounded bg-neutral-100" />
                <div className="space-y-1.5 mt-0.5">
                  <div className="h-4 w-4/5 rounded bg-neutral-200/90" />
                  <div className="h-3.5 w-3/5 rounded bg-neutral-100" />
                </div>
                <div className="mt-1 flex items-baseline gap-2">
                  <div className="h-5 w-24 rounded bg-neutral-200/90" />
                  <div className="h-3.5 w-14 rounded bg-neutral-100" />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Cursor 1: Infinite Scroll when reaching bottom of page */}
      {!isLoading && deals.length > 0 && (
        <div className="mt-4 flex flex-col items-center justify-center">
          {hasOlder ? (
            <div ref={observerRef} className="py-4 flex flex-col items-center gap-2">
              {isLoadingOlder ? (
                <div className="flex items-center gap-2.5 rounded-full border border-emerald-200 bg-emerald-50/80 px-4 py-2 text-xs font-semibold text-emerald-800 shadow-2xs">
                  <Loader2 size={15} className="animate-spin text-emerald-600" />
                  <span>Loading next flash deals...</span>
                </div>
              ) : (
                <div className="h-6 flex items-center gap-1.5 text-xs text-neutral-400">
                  <span className="inline-block h-1.5 w-1.5 rounded-full bg-emerald-500 animate-ping" />
                  <span>Scroll down to load more</span>
                </div>
              )}
            </div>
          ) : (
            <p className="py-4 text-xs text-neutral-400">
              All available flash deals loaded ({deals.length} deals).
            </p>
          )}
        </div>
      )}
    </section>
  );
}
