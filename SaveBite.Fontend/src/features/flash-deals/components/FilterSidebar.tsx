import { useEffect, useMemo, useState, type Dispatch, type SetStateAction } from "react";
import { Filter, RotateCcw, Loader2, X } from "lucide-react";
import type { DealFilterState } from "@/features/flash-deals/types/flashDeal.types";
import { categoryApi } from "@/features/categories/api/categoryApi";

interface FilterSidebarProps {
  filters: DealFilterState;
  setFilters: Dispatch<SetStateAction<DealFilterState>>;
  radiusInKm?: number;
  setRadiusInKm?: (r: number) => void;
  selectedShop: string | null;
  onClearShop: () => void;
  categoryCounts?: Record<string, number>;
  categories?: string[];
  isLoadingCategories?: boolean;
  isSearching?: boolean;
}

const DISTANCE_OPTIONS = ["Under 2km", "Under 5km", "Over 5km - 10km"];
const PRICE_OPTIONS = ["Under 30k", "30k - 50k", "Over 50k"];

export function FilterSidebar({
  filters,
  setFilters,
  setRadiusInKm,
  selectedShop,
  onClearShop,
  categoryCounts = {},
  categories: propCategories,
  isLoadingCategories: propIsLoadingCategories,
  isSearching = false,
}: FilterSidebarProps) {
  const [internalCategories, setInternalCategories] = useState<string[]>([]);
  const [internalLoading, setInternalLoading] = useState(false);

  // If parent didn't supply categories, fetch directly from backend DB API
  useEffect(() => {
    if (propCategories !== undefined) return;
    let isMounted = true;
    setInternalLoading(true);
    categoryApi
      .getCategories()
      .then((data) => {
        if (isMounted) {
          setInternalCategories(data.map((c) => c.name));
        }
      })
      .catch(() => {
        // Ignored
      })
      .finally(() => {
        if (isMounted) {
          setInternalLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [propCategories]);

  const activeCategories = propCategories ?? internalCategories;
  const isCategoriesLoading = propIsLoadingCategories ?? internalLoading;

  // Multi-select for categories
  const toggle = (key: keyof DealFilterState, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: prev[key].includes(value)
        ? prev[key].filter((v) => v !== value)
        : [...prev[key], value],
    }));
  };

  // Single-select for distance and price (clicking selected item unselects it)
  const toggleSingle = (key: "distance" | "price", value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: prev[key].includes(value) ? [] : [value],
    }));
  };

  const clearAll = () => {
    setFilters({ distance: [], price: [], category: [] });
    setRadiusInKm?.(10);
    onClearShop();
  };

  const categoryList = useMemo(() => {
    const set = new Set([...activeCategories, ...Object.keys(categoryCounts)]);
    return Array.from(set).filter(Boolean);
  }, [activeCategories, categoryCounts]);

  return (
    <aside className="relative lg:sticky lg:top-24 rounded-xl border border-neutral-200 bg-white p-5 shadow-xs">
      <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
        <h2 className="flex items-center gap-2 text-base font-bold text-neutral-900">
          <Filter size={18} className="text-emerald-600" />
          Filters
        </h2>
        <button
          type="button"
          onClick={clearAll}
          className="flex items-center gap-1 text-xs font-semibold text-emerald-600 hover:text-emerald-700 cursor-pointer"
        >
          <RotateCcw size={12} />
          Reset
        </button>
      </div>

      {/* Category Filter */}
      {!isSearching ? (
        <div className="border-b border-neutral-100 py-4">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">
              Categories
            </span>
            {filters.category.length > 0 && (
              <span className="rounded-full bg-emerald-100 px-1.5 py-0.2 text-[10px] font-bold text-emerald-800">
                {filters.category.length}
              </span>
            )}
          </div>

          {/* Scrollable list with max-height to avoid overflow */}
          <div className="mt-2.5 max-h-52 overflow-y-auto pr-1 space-y-1 overscroll-contain">
            {isCategoriesLoading && categoryList.length === 0 ? (
              <div className="flex items-center gap-2 py-3 text-xs text-neutral-400">
                <Loader2 size={14} className="animate-spin text-emerald-600" />
                <span>Loading categories...</span>
              </div>
            ) : categoryList.length === 0 ? (
              <p className="py-2 text-center text-xs text-neutral-400 italic">
                No categories available
              </p>
            ) : (
              categoryList.map((cat) => {
                const count = categoryCounts[cat] ?? 0;
                const isChecked = filters.category.includes(cat);
                return (
                  <label
                    key={cat}
                    className="flex cursor-pointer items-center justify-between rounded-md px-1.5 py-1 text-xs text-neutral-700 hover:bg-emerald-50/60 hover:text-emerald-800 transition"
                  >
                    <div className="flex items-center gap-2 min-w-0">
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => toggle("category", cat)}
                        className="h-3.5 w-3.5 shrink-0 rounded-xs border-neutral-300 accent-emerald-600"
                      />
                      <span className="truncate">{cat}</span>
                    </div>
                    {count > 0 && (
                      <span className="ml-2 shrink-0 rounded-full bg-neutral-100 px-1.5 py-0.2 text-[10px] font-semibold text-neutral-500">
                        {count}
                      </span>
                    )}
                  </label>
                );
              })
            )}
          </div>
        </div>
      ) : (
        <div className="border-b border-neutral-100 py-3.5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-neutral-400">
              Categories
            </span>
            <span className="text-[10px] font-semibold text-neutral-400 italic">
              Hidden while searching
            </span>
          </div>
          <p className="mt-1 text-[11px] text-neutral-400">
            Clear search to browse categories.
          </p>
        </div>
      )}




      {/* Distance */}
      <div className="border-b border-neutral-100 py-4">
        <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">
          Distance
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
                onChange={() => toggleSingle("distance", opt)}
                className="h-4 w-4 rounded-full border-neutral-300 accent-emerald-600"
              />
              <span>{opt}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Price Range */}
      <div className="py-4">
        <span className="text-xs font-bold uppercase tracking-wider text-neutral-500">Price Range</span>
        <div className="mt-2.5 space-y-1.5">
          {PRICE_OPTIONS.map((opt) => (
            <label
              key={opt}
              className="flex cursor-pointer items-center gap-2.5 text-xs text-neutral-700 hover:text-emerald-700"
            >
              <input
                type="checkbox"
                checked={filters.price.includes(opt)}
                onChange={() => toggleSingle("price", opt)}
                className="h-4 w-4 rounded-full border-neutral-300 accent-emerald-600"
              />
              <span>{opt}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Filtered by store */}
      {selectedShop && (
        <div className="mt-2 flex items-center justify-between rounded-lg bg-emerald-50 px-3 py-2 text-xs font-medium text-emerald-800">
          <span className="truncate">Store: {selectedShop}</span>
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
