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
  emptyMessage = "Hiện tại chưa có flash deal nào trong bán kính này.",
  onRefresh,
}: DealGridProps) {
  // 1. Trạng thái đang tải (Loading Skeleton)
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
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

  // 2. Trạng thái không có Deal (Empty State - đúng yêu cầu của bạn trước đó)
  if (deals.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-neutral-300 bg-neutral-50/50 px-6 py-16 text-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
          <Utensils size={32} />
        </div>
        <h3 className="mt-4 text-base font-semibold text-neutral-800">Chưa có ưu đãi gần đây</h3>
        <p className="mt-1.5 max-w-sm text-sm text-neutral-500">{emptyMessage}</p>
        {onRefresh && (
          <button
            type="button"
            onClick={onRefresh}
            className="mt-5 inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-xs font-semibold text-white shadow-xs transition hover:bg-emerald-700"
          >
            <Sparkles size={14} />
            Làm mới ưu đãi
          </button>
        )}
      </div>
    );
  }

  // 3. Hiển thị danh sách các thẻ DealCard
  return (
    <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
      {deals.map((deal) => (
        <DealCard key={deal.id} deal={deal} isSameShop={isSameShop} />
      ))}
    </div>
  );
}
