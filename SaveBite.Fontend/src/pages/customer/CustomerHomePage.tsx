import { useState } from "react";
import {
  SlidersHorizontal,
  Navigation,
  AlertCircle,
  Home,
  MapPin,
  Map,
  ChevronDown,
  Leaf,
} from "lucide-react";
import { Badge, Button, Card, ErrorState, PageHeader } from "@/shared/components/ui";
import { useNearbyFlashDeals } from "@/features/flash-deals/hooks/useNearbyFlashDeals";
import { FilterSidebar } from "@/features/flash-deals/components/FilterSidebar";
import { DealSection } from "@/features/flash-deals/components/DealSection";
import { NearbyStoresMapSection } from "@/features/shops/components/NearbyStoresMapSection";

export function CustomerHomePage() {
  const {
    deals,
    categoryCounts,
    serverMessage,
    error,
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
  const [isMapOpen, setIsMapOpen] = useState(false);

  const activeFilterCount =
    filters.category.length +
    filters.distance.length +
    filters.price.length +
    (selectedShop ? 1 : 0);

  if (!isLocationReady) {
    return (
      <div className="flex min-h-dvh items-center justify-center py-12">
        <Card className="w-full max-w-xl text-center">
          <span className="mx-auto mb-6 flex size-16 items-center justify-center rounded-full bg-primary-50 text-primary-700">
            <MapPin className="size-8" aria-hidden="true" />
          </span>
          <PageHeader
            title="Good food is closer than you think"
            description="Choose your location to find flash deals and stores nearby."
          />
          <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
            <Button size="lg" onClick={requestGpsLocation} loading={isGpsLoading}>
              <Navigation className="size-4" aria-hidden="true" />
              {isGpsLoading ? "Finding your location…" : "Use my location"}
            </Button>
            {userDefaultAddress && (
              <Button size="lg" variant="secondary" onClick={selectSavedAddress}>
                <Home className="size-4" aria-hidden="true" />
                Use saved address
              </Button>
            )}
          </div>
          {gpsError && (
            <div
              role="alert"
              className="mt-6 rounded-md bg-accent-50 p-4 text-left text-accent-700"
            >
              <p className="flex items-start gap-2 font-medium">
                <AlertCircle className="size-5 shrink-0" aria-hidden="true" />
                {gpsError}
              </p>
              <p className="mt-2 text-body">
                Allow Location in your browser's site settings, then try again
                {userDefaultAddress ? ", or use your saved address." : "."}
              </p>
            </div>
          )}
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6 py-6 sm:space-y-8 sm:py-8">
      <div className="flex items-start justify-between gap-4 border-l-4 border-primary-300 pl-4">
        <PageHeader
          title="A little less waste. A lot more taste."
          description="Discover nearby flash deals. Find your next bite at a better price."
        />
        <span className="hidden size-12 shrink-0 items-center justify-center rounded-full bg-primary-100 text-primary-700 sm:flex">
          <Leaf className="size-6" aria-hidden="true" />
        </span>
      </div>

      <section
        aria-label="Browsing location"
        className="rounded-lg border border-border-default bg-bg-surface shadow-sm"
      >
        <div className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-md bg-primary-50 text-primary-700">
              <MapPin className="size-5" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="text-caption font-medium text-text-secondary">
                Deals near you ·{" "}
                {locationMode === "saved_address"
                  ? "Saved address"
                  : locationMode === "default"
                    ? "Default location"
                    : "GPS location"}
              </p>
              <p
                className="truncate font-semibold text-text-primary"
                title={locationAddress || locationLabel}
              >
                {locationAddress || locationLabel}
              </p>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-2 sm:flex sm:flex-wrap">
            <Button
              variant="secondary"
              className="min-h-11 aria-pressed:border-primary-200 aria-pressed:bg-primary-50 aria-pressed:text-primary-700"
              onClick={requestGpsLocation}
              loading={isGpsLoading}
              aria-pressed={locationMode === "gps"}
            >
              <Navigation className="size-4" aria-hidden="true" />
              Use GPS
            </Button>
            {userDefaultAddress && (
              <Button
                variant="secondary"
                className="min-h-11 aria-pressed:border-secondary-100 aria-pressed:bg-secondary-50 aria-pressed:text-secondary-700"
                onClick={selectSavedAddress}
                aria-pressed={locationMode === "saved_address"}
              >
                <Home className="size-4" aria-hidden="true" />
                Saved address
              </Button>
            )}
          </div>
        </div>
        {gpsError && (
          <p
            role="alert"
            className="mx-4 mb-4 flex items-start gap-2 rounded-md bg-accent-50 p-3 text-accent-700"
          >
            <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
            {gpsError}
          </p>
        )}
        <details
          onToggle={(event) => setIsMapOpen(event.currentTarget.open)}
          className="border-t border-border-default"
        >
          <summary className="flex min-h-12 cursor-pointer list-none items-center justify-between gap-2 rounded-b-lg px-4 text-body font-medium text-primary-700 transition-colors hover:bg-primary-50">
            <span className="flex items-center gap-2">
              <Map className="size-4" aria-hidden="true" />
              Explore nearby stores on the map
            </span>
            <ChevronDown
              className={`size-4 shrink-0 transition-transform ${isMapOpen ? "rotate-180" : ""}`}
              aria-hidden="true"
            />
          </summary>
          {isMapOpen && (
            <div className="border-t border-border-default p-4">
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
                userDefaultAddressLabel={
                  userDefaultAddress?.label || userDefaultAddress?.addressLine
                }
              />
            </div>
          )}
        </details>
      </section>

      <div className="lg:hidden">
        <Button
          variant="secondary"
          className="min-h-11 w-full"
          aria-expanded={isMobileFilterOpen}
          aria-controls="home-filters"
          onClick={() => setIsMobileFilterOpen(!isMobileFilterOpen)}
        >
          <SlidersHorizontal className="size-4" aria-hidden="true" />
          {isMobileFilterOpen ? "Hide filters" : "Filter deals"}
          {activeFilterCount > 0 && <Badge tone="primary">{activeFilterCount} active</Badge>}
        </Button>
      </div>
      <div className="grid items-start gap-6 lg:grid-cols-[16rem_minmax(0,1fr)]">
        <div
          id="home-filters"
          className={isMobileFilterOpen ? "min-w-0" : "hidden min-w-0 lg:block"}
        >
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
        <div className="min-w-0">
          {error && (
            <div className="mb-4">
              <ErrorState
                title="Couldn't load flash deals"
                description="Something went wrong while loading nearby deals. Please try again."
                onRetry={refreshDeals}
              />
            </div>
          )}
          {(!error || deals.length > 0) && (
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
          )}
        </div>
      </div>
    </div>
  );
}
