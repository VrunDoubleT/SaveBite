import { Sparkles, Utensils } from "lucide-react";
import { DealCard } from "@/features/flash-deals/components/DealCard";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

interface DealGridProps {
  deals: FlashDeal[];
  isSameShop?: boolean;
  isLoading?: boolean;
  emptyMessage?: string;
  onRefresh?: () => void;
}

export function DealGrid({
  deals,
  isSameShop = false,
  isLoading = false,
  emptyMessage = "No flash deals found within this radius.",
  onRefresh,
}: DealGridProps) {
  // 1. Loading Skeleton: Clean 9-card grid matching DealCard layout
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 9 }).map((_, i) => (
          <div
            key={i}
            className="flex flex-col overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-2xs"
          >
            {/* Image Skeleton with pill placeholders */}
            <div className="relative aspect-[4/3] w-full bg-neutral-100 animate-pulse">
              <div className="absolute top-3 left-3 h-5 w-12 rounded-full bg-neutral-200" />
              <div className="absolute top-3 right-3 h-5 w-20 rounded-full bg-neutral-200" />
            </div>

            {/* Content Skeleton */}
            <div className="flex flex-1 flex-col gap-2.5 p-4 animate-pulse">
              {/* Category Pill */}
              <div className="h-4 w-16 rounded bg-neutral-100" />

              {/* Title & Desc (2 lines) */}
              <div className="space-y-1.5 mt-0.5">
                <div className="h-4 w-4/5 rounded bg-neutral-200/90" />
                <div className="h-3.5 w-3/5 rounded bg-neutral-100" />
              </div>

              {/* Price Row */}
              <div className="mt-1 flex items-baseline gap-2">
                <div className="h-5 w-24 rounded bg-neutral-200/90" />
                <div className="h-3.5 w-14 rounded bg-neutral-100" />
              </div>

              {/* Progress Bar */}
              <div className="mt-1 space-y-1">
                <div className="flex justify-between">
                  <div className="h-2.5 w-14 rounded bg-neutral-100" />
                  <div className="h-2.5 w-12 rounded bg-neutral-100" />
                </div>
                <div className="h-1.5 w-full rounded-full bg-neutral-100 overflow-hidden">
                  <div className="h-full w-2/5 rounded-full bg-neutral-200" />
                </div>
              </div>

              {/* Shop Footer */}
              <div className="mt-auto border-t border-neutral-100 pt-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="h-5 w-5 rounded-full bg-neutral-200" />
                  <div className="h-3 w-24 rounded bg-neutral-100" />
                </div>
                <div className="h-3 w-10 rounded bg-neutral-100" />
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  // 2. Empty State
  if (deals.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-neutral-300 bg-neutral-50/50 px-6 py-16 text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
          <Utensils size={32} />
        </div>
        <h3 className="mt-4 text-base font-semibold text-neutral-800">No deals found nearby</h3>
        <p className="mt-1.5 max-w-sm text-sm text-neutral-500">{emptyMessage}</p>

        <div className="mt-5 flex flex-wrap items-center justify-center gap-3">
          {onRefresh && (
            <button
              type="button"
              onClick={onRefresh}
              className="inline-flex items-center gap-2 rounded-lg border border-neutral-300 bg-white px-4 py-2 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50"
            >
              <Sparkles size={14} />
              Refresh deals
            </button>
          )}
        </div>
      </div>
    );
  }

  // 3. Render DealCard list
  return (
    <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-3">
      {deals.map((deal) => (
        <DealCard key={deal.id} deal={deal} isSameShop={isSameShop} />
      ))}
    </div>
  );
}
