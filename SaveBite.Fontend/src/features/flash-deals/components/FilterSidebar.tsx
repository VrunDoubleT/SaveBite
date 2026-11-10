import { useEffect, useMemo, useState, type Dispatch, type SetStateAction } from "react";
import { Filter, RotateCcw, X } from "lucide-react";
import type { DealFilterState } from "@/features/flash-deals/types/flashDeal.types";
import { categoryApi } from "@/features/categories/api/categoryApi";

import { Button, Card, Checkbox, IconButton, Skeleton } from "@/shared/components/ui";
import { toast } from "@/shared/stores/toastStore";

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
  isCategoryMismatch?: boolean;
}

const DISTANCE_OPTIONS = [
  "Under 2km",
  "Under 5km",
  "Under 10km",
  "Under 20km",
  "Under 50km",
];
const PRICE_OPTIONS = ["Under 30k", "30k - 50k", "Over 50k"];

export function FilterSidebar({
  filters,
  setFilters,
  radiusInKm = 5,
  setRadiusInKm,
  selectedShop,
  onClearShop,
  categoryCounts = {},
  categories: propCategories,
  isLoadingCategories: propIsLoadingCategories,
  isSearching = false,
  isCategoryMismatch = false,
}: FilterSidebarProps) {
  const [internalCategories, setInternalCategories] = useState<string[]>([]);
  const [internalLoading, setInternalLoading] = useState(propCategories === undefined);

  // Custom distance input state & validation
  const [customDistance, setCustomDistance] = useState<string>(
    radiusInKm !== undefined ? String(radiusInKm) : "5",
  );
  const [distanceError, setDistanceError] = useState<string | null>(null);

  useEffect(() => {
    if (radiusInKm !== undefined) {
      setCustomDistance(String(radiusInKm));
    }
  }, [radiusInKm]);

  const handleCustomDistanceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value;
    setCustomDistance(raw);
    if (raw.trim() === "") {
      setDistanceError(null);
      return;
    }
    const val = parseFloat(raw);
    if (isNaN(val) || val < 0 || val > 50) {
      setDistanceError("Distance must be between 0 and 50 km");
    } else {
      setDistanceError(null);
      setRadiusInKm?.(val <= 0 ? 5 : val);
    }
  };

  const handleCustomDistanceBlur = () => {
    const trimmed = customDistance.trim();
    if (!trimmed) {
      setCustomDistance("5");
      setDistanceError(null);
      setRadiusInKm?.(5);
      return;
    }
    const val = parseFloat(trimmed);
    if (isNaN(val) || val <= 0 || val > 50) {
      setCustomDistance("5");
      setDistanceError(null);
      setRadiusInKm?.(5);
    }
  };

  // If parent didn't supply categories, fetch directly from backend DB API
  useEffect(() => {
    if (propCategories !== undefined) return;
    let isMounted = true;
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

  // Distance selection
  const toggleDistance = (value: string) => {
    const isSelected =
      filters.distance.includes(value) ||
      (value === "Under 10km" && filters.distance.includes("Over 5km - 10km")) ||
      (value === "Under 20km" && filters.distance.includes("10km - 20km")) ||
      (value === "Under 50km" && filters.distance.includes("20km - 50km"));

    const nextDistance = isSelected ? [] : [value];
    setFilters((prev) => ({
      ...prev,
      distance: nextDistance,
    }));
    if (!isSelected) {
      let mappedRadius = 5;
      if (value === "Under 2km") mappedRadius = 2;
      else if (value === "Under 5km") mappedRadius = 5;
      else if (value === "Under 10km" || value === "Over 5km - 10km") mappedRadius = 10;
      else if (value === "Under 20km" || value === "10km - 20km") mappedRadius = 20;
      else if (value === "Under 50km" || value === "20km - 50km") mappedRadius = 50;
      setRadiusInKm?.(mappedRadius);
      setCustomDistance(String(mappedRadius));
      setDistanceError(null);
    } else {
      // Uncheck -> Revert to default 5km
      setRadiusInKm?.(5);
      setCustomDistance("5");
      setDistanceError(null);
    }
  };

  // Single-select for price (clicking selected item unselects it)
  const toggleSinglePrice = (value: string) => {
    setFilters((prev) => ({
      ...prev,
      price: prev.price.includes(value) ? [] : [value],
    }));
  };

  const clearAll = () => {
    setFilters({ distance: [], price: [], category: [] });
    setRadiusInKm?.(5);
    setCustomDistance("5");
    setDistanceError(null);
    onClearShop();
    toast.success("Filters reset");
  };

  const categoryList = useMemo(() => {
    const set = new Set([...activeCategories, ...Object.keys(categoryCounts)]);
    return Array.from(set).filter(Boolean);
  }, [activeCategories, categoryCounts]);

  return (
    <Card aria-label="Deal filters" className="!p-3 lg:sticky lg:top-20">
      <div className="flex items-center justify-between gap-2 border-b border-border-default pb-2">
        <h2 className="flex items-center gap-2 text-body font-semibold text-text-primary">
          <span className="flex size-7 shrink-0 items-center justify-center rounded-md bg-primary-50 text-primary-700">
            <Filter className="size-4" aria-hidden="true" />
          </span>
          Filters
        </h2>
        <Button variant="ghost" size="sm" className="min-h-9 px-2" onClick={clearAll}>
          <RotateCcw className="size-4" aria-hidden="true" />
          Reset
        </Button>
      </div>

      {/* Categories */}
      <fieldset className="border-b border-border-default py-3">
        <div className="float-left mb-2 flex w-full items-center justify-between">
          <legend className="text-caption font-semibold text-text-primary">
            Categories
          </legend>
          {filters.category.length > 0 && (
            <button
              type="button"
              onClick={() => setFilters((prev) => ({ ...prev, category: [] }))}
              className="text-[11px] font-medium text-primary-700 hover:underline"
            >
              Clear
            </button>
          )}
        </div>
        {isCategoryMismatch ? (
          <div className="clear-both mb-2 rounded-md border border-amber-300 bg-amber-50 p-2 text-[11px] text-amber-900 animate-in fade-in duration-200">
            ⚠️ Selected category has no results for this keyword.
          </div>
        ) : isSearching && filters.category.length > 0 ? (
          <p className="clear-both mb-2 text-[11px] text-text-secondary">
            Searching in {filters.category.length} selected categories
          </p>
        ) : null}
        <div className="clear-both space-y-0.5">
          {isCategoriesLoading && !categoryList.length ? (
            <div role="status" aria-label="Loading categories" className="space-y-3">
              {[0, 1, 2].map((i) => (
                <Skeleton key={i} className="h-8" />
              ))}
            </div>
          ) : !categoryList.length ? (
            <p className="text-body text-text-secondary">No categories available.</p>
          ) : (
            categoryList.map((cat) => {
              const isSelected = filters.category.includes(cat);
              return (
                <label
                  key={cat}
                  className={`flex min-h-11 lg:min-h-8 cursor-pointer items-center justify-between gap-2 rounded-md px-2 text-caption transition-colors ${
                    isSelected && isCategoryMismatch
                      ? "border border-amber-300 bg-amber-50/70 text-amber-900"
                      : "text-text-secondary hover:bg-primary-50 has-checked:bg-primary-50 has-checked:text-primary-700"
                  }`}
                >
                  <span className="flex min-w-0 items-center gap-2">
                    <Checkbox
                      checked={isSelected}
                      onChange={() => toggle("category", cat)}
                    />
                    <span className="truncate" title={cat}>
                      {cat}
                    </span>
                  </span>
                  {isSelected && isCategoryMismatch && (
                    <span className="shrink-0 text-[11px] font-semibold text-amber-700">
                      (0)
                    </span>
                  )}
                </label>
              );
            })
          )}
        </div>
      </fieldset>

      {/* Distance */}
      <fieldset className="border-b border-border-default py-3">
        <legend className="float-left mb-2 w-full text-caption font-semibold text-text-primary">
          Distance
        </legend>
        <div className="clear-both space-y-2">
          <div>
            <label
              htmlFor="custom-distance-input"
              className="block text-[11px] font-medium text-text-secondary mb-1"
            >
              Custom radius (0 - 50 km)
            </label>
            <div className="relative">
              <input
                id="custom-distance-input"
                type="number"
                min={0}
                max={50}
                step={0.5}
                value={customDistance}
                onChange={handleCustomDistanceChange}
                onBlur={handleCustomDistanceBlur}
                placeholder="5"
                className={`w-full rounded-md border px-2.5 py-1 text-caption text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-1 ${
                  distanceError
                    ? "border-red-500 focus:border-red-500 focus:ring-red-500"
                    : "border-border-default focus:border-primary-500 focus:ring-primary-500"
                }`}
              />
              <span className="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2 text-caption text-text-tertiary">
                km
              </span>
            </div>
            {distanceError && (
              <p className="mt-1 text-[11px] text-red-500">{distanceError}</p>
            )}
          </div>

          <div className="space-y-0.5">
            {DISTANCE_OPTIONS.map((option) => (
              <label
                key={option}
                className="flex min-h-11 lg:min-h-8 cursor-pointer items-center gap-2 rounded-md px-2 text-caption text-text-secondary transition-colors hover:bg-primary-50 has-checked:bg-primary-50 has-checked:text-primary-700"
              >
                <Checkbox
                  checked={
                    filters.distance.includes(option) ||
                    (option === "Under 10km" && filters.distance.includes("Over 5km - 10km")) ||
                    (option === "Under 20km" && filters.distance.includes("10km - 20km")) ||
                    (option === "Under 50km" && filters.distance.includes("20km - 50km"))
                  }
                  onChange={() => toggleDistance(option)}
                />
                {option}
              </label>
            ))}
          </div>
        </div>
      </fieldset>

      {/* Price range */}
      <fieldset className="border-b border-border-default py-3 last:border-b-0">
        <legend className="float-left mb-2 w-full text-caption font-semibold text-text-primary">
          Price range
        </legend>
        <div className="clear-both space-y-0.5">
          {PRICE_OPTIONS.map((option) => (
            <label
              key={option}
              className="flex min-h-11 lg:min-h-8 cursor-pointer items-center gap-2 rounded-md px-2 text-caption text-text-secondary transition-colors hover:bg-primary-50 has-checked:bg-primary-50 has-checked:text-primary-700"
            >
              <Checkbox
                checked={filters.price.includes(option)}
                onChange={() => toggleSinglePrice(option)}
              />
              {option}
            </label>
          ))}
        </div>
      </fieldset>

      {selectedShop && (
        <div className="mt-3 flex min-w-0 items-center justify-between gap-2 rounded-md bg-primary-50 p-2 text-primary-700">
          <span className="truncate" title={selectedShop}>
            Store: {selectedShop}
          </span>
          <IconButton aria-label="Clear store filter" onClick={onClearShop}>
            <X className="size-4" aria-hidden="true" />
          </IconButton>
        </div>
      )}
    </Card>
  );
}
