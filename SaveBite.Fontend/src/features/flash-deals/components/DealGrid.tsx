import { AlertTriangle, Utensils } from "lucide-react";
import { DealCard } from "@/features/flash-deals/components/DealCard";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

import { Button, EmptyState, Skeleton } from "@/shared/components/ui";

interface DealGridProps {
  deals: FlashDeal[];
  desktopColumns?: 3 | 4;
  isSameShop?: boolean;
  isLoading?: boolean;
  showSkeleton?: boolean;
  emptyMessage?: string;
  isCategoryMismatch?: boolean;
  onRefresh?: () => void;
  onClearSearch?: () => void;
  onClearCategory?: () => void;
}

export function DealGrid({
  deals,
  desktopColumns = 3,
  isSameShop = false,
  isLoading = false,
  showSkeleton = true,
  emptyMessage = "No flash deals found within this radius.",
  isCategoryMismatch = false,
  onRefresh,
  onClearSearch,
  onClearCategory,
}: DealGridProps) {
  const gridClassName = `grid grid-cols-1 gap-4 sm:grid-cols-2 ${desktopColumns === 4 ? "lg:grid-cols-3 xl:grid-cols-4" : "xl:grid-cols-3"}`;
  if (isLoading && !showSkeleton) {
    return (
      <div role="status" aria-label="Loading flash deals" className="min-h-12">
        <span className="sr-only">Loading flash deals</span>
      </div>
    );
  }
  if (isLoading) {
    return (
      <div role="status" aria-label="Loading flash deals" className={gridClassName}>
        {Array.from({ length: desktopColumns === 4 ? 8 : 6 }).map((_, i) => (
          <div
            key={i}
            className="overflow-hidden rounded-lg border border-border-default bg-bg-surface"
          >
            <Skeleton className="aspect-[4/3] rounded-none" />
            <div className="space-y-3 p-4">
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-5 w-4/5" />
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-6 w-1/2" />
              <Skeleton className="h-11" />
            </div>
          </div>
        ))}
      </div>
    );
  }
  if (!deals.length) {
    return (
      <EmptyState
        title={
          isCategoryMismatch
            ? "No deals in this category matching keyword"
            : onClearSearch
              ? "No matching deals"
              : "No deals nearby yet"
        }
        description={emptyMessage}
        icon={isCategoryMismatch ? AlertTriangle : Utensils}
        action={
          <div className="flex flex-wrap justify-center gap-3">
            {onClearCategory && (
              <Button variant="primary" onClick={onClearCategory}>
                Clear category filter
              </Button>
            )}
            {onClearSearch && (
              <Button variant={onClearCategory ? "secondary" : "primary"} onClick={onClearSearch}>
                Clear search
              </Button>
            )}
            {onRefresh && !onClearCategory && !onClearSearch && (
              <Button variant="primary" onClick={onRefresh}>
                Refresh deals
              </Button>
            )}
          </div>
        }
      />
    );
  }
  return (
    <div className={gridClassName}>
      {deals.map((deal) => (
        <DealCard key={deal.id} deal={deal} isSameShop={isSameShop} />
      ))}
    </div>
  );
}
