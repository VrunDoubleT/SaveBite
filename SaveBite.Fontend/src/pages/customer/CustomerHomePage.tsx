import { useState } from "react";
import { SlidersHorizontal } from "lucide-react";
import { useNearbyFlashDeals } from "@/features/flash-deals/hooks/useNearbyFlashDeals";
import { FilterSidebar } from "@/features/flash-deals/components/FilterSidebar";
import { DealSection } from "@/features/flash-deals/components/DealSection";
import { NearbyStoresSection } from "@/features/shops/components/NearbyStoresSection";

export function CustomerHomePage() {
  const {
    deals,
    categoryCounts,
    serverMessage,
    isLoading,
    coords,
    isLocationReady,
    radiusInKm,
    setRadiusInKm,
    selectedShop,
    setSelectedShop,
    filters,
    setFilters,
    refreshDeals,
    categories,
    isLoadingCategories,
  } = useNearbyFlashDeals();

  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState(false);

  const activeFilterCount =
    filters.category.length +
    filters.distance.length +
    filters.price.length +
    (selectedShop ? 1 : 0);

  return (
    <div className="min-h-screen bg-neutral-50/40 pb-20 pt-6">
      <main className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Mobile / Tablet Filter Toggle Bar */}
        <div className="mb-4 flex items-center justify-between rounded-xl border border-neutral-200 bg-white p-3 shadow-2xs lg:hidden">
          <button
            type="button"
            onClick={() => setIsMobileFilterOpen(!isMobileFilterOpen)}
            className="flex items-center gap-2 text-xs font-bold text-neutral-800"
          >
            <SlidersHorizontal size={15} className="text-emerald-600" />
            <span>Filters</span>
            {activeFilterCount > 0 && (
              <span className="rounded-full bg-emerald-600 px-2 py-0.5 text-[10px] font-bold text-white">
                {activeFilterCount} active
              </span>
            )}
          </button>
          <div className="flex items-center gap-2">
            {activeFilterCount > 0 && (
              <button
                type="button"
                onClick={() => {
                  setFilters({ category: [], distance: [], price: [] });
                  setSelectedShop(null);
                }}
                className="text-xs font-semibold text-neutral-500 hover:text-emerald-700"
              >
                Clear all
              </button>
            )}
            <button
              type="button"
              onClick={() => setIsMobileFilterOpen(!isMobileFilterOpen)}
              className="rounded-lg bg-neutral-100 px-2.5 py-1 text-xs font-semibold text-neutral-700 transition hover:bg-neutral-200"
            >
              {isMobileFilterOpen ? "Hide Filters" : "Show Filters"}
            </button>
          </div>
        </div>

        {/* Main Layout Grid */}
        <div className="grid grid-cols-1 items-start gap-6 lg:grid-cols-[280px_minmax(0,1fr)] lg:gap-8">
          {/* Sidebar: In-flow toggle on mobile, sticky left column on desktop */}
          <div className={isMobileFilterOpen ? "block" : "hidden lg:block"}>
            <FilterSidebar
              filters={filters}
              setFilters={setFilters}
              radiusInKm={radiusInKm}
              setRadiusInKm={setRadiusInKm}
              selectedShop={selectedShop}
              onClearShop={() => setSelectedShop(null)}
              categoryCounts={categoryCounts}
              categories={categories}
              isLoadingCategories={isLoadingCategories}
            />
          </div>

          {/* Main Content Area */}
          <div className="space-y-12 min-w-0">
            {/* Deals section */}
            <DealSection
              deals={deals}
              isLoading={isLoading}
              emptyMessage={serverMessage ?? undefined}
              onRefresh={refreshDeals}
            />

            {/* View Nearby Stores */}
            <NearbyStoresSection
              latitude={coords.latitude}
              longitude={coords.longitude}
              radiusInKm={radiusInKm}
              isLocationReady={isLocationReady}
            />
          </div>
        </div>
      </main>
    </div>
  );
}
