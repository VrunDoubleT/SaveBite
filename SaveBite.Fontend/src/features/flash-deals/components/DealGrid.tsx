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
  // 1. Loading Skeleton
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="animate-pulse rounded-xl border border-neutral-200 bg-white p-4">
            <div className="aspect-[4/3] w-full rounded-lg bg-neutral-200" />
            <div className="mt-4 h-5 w-3/4 rounded bg-neutral-200" />
            <div className="mt-2 h-4 w-1/2 rounded bg-neutral-200" />
            <div className="mt-4 h-8 w-full rounded bg-neutral-200" />
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
