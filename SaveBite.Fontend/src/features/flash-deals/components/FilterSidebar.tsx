import type { Dispatch, SetStateAction } from "react";
import { Filter, RotateCcw, X } from "lucide-react";
import type { DealFilterState } from "@/features/flash-deals/types/flashDeal.types";

interface FilterSidebarProps {
  filters: DealFilterState;
  setFilters: Dispatch<SetStateAction<DealFilterState>>;
  radiusInKm: number;
  setRadiusInKm: (r: number) => void;
  selectedShop: string | null;
  onClearShop: () => void;
}

const DISTANCE_OPTIONS = ["Dưới 2km", "Dưới 5km", "Trên 5km"];
const PRICE_OPTIONS = ["Dưới 30k", "30k - 50k", "Trên 50k"];
const RADIUS_OPTIONS = [5, 10, 15, 25];

export function FilterSidebar({
  filters,
  setFilters,
  radiusInKm,
  setRadiusInKm,
  selectedShop,
  onClearShop,
}: FilterSidebarProps) {
  const toggle = (key: keyof DealFilterState, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: prev[key].includes(value)
        ? prev[key].filter((v) => v !== value)
        : [...prev[key], value],
    }));
  };

  const clearAll = () => {
    setFilters({ distance: [], price: [], category: [] });
    setRadiusInKm(15);
    onClearShop();
  };

  return (
    <aside className="sticky top-20 rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
      <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
        <h2 className="flex items-center gap-2 text-base font-bold text-neutral-900">
          <Filter size={18} className="text-emerald-600" />
          Bộ lọc
        </h2>
        <button
          type="button"
          onClick={clearAll}
          className="flex items-center gap-1 text-xs font-semibold text-emerald-600 hover:text-emerald-700"
        >
          <RotateCcw size={12} />
          Đặt lại
        </button>
      </div>

      {/* Bán kính quét GPS */}
      <div className="border-b border-neutral-100 py-4">
        <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">
          Bán kính quét ({radiusInKm} km)
        </span>
        <div className="mt-2.5 flex flex-wrap gap-1.5">
          {RADIUS_OPTIONS.map((r) => (
            <button
              key={r}
              type="button"
              onClick={() => setRadiusInKm(r)}
              className={`rounded-lg px-2.5 py-1 text-xs font-semibold transition ${
                radiusInKm === r
                  ? "bg-emerald-600 text-white"
                  : "bg-neutral-100 text-neutral-600 hover:bg-neutral-200"
              }`}
            >
              {r} km
            </button>
          ))}
        </div>
      </div>

      {/* Khoảng cách */}
      <div className="border-b border-neutral-100 py-4">
        <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">
          Khoảng cách
        </span>
        <div className="mt-2.5 space-y-1.5">
          {DISTANCE_OPTIONS.map((opt) => (
            <label
              key={opt}
              className="flex cursor-pointer items-center gap-2.5 text-xs text-neutral-700 hover:text-emerald-700"
            >
              <input
                type="checkbox"
                checked={filters.distance.includes(opt)}
                onChange={() => toggle("distance", opt)}
                className="h-4 w-4 rounded-sm border-neutral-300 accent-emerald-600"
              />
              <span>{opt}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Mức giá */}
      <div className="py-4">
        <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">Mức giá</span>
        <div className="mt-2.5 space-y-1.5">
          {PRICE_OPTIONS.map((opt) => (
            <label
              key={opt}
              className="flex cursor-pointer items-center gap-2.5 text-xs text-neutral-700 hover:text-emerald-700"
            >
              <input
                type="checkbox"
                checked={filters.price.includes(opt)}
                onChange={() => toggle("price", opt)}
                className="h-4 w-4 rounded-sm border-neutral-300 accent-emerald-600"
              />
              <span>{opt}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Đang lọc theo cửa hàng */}
      {selectedShop && (
        <div className="mt-2 flex items-center justify-between rounded-lg bg-emerald-50 px-3 py-2 text-xs font-medium text-emerald-800">
          <span className="truncate">Cửa hàng: {selectedShop}</span>
          <button
            type="button"
            onClick={onClearShop}
            className="text-emerald-700 hover:text-emerald-900"
          >
            <X size={14} />
          </button>
        </div>
      )}
    </aside>
  );
}
