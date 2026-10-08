import { Flame, RefreshCw } from "lucide-react";
import { DealGrid } from "@/features/flash-deals/components/DealGrid";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

interface DealSectionProps {
  deals: FlashDeal[];
  isLoading: boolean;
  emptyMessage?: string;
  onRefresh: () => void;
}

export function DealSection({ deals, isLoading, emptyMessage, onRefresh }: DealSectionProps) {
  return (
    <section className="flex-1">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-amber-100 text-amber-600">
              <Flame size={18} />
            </span>
            <h2 className="text-2xl font-bold tracking-tight text-neutral-900">
              Flash Deal Hôm Nay
            </h2>
          </div>
          <p className="mt-1 text-sm text-neutral-500">
            {isLoading
              ? "Đang tìm kiếm deal gần bạn..."
              : `${deals.length} ưu đãi giá tốt đang diễn ra quanh bạn`}
          </p>
        </div>

        <button
          type="button"
          onClick={onRefresh}
          disabled={isLoading}
          className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-50"
        >
          <RefreshCw size={13} className={isLoading ? "animate-spin" : ""} />
          Làm mới
        </button>
      </div>

      <DealGrid
        deals={deals}
        isLoading={isLoading}
        emptyMessage={emptyMessage}
        onRefresh={onRefresh}
      />
    </section>
  );
}
