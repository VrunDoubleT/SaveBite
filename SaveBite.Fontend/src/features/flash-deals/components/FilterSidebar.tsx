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
  const [internalLoading, setInternalLoading] = useState(propCategories === undefined);

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
      <fieldset className="border-b border-border-default py-3" disabled={isSearching}>
        <legend className="float-left mb-2 w-full text-caption font-semibold text-text-primary">
          Categories
        </legend>
        <div className="clear-both space-y-0.5">
          {isSearching ? (
            <p className="text-caption text-text-secondary">Clear search to browse categories.</p>
          ) : isCategoriesLoading && !categoryList.length ? (
            <div role="status" aria-label="Loading categories" className="space-y-3">
              {[0, 1, 2].map((i) => (
                <Skeleton key={i} className="h-8" />
              ))}
            </div>
          ) : !categoryList.length ? (
            <p className="text-body text-text-secondary">No categories available.</p>
          ) : (
            categoryList.map((cat) => (
              <label
                key={cat}
                className="flex min-h-11 lg:min-h-8 cursor-pointer items-center justify-between gap-2 rounded-md px-2 text-caption text-text-secondary transition-colors hover:bg-primary-50 has-checked:bg-primary-50 has-checked:text-primary-700"
              >
                <span className="flex min-w-0 items-center gap-2">
                  <Checkbox
                    checked={filters.category.includes(cat)}
                    onChange={() => toggle("category", cat)}
                  />
                  <span className="truncate" title={cat}>
                    {cat}
                  </span>
                </span>

              </label>
            ))
          )}
        </div>
      </fieldset>
      {(
        [
          { key: "distance", title: "Distance", options: DISTANCE_OPTIONS },
          { key: "price", title: "Price range", options: PRICE_OPTIONS },
        ] as const
      ).map((group) => (
        <fieldset key={group.key} className="border-b border-border-default py-3 last:border-b-0">
          <legend className="float-left mb-2 w-full text-caption font-semibold text-text-primary">
            {group.title}
          </legend>
          <div className="clear-both space-y-0.5">
            {group.options.map((option) => (
              <label
                key={option}
                className="flex min-h-11 lg:min-h-8 cursor-pointer items-center gap-2 rounded-md px-2 text-caption text-text-secondary transition-colors hover:bg-primary-50 has-checked:bg-primary-50 has-checked:text-primary-700"
              >
                <Checkbox
                  checked={filters[group.key].includes(option)}
                  onChange={() => toggleSingle(group.key, option)}
                />
                {option}
              </label>
            ))}
          </div>
        </fieldset>
      ))}
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
