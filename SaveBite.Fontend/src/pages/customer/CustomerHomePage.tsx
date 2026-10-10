import { useState } from "react";
import { SlidersHorizontal, Navigation, AlertCircle } from "lucide-react";
import { useNearbyFlashDeals } from "@/features/flash-deals/hooks/useNearbyFlashDeals";
import { LocationBar } from "@/features/flash-deals/components/LocationBar";
import { FilterSidebar } from "@/features/flash-deals/components/FilterSidebar";
import { DealSection } from "@/features/flash-deals/components/DealSection";
import { NearbyStoresMapSection } from "@/features/shops/components/NearbyStoresMapSection";

export function CustomerHomePage() {
  const {
    deals,
    categoryCounts,
    serverMessage,
    isLoading,
    isLoadingOlder,
    hasOlder,
    newDealsCount,
    loadOlderDeals,
    applyNewDeals,
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
    locationMode,
    locationLabel,
    locationAddress,
    gpsError,
    isGpsLoading,
    requestGpsLocation,
    selectSavedAddress,
    selectDefaultLocation,
    userDefaultAddress,
    isAuthenticated,
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
        {/* Khi CHƯA CẤP GPS: Chỉ hiển thị duy nhất màn hình mời bật GPS, ẩn LocationBar */}
        {!isLocationReady ? (
          <div className="my-8 flex flex-col items-center justify-center rounded-2xl border border-dashed border-emerald-300 bg-white px-6 py-16 text-center shadow-xs">
            <div className="relative mb-5 flex h-16 w-16 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600 ring-8 ring-emerald-50/50">
              <Navigation size={30} className={isGpsLoading ? "animate-spin" : "animate-pulse"} />
            </div>
            <h3 className="text-lg font-bold text-neutral-900 sm:text-xl">
              Enable Location to Discover Flash Deals
            </h3>
            <p className="mt-2 max-w-md text-xs sm:text-sm text-neutral-500">
              SaveBite needs your current location to discover exclusive flash deals and stores near you.
            </p>

            {gpsError && (
              <div className="mt-4 flex items-center gap-2 rounded-xl border border-amber-200 bg-amber-50 px-4 py-2.5 text-xs font-medium text-amber-800 max-w-md text-left">
                <AlertCircle size={16} className="text-amber-600 shrink-0" />
                <span>{gpsError}</span>
              </div>
            )}

            <button
              type="button"
              onClick={requestGpsLocation}
              disabled={isGpsLoading}
              className="mt-6 inline-flex items-center gap-2 rounded-xl bg-emerald-600 px-6 py-3 text-sm font-bold text-white shadow-md shadow-emerald-600/25 transition hover:bg-emerald-700 disabled:opacity-60 cursor-pointer"
            >
              <Navigation size={16} className={isGpsLoading ? "animate-spin" : ""} />
              <span>{isGpsLoading ? "Locating..." : "Enable Location Now"}</span>
            </button>
          </div>
        ) : (
          <>
            {/* In-Page Location Selector Bar: Chỉ hiện khi ĐÃ CẤP GPS */}
            <LocationBar
              locationMode={locationMode}
              locationLabel={locationLabel}
              locationAddress={locationAddress}
              coords={coords}
              isGpsLoading={isGpsLoading}
              gpsError={gpsError}
              onRequestGps={requestGpsLocation}
              onSelectSavedAddress={selectSavedAddress}
              onSelectDefaultLocation={selectDefaultLocation}
              userDefaultAddress={userDefaultAddress}
              isAuthenticated={isAuthenticated}
            />

            {/* 1. VietMap Nearby Stores & Routing: Thay thế Nearby Stores cũ */}
            <div className="mb-10">
              <NearbyStoresMapSection
                latitude={coords?.latitude}
                longitude={coords?.longitude}
                radiusInKm={radiusInKm}
                isLocationReady={isLocationReady}
                locationMode={locationMode}
                locationLabel={locationLabel}
                locationAddress={locationAddress}
                onRequestGps={requestGpsLocation}
                onSelectSavedAddress={selectSavedAddress}
                hasDefaultAddress={Boolean(userDefaultAddress)}
                userDefaultAddressLabel={userDefaultAddress?.label || userDefaultAddress?.addressLine}
              />
            </div>

            {/* 2. Mobile / Tablet Filter Toggle Bar */}
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

            {/* 3. Main Layout Grid: FILTER & FLASH DEAL CHUNG VỚI NHAU */}
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

              {/* Flash Deals Section (Chung hàng với Filter) */}
              <div className="min-w-0">
                <DealSection
                  deals={deals}
                  isLoading={isLoading}
                  emptyMessage={serverMessage ?? undefined}
                  onRefresh={refreshDeals}
                  hasOlder={hasOlder}
                  isLoadingOlder={isLoadingOlder}
                  onLoadOlder={loadOlderDeals}
                  newDealsCount={newDealsCount}
                  onApplyNewDeals={applyNewDeals}
                />
              </div>
            </div>
          </>
        )}
      </main>
    </div>
  );
}
