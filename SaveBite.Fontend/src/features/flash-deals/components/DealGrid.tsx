import { Utensils } from "lucide-react";
import { DealCard } from "@/features/flash-deals/components/DealCard";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

import { Button, EmptyState, Skeleton } from "@/shared/components/ui";

interface DealGridProps {
  deals: FlashDeal[];
  isSameShop?: boolean;
  isLoading?: boolean;
  emptyMessage?: string;
  onRefresh?: () => void;
  onClearSearch?: () => void;
}

export function DealGrid({
  deals,
  isSameShop = false,
  isLoading = false,
  emptyMessage = "No flash deals found within this radius.",
  onRefresh,
  onClearSearch,
}: DealGridProps) {
  if (isLoading) {
    return (
      <div
        role="status"
        aria-label="Loading flash deals"
        className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3"
      >
        {Array.from({ length: 6 }).map((_, i) => (
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
        title={onClearSearch ? "No matching deals" : "No deals nearby yet"}
        description={emptyMessage}
        icon={Utensils}
        action={
          <div className="flex flex-wrap justify-center gap-3">
            {onClearSearch && <Button onClick={onClearSearch}>Clear search</Button>}
            {onRefresh && (
              <Button variant={onClearSearch ? "secondary" : "primary"} onClick={onRefresh}>
                Refresh deals
              </Button>
            )}
          </div>
        }
      />
    );
  }
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {deals.map((deal) => (
        <DealCard key={deal.id} deal={deal} isSameShop={isSameShop} />
      ))}
    </div>
  );
}
