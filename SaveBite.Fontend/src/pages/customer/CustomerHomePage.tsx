import { useAuthStore } from "@/shared/stores/authStore";
import { CustomerSuspensionBanner } from "@/shared/components/CustomerSuspensionBanner";
import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { useHomePreferencesStore } from "@/features/shops/stores/homePreferencesStore";
import {
  SlidersHorizontal,
  Navigation,
  AlertCircle,
  Home,
  MapPin,
  Map,
  ChevronDown,
  Pause,
  Play,
} from "lucide-react";
import { Badge, Button, ErrorState, ScrollToTopButton } from "@/shared/components/ui";
import { NewDealAlertToast } from "@/features/flash-deals/components/NewDealAlertToast";
import {
  useNearbyFlashDeals,
  cleanAddressParts,
} from "@/features/flash-deals/hooks/useNearbyFlashDeals";
import { FilterSidebar } from "@/features/flash-deals/components/FilterSidebar";
import { DealSection } from "@/features/flash-deals/components/DealSection";
import { NearbyStoresBrowser } from "@/features/shops/components/NearbyStoresBrowser";

export function CustomerHomePage() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const customerSuspended = useAuthStore(
    (state) => state.isAuthenticated && state.user?.customerStatus === "suspended",
  );
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

  const isMobileFilterOpen = useHomePreferencesStore((state) => state.mobileFiltersExpanded);
  const setIsMobileFilterOpen = useHomePreferencesStore((state) => state.setMobileFiltersExpanded);
  const isMapOpen = useHomePreferencesStore((state) => state.storesExpanded);
  const setIsMapOpen = useHomePreferencesStore((state) => state.setStoresExpanded);
  const [shopSearchKeyword, setShopSearchKeyword] = useState("");
  const [isCategoryAnimationPaused, setIsCategoryAnimationPaused] = useState(false);
  const shouldAnimateCategories = categories.length > 0;
  const [categoryRepeatCount, setCategoryRepeatCount] = useState(1);
  const categoryViewportRef = useRef<HTMLDivElement>(null);
  const categoryGroupRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const viewport = categoryViewportRef.current;
    const group = categoryGroupRef.current;
    if (!viewport || !group) return;
    const measure = () => {
      const sequenceWidth = group.scrollWidth / categoryRepeatCount;
      if (sequenceWidth > 0) {
        setCategoryRepeatCount(Math.max(1, Math.ceil(viewport.clientWidth / sequenceWidth)));
      }
    };
    const observer = new ResizeObserver(measure);
    observer.observe(viewport);
    observer.observe(group);
    measure();
    return () => observer.disconnect();
  }, [categories, categoryRepeatCount]);

  const activeFilterCount =
    filters.category.length +
    filters.distance.length +
    filters.price.length +
    (selectedShop ? 1 : 0);

  const isCategoryMismatch = Boolean(
    !isLoading &&
    deals.length === 0 &&
    searchKeyword.trim().length > 0 &&
    filters.category.length > 0,
  );

  const defaultAddressText = userDefaultAddress
    ? cleanAddressParts([
        userDefaultAddress.addressLine,
        userDefaultAddress.ward,
        userDefaultAddress.district,
        userDefaultAddress.city,
      ]) || userDefaultAddress.addressLine
    : "";

  if (!isLocationReady) {
    return (
      <div
        className={`relative isolate w-full overflow-hidden ${customerSuspended ? "min-h-[calc(100dvh-4rem)]" : "h-[calc(100dvh-4rem)]"}`}
      >
        <div
          className="pointer-events-none absolute inset-0 -z-10 bg-cover bg-center"
          style={{
            backgroundImage:
              "linear-gradient(180deg,rgba(8,50,33,.52),rgba(8,50,33,.34) 48%,rgba(8,50,33,.58)),url(https://res.cloudinary.com/qhd4t4qt/image/upload/f_auto,q_auto,w_1920/v1793859170/IMG_6341.png)",
          }}
          aria-hidden="true"
        />
        <div
          className={`mx-auto flex w-full max-w-320 flex-col gap-3 px-4 py-3 sm:gap-4 sm:py-5 md:px-6 ${customerSuspended ? "min-h-[calc(100dvh-4rem)]" : "h-full"}`}
        >
          <div className="shrink-0 empty:hidden">
            <CustomerSuspensionBanner />
          </div>
          <section
            aria-labelledby="location-welcome-heading"
            className="grid min-h-0 flex-1 grid-rows-[auto_minmax(180px,1fr)] items-center gap-3 overflow-hidden rounded-[1.75rem] border border-white/50 bg-white/[0.88] p-5 shadow-[0_24px_70px_rgba(16,33,26,.22)] backdrop-blur-xl sm:grid-cols-[1.05fr_.95fr] sm:grid-rows-1 sm:gap-5 sm:rounded-[2rem] sm:p-7 lg:gap-8 lg:px-14 lg:py-[clamp(2rem,6vh,4rem)]"
          >
            <div className="relative z-10 min-w-0">
              <span className="mb-2 inline-flex items-center gap-2 rounded-full bg-white px-2.5 py-1.5 text-[11px] font-bold uppercase tracking-[.08em] text-primary-800 shadow-sm sm:mb-4 sm:px-3 sm:text-xs">
                <span className="flex size-6 items-center justify-center rounded-full bg-primary-700 text-white">
                  <MapPin className="size-3.5" aria-hidden="true" />
                </span>
                Flash deals near you
              </span>
              <h1
                id="location-welcome-heading"
                className="max-w-[13ch] text-[clamp(1.75rem,4.4vw,3.75rem)] font-extrabold leading-[1.02] tracking-[-.04em] text-slate-950"
              >
                Good food is <span className="text-primary-700">closer</span> than you think
              </h1>
              <p className="mt-2 max-w-[44ch] text-xs leading-5 text-slate-700 sm:mt-4 sm:text-sm sm:leading-6">
                Find discounted dishes from nearby shops in the last hours before closing. Reserve
                and pick up in time.
              </p>
              <div className="mt-3 flex flex-wrap items-center gap-2 sm:mt-6 sm:gap-3">
                <Button
                  size="lg"
                  className="min-h-11 rounded-full bg-primary-700 px-5 text-white shadow-lg shadow-primary-700/25 hover:bg-primary-800 sm:min-h-13 sm:px-7"
                  onClick={requestGpsLocation}
                  loading={isGpsLoading}
                >
                  <Navigation className="size-4 shrink-0" aria-hidden="true" />
                  {isGpsLoading
                    ? "Finding your location…"
                    : gpsError
                      ? "Try my location again"
                      : "Use my location"}
                </Button>

                {isAuthenticated && userDefaultAddress && (
                  <Button
                    size="lg"
                    className="min-h-11 rounded-full bg-primary-700 px-5 text-white shadow-lg shadow-primary-700/25 hover:bg-primary-800 sm:min-h-13 sm:px-7"
                    onClick={selectSavedAddress}
                    title={defaultAddressText}
                  >
                    <Home className="size-4 shrink-0" aria-hidden="true" />
                    Use my default address
                  </Button>
                )}
              </div>

              {/* If authenticated but no default address saved yet */}
              {isAuthenticated && !userDefaultAddress && (
                <div className="mt-3 flex items-center gap-2 text-xs text-slate-600">
                  <Home className="size-4 shrink-0 text-slate-400" />
                  <span>No default address saved.</span>
                  <Link
                    to={APP_PATHS.ACCOUNT_PROFILE}
                    className="font-semibold text-primary-700 underline hover:text-primary-800"
                  >
                    Add one in your profile
                  </Link>
                </div>
              )}
              <p className="mt-2 max-w-md text-[11px] leading-4 text-slate-600 sm:mt-3 sm:text-xs sm:leading-5">
                Allow location access in your browser to use GPS. You can change your browsing
                location later.
              </p>
              {isGpsLoading && (
                <p role="status" className="mt-1 text-xs font-medium text-primary-800">
                  Waiting for your browser&apos;s location response…
                </p>
              )}
              {gpsError && (
                <div
                  role="alert"
                  className="mt-2 flex items-start gap-2 rounded-xl border border-red-200 bg-red-50/95 p-2.5 text-xs leading-5 text-red-800 sm:mt-3 sm:p-3"
                >
                  <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
                  <span className="min-w-0 [overflow-wrap:anywhere]">
                    {gpsError} Allow Location in your browser settings, then try again
                    {userDefaultAddress ? " or use your saved address." : "."}
                  </span>
                </div>
              )}
              <div className="mt-3 hidden max-w-md border-t border-slate-900/10 pt-3 sm:flex sm:gap-0 sm:pt-4">
                <HomeStat value="30–120′" label="Before closing" />
                <HomeStat value="Pick-up" label="Collect in store" />
                <HomeStat value="Per dish" label="Photo & price" />
              </div>
            </div>
            <HomeFoodVisual />
          </section>

          <section aria-label="Browse food categories" className="shrink-0">
            <div className="mb-1.5 flex items-center justify-between px-1 text-white sm:mb-2">
              <h2 className="text-xs font-bold tracking-wide drop-shadow sm:text-sm">
                Explore categories
              </h2>
              {categories.length > 0 && shouldAnimateCategories && (
                <button
                  type="button"
                  aria-label={
                    isCategoryAnimationPaused
                      ? "Play category animation"
                      : "Pause category animation"
                  }
                  onClick={() => setIsCategoryAnimationPaused((paused) => !paused)}
                  className="inline-flex min-h-8 items-center gap-1 rounded-full px-2 text-[11px] font-semibold text-white/90 transition-colors hover:bg-white/15 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white"
                >
                  {isCategoryAnimationPaused ? (
                    <Play className="size-3.5" aria-hidden="true" />
                  ) : (
                    <Pause className="size-3.5" aria-hidden="true" />
                  )}
                  {isCategoryAnimationPaused ? "Play" : "Pause"}
                </button>
              )}
            </div>
            {categories.length > 0 ? (
              <div
                ref={categoryViewportRef}
                className={`overflow-hidden pb-1 ${shouldAnimateCategories ? "[mask-image:linear-gradient(90deg,transparent,#000_5%,#000_95%,transparent)]" : ""}`}
              >
                <div
                  className={`flex w-max ${shouldAnimateCategories ? "animate-[categoryScroll_38s_linear_infinite]" : ""} motion-reduce:animate-none hover:[animation-play-state:paused] focus-within:[animation-play-state:paused] ${
                    isCategoryAnimationPaused ? "[animation-play-state:paused]" : ""
                  }`}
                >
                  {(shouldAnimateCategories ? [0, 1] : [0]).map((copy) => (
                    <div
                      key={copy}
                      ref={copy === 0 ? categoryGroupRef : undefined}
                      aria-hidden={copy === 1 ? true : undefined}
                      className="flex shrink-0 gap-4 pr-4"
                    >
                      {Array.from({ length: categoryRepeatCount }, (_, repeat) => categories.map((category) => (
                        <span
                          key={`${repeat}:${category}`}
                          aria-hidden={repeat > 0 ? true : undefined}
                          className="inline-flex min-h-11 shrink-0 items-center rounded-full border border-white/60 bg-white/95 px-5 text-sm font-semibold text-slate-800 shadow-md backdrop-blur-md"
                        >
                          {category}
                        </span>
                      )))}
                    </div>
                  ))}
                </div>
                <style>{`
                  @keyframes categoryScroll {
                    to { transform: translateX(-50%); }
                  }
                `}</style>
              </div>
            ) : (
              !isLoadingCategories && (
                <p className="rounded-full bg-black/30 px-4 py-2 text-xs text-white">
                  No food categories are available right now.
                </p>
              )
            )}
          </section>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto w-full max-w-320 px-4 py-4 md:p-6">
      <div className="space-y-6 pb-6 sm:space-y-8 sm:pb-8">
        <CustomerSuspensionBanner />
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
            open={isMapOpen}
            onToggle={(event) => setIsMapOpen(event.currentTarget.open)}
            className="border-t border-border-default"
          >
            <summary className="flex min-h-12 cursor-pointer list-none items-center justify-between gap-2 rounded-b-lg px-4 text-body font-medium text-primary-700 transition-colors hover:bg-primary-50">
              <span className="flex items-center gap-2">
                <Map className="size-4" aria-hidden="true" />
                Explore nearby stores
              </span>
              <ChevronDown
                className={`size-4 shrink-0 transition-transform ${isMapOpen ? "rotate-180" : ""}`}
                aria-hidden="true"
              />
            </summary>
            {isMapOpen && (
              <div className="border-t border-border-default p-4">
                <NearbyStoresBrowser
                  showSkeleton={false}
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
                  searchKeyword={shopSearchKeyword}
                  onSearchChange={setShopSearchKeyword}
                  price={filters.price.join(",") || undefined}
                  category={filters.category.length > 0 ? filters.category.join(",") : undefined}
                  distance={filters.distance.length > 0 ? filters.distance.join(",") : undefined}
                  selectedShop={selectedShop}
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
        <div className="grid items-start gap-5 lg:grid-cols-[16rem_minmax(0,1fr)]">
          <div
            id="home-filters"
            className={isMobileFilterOpen ? "min-w-0" : "hidden min-w-0 lg:block"}
          >
            <FilterSidebar
              showSkeleton={false}
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
              isCategoryMismatch={isCategoryMismatch}
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
                showSkeleton={false}
                deals={deals}
                isLoading={isLoading}
                emptyMessage={serverMessage ?? undefined}
                onRefresh={refreshDeals}
                hasOlder={hasOlder}
                isLoadingOlder={isLoadingOlder}
                onLoadOlder={loadOlderDeals}
                searchKeyword={searchKeyword}
                onSearchChange={setSearchKeyword}
                onClearSearch={clearSearch}
                selectedCategories={filters.category}
                onClearCategory={() => setFilters((prev) => ({ ...prev, category: [] }))}
                locationAddress={locationAddress || locationLabel}
                locationMode={locationMode}
              />
            )}
          </div>
        </div>
      </div>

      {/* Floating Real-time New Deal Notifier Alert */}
      <NewDealAlertToast
        newDealsCount={newDealsCount}
        onViewNewDeals={() => {
          applyNewDeals();
          setTimeout(() => {
            const heading = document.getElementById("flash-deals-heading");
            if (heading) {
              const targetPos = heading.getBoundingClientRect().top + window.scrollY - 80;
              window.scrollTo({ top: Math.max(0, targetPos), behavior: "smooth" });
            }
          }, 50);
        }}
      />

      {/* Floating Scroll to Top Button */}
      <ScrollToTopButton />
    </div>
  );
}

function HomeStat({ value, label }: { value: string; label: string }) {
  return (
    <div className="min-w-0 flex-1 px-3 first:pl-0 last:pr-0 [&+div]:border-l [&+div]:border-slate-900/10">
      <strong className="block text-lg font-extrabold tracking-tight text-primary-700">
        {value}
      </strong>
      <span className="block text-[11px] leading-4 text-slate-600">{label}</span>
    </div>
  );
}

function HomeFoodVisual() {
  return (
    <div className="relative flex h-full min-h-[11.25rem] items-center justify-center sm:h-[clamp(15rem,42vh,24rem)]">
      <div className="absolute aspect-square h-[92%] max-h-[24rem] rounded-full border-[10px] border-white/60 bg-[radial-gradient(circle_at_35%_30%,#fff,#d9f1e3_60%,#bfe5cf)] shadow-[0_28px_60px_rgba(20,128,79,.24)] sm:border-[14px]" />
      <span
        className="relative z-10 select-none text-[clamp(3.25rem,10vh,6.5rem)] leading-none drop-shadow-[0_14px_14px_rgba(16,33,26,.22)] [--sway:6deg] animate-[homeSway_5.5s_ease-in-out_infinite] motion-reduce:animate-none"
        aria-hidden="true"
      >
        🍗
      </span>
      <span
        className="absolute left-[14%] top-[8%] select-none text-[clamp(2.5rem,8vh,5rem)] leading-none drop-shadow-[0_14px_14px_rgba(16,33,26,.22)] [--sway:9deg] animate-[homeSway_4.6s_ease-in-out_-1s_infinite] motion-reduce:animate-none"
        aria-hidden="true"
      >
        🥖
      </span>
      <span
        className="absolute right-[14%] top-[8%] select-none text-[clamp(2.6rem,8.5vh,5.25rem)] leading-none drop-shadow-[0_14px_14px_rgba(16,33,26,.22)] [--sway:8deg] animate-[homeSway_5s_ease-in-out_-2s_infinite] motion-reduce:animate-none"
        aria-hidden="true"
      >
        🍛
      </span>
      <span
        className="absolute left-[2%] top-1/2 -translate-y-1/2 select-none text-[clamp(2.35rem,7.8vh,4.75rem)] leading-none drop-shadow-[0_14px_14px_rgba(16,33,26,.22)] [--sway:10deg] animate-[homeSway_4.8s_ease-in-out_-.5s_infinite] motion-reduce:animate-none"
        aria-hidden="true"
      >
        🍜
      </span>
      <span
        className="absolute right-[2%] top-1/2 -translate-y-1/2 select-none text-[clamp(2.5rem,8.5vh,5rem)] leading-none drop-shadow-[0_14px_14px_rgba(16,33,26,.22)] [--sway:7deg] animate-[homeSway_5.2s_ease-in-out_-3s_infinite] motion-reduce:animate-none"
        aria-hidden="true"
      >
        🧋
      </span>
      <span
        className="absolute bottom-[6%] left-[20%] select-none text-[clamp(2.2rem,7vh,4.5rem)] leading-none drop-shadow-[0_14px_14px_rgba(16,33,26,.22)] [--sway:9deg] animate-[homeSway_4.4s_ease-in-out_-1.6s_infinite] motion-reduce:animate-none"
        aria-hidden="true"
      >
        🥐
      </span>
      <span
        className="absolute bottom-[6%] right-[20%] select-none text-[clamp(2.35rem,7.5vh,4.75rem)] leading-none drop-shadow-[0_14px_14px_rgba(16,33,26,.22)] [--sway:7deg] animate-[homeSway_5.2s_ease-in-out_-2.2s_infinite] motion-reduce:animate-none"
        aria-hidden="true"
      >
        🍰
      </span>
      <style>{`
        @keyframes homeSway {
          0%, 100% { transform: rotate(calc(var(--sway) * -1)) translateY(0); }
          50% { transform: rotate(var(--sway)) translateY(-10px); }
        }
      `}</style>
    </div>
  );
}
