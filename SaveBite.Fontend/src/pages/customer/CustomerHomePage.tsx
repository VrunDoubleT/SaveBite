import { useState } from "react";
import { SlidersHorizontal, Navigation, AlertCircle, Home } from "lucide-react";
import { useNearbyFlashDeals } from "@/features/flash-deals/hooks/useNearbyFlashDeals";
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
    searchKeyword,
    setSearchKeyword,
    clearSearch,
    filters,
    setFilters,
    refreshDeals,
    categories,
    isLoadingCategories,
    locationMode,
    locationLabel,
    locationAddress,
    locationVersion,
    gpsError,
    isGpsLoading,
    requestGpsLocation,
    selectSavedAddress,
    selectDefaultLocation,
    userDefaultAddress,
  } = useNearbyFlashDeals();

  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState(false);

  const activeFilterCount =
    filters.category.length +
    filters.distance.length +
    filters.price.length +
    (selectedShop ? 1 : 0);

  // When GPS location is not ready: show location request screen matching NotFoundPage style
  if (!isLocationReady) {
    return (
      <div className="-mx-4 -my-8 md:-mx-8 md:-my-12 flex min-h-[calc(100vh-80px)] items-center justify-center bg-bg-body bg-[radial-gradient(circle_at_1px_1px,rgba(18,110,68,0.12)_1px,transparent_0)] p-6 [background-size:22px_22px]">
        <section className="w-full max-w-2xl text-center py-10">
          <LocationPlateIllustration />

          <h1 className="mt-2 text-3xl font-black tracking-tight text-text-primary sm:text-4xl">
            Enable Location to Discover Flash Deals
          </h1>
          <p className="mx-auto mt-3 max-w-md leading-relaxed text-text-secondary text-sm sm:text-base">
            SaveBite needs your location to discover exclusive flash deals, discount meals, and nearby stores on VietMap.
          </p>

          <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
            <button
              type="button"
              onClick={requestGpsLocation}
              disabled={isGpsLoading}
              className="inline-flex min-h-11 items-center justify-center gap-2 rounded-md bg-primary-600 px-6 py-3 text-sm font-bold text-white shadow-sm transition hover:bg-primary-700 focus-visible:outline-none focus-visible:shadow-focus disabled:opacity-60 cursor-pointer"
            >
              <Navigation className={`size-4 ${isGpsLoading ? "animate-spin" : ""}`} aria-hidden="true" />
              <span>{isGpsLoading ? "Locating your position..." : "Enable Location Now"}</span>
            </button>

            {userDefaultAddress && (
              <button
                type="button"
                onClick={selectSavedAddress}
                className="inline-flex min-h-11 items-center justify-center gap-2 rounded-md border border-primary-700 bg-bg-body px-5 py-3 text-sm font-bold text-primary-800 transition hover:bg-primary-800 hover:text-white focus-visible:outline-none focus-visible:shadow-focus cursor-pointer"
              >
                <Home className="size-4" aria-hidden="true" />
                <span>Use {userDefaultAddress.label || "Saved Address"}</span>
              </button>
            )}
          </div>

          {gpsError && (
            <div className="mx-auto mt-6 max-w-md rounded-xl border border-amber-200 bg-amber-50/90 p-4 text-left text-xs text-amber-900 shadow-xs">
              <div className="flex items-start gap-2.5">
                <AlertCircle className="size-4 text-amber-600 shrink-0 mt-0.5" />
                <div className="space-y-1">
                  <p className="font-bold">{gpsError}</p>
                  <p className="text-amber-800 leading-relaxed">
                    Tip: Click the <strong>lock / site settings icon (🔒 / ⚙️)</strong> in your browser address bar &rarr; Set <strong>Location</strong> to <strong>Allow</strong> &rarr; Click &ldquo;Enable Location Now&rdquo; again{userDefaultAddress ? ", or use your saved address to browse immediately." : "."}
                  </p>
                </div>
              </div>
            </div>
          )}
        </section>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-neutral-50/40 pb-20 pt-6">
      <main className="mx-auto max-w-7xl ">
        {/* 1. VietMap Nearby Stores & Interactive Map Banner */}
        <div className="mb-10">
          <NearbyStoresMapSection
                latitude={coords?.latitude}
                longitude={coords?.longitude}
                radiusInKm={radiusInKm}
                isLocationReady={isLocationReady}
                locationMode={locationMode}
                locationLabel={locationLabel}
                locationAddress={locationAddress}
                locationVersion={locationVersion}
                isGpsLoading={isGpsLoading}
                gpsError={gpsError}
                onRequestGps={requestGpsLocation}
                onSelectSavedAddress={selectSavedAddress}
                onSelectDefaultLocation={selectDefaultLocation}
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

            {/* 3. Main Layout Grid: Filters Sidebar & Flash Deals */}
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
                  isSearching={Boolean(searchKeyword && searchKeyword.trim().length > 0)}
                />
              </div>

              {/* Flash Deals Section */}
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
                  searchKeyword={searchKeyword}
                  onSearchChange={(kw) => {
                    setSearchKeyword(kw);
                    if (kw.trim() && filters.category.length > 0) {
                      setFilters((prev) => ({ ...prev, category: [] }));
                    }
                  }}
                  onClearSearch={clearSearch}
                  locationAddress={locationAddress || locationLabel}
                  locationMode={locationMode}
                />
              </div>
            </div>
      </main>
    </div>
  );
}

function LocationPlateIllustration() {
  return (
    <svg viewBox="0 0 320 300" className="mx-auto w-full max-w-72" fill="none" aria-hidden="true">
      {/* Plate shadow */}
      <ellipse cx="160" cy="252" rx="118" ry="14" className="fill-neutral-900/8" />

      {/* Plate Outer Rim */}
      <circle
        cx="160"
        cy="150"
        r="118"
        className="fill-bg-surface stroke-primary-700/15"
        strokeWidth="2"
      />
      {/* Plate Inner Base */}
      <circle
        cx="160"
        cy="150"
        r="92"
        className="fill-primary-50/60 stroke-primary-700/15"
        strokeWidth="1.5"
      />

      {/* Seasoning / Crumbs */}
      <g>
        <circle cx="118" cy="120" r="3.2" className="fill-accent-500" />
        <circle cx="205" cy="108" r="2.4" className="fill-primary-500" />
        <circle cx="196" cy="176" r="2.8" className="fill-primary-700/70" />
        <circle cx="130" cy="188" r="2" className="fill-accent-500" />
        <circle cx="172" cy="202" r="2.4" className="fill-primary-400" />
        <circle cx="104" cy="157" r="1.8" className="fill-primary-600" />
        <circle cx="215" cy="143" r="1.8" className="fill-accent-500" />
        <circle cx="154" cy="108" r="1.5" className="fill-primary-300" />
        <path d="M150 132q6-4 11 1-3 6-9 5-4-2-2-6Z" className="fill-primary-700/60" />
        <path d="m181 157 6-3 4 5-7 4Z" className="fill-accent-500/80" />
      </g>

      {/* Left Fork */}
      <g transform="translate(90 150) rotate(-28)" className="fill-primary-800 stroke-primary-800">
        <rect x="-3.5" y="-6" width="7" height="70" rx="3.5" stroke="none" />
        <path d="M-10-6v-22M-3.5-6v-26M3.5-6v-26M10-6v-22" strokeWidth="4" strokeLinecap="round" />
        <path d="M-10-28q0 12 10 12t10-12" fill="none" strokeWidth="4" strokeLinecap="round" />
      </g>

      {/* Right Knife */}
      <g transform="translate(232 150) rotate(28)" className="fill-primary-800">
        <rect x="-3.5" y="10" width="7" height="55" rx="3.5" />
        <path d="M-6 8c0-30 16-42 16-48 0 10-6 48-6 48Z" />
      </g>

      {/* Location Badge (hanging tag matching 404 tag) */}
      <g transform="translate(160 86) rotate(-4)">
        <rect x="-46" y="-24" width="92" height="46" rx="6" className="fill-primary-600" />
        <circle cx="0" cy="-24" r="4" className="fill-bg-surface" />
        <g transform="translate(-24, -9)">
          <path
            d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5a2.5 2.5 0 1 1 0-5 2.5 2.5 0 0 1 0 5z"
            fill="white"
          />
        </g>
        <text x="8" y="7" textAnchor="middle" className="fill-white text-[15px] font-black tracking-wide">
          GPS
        </text>
      </g>
    </svg>
  );
}
