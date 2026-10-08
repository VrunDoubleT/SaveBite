import { Flame, RefreshCw } from "lucide-react";
import { DealGrid } from "@/features/flash-deals/components/DealGrid";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";
import { Pagination, usePagination } from "@/shared/components/pagination";

interface DealSectionProps {
  deals: FlashDeal[];
  isLoading: boolean;
  emptyMessage?: string;
  onRefresh: () => void;
}

export function DealSection({
  deals,
  isLoading,
  emptyMessage,
  onRefresh,
}: DealSectionProps) {
  const {
    paginatedItems: paginatedDeals,
    currentPage,
    setCurrentPage,
    totalPages,
    pageSize,
    totalItems,
  } = usePagination({
    items: deals,
    initialPageSize: 9,
  });

  return (
    <section className="flex-1">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
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
          className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-50"
        >
          <RefreshCw size={13} className={isLoading ? "animate-spin" : ""} />
          Refresh
        </button>
      </div>

      <DealGrid
        deals={paginatedDeals}
        isLoading={isLoading}
        emptyMessage={emptyMessage}
        onRefresh={onRefresh}
      />

      {!isLoading && deals.length > 0 && totalPages > 1 && (
        <div className="mt-8">
          <Pagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={setCurrentPage}
            totalItems={totalItems}
            pageSize={pageSize}
            itemLabel="deals"
            showTotalItems={true}
          />
        </div>
      )}
    </section>
  );
}
