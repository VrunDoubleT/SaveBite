import { useEffect, useRef } from "react";
import { Flame, Loader2, RefreshCw, Sparkles } from "lucide-react";
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
          <p className="mt-1 text-sm text-neutral-500">
            {isLoading
              ? "Searching for deals near you..."
              : `${deals.length} active deals available near you`}
          </p>
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

      {/* Cursor 2: Thông báo Deal mới xuất hiện */}
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

      {/* Grid danh sách deal */}
      <DealGrid
        deals={deals}
        isLoading={isLoading}
        emptyMessage={emptyMessage}
        onRefresh={onRefresh}
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

      {/* Cursor 1: Infinite Scroll khi lăn chuột xuống đáy */}
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
