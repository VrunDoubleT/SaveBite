import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { List, Map, MapPin, Search, Star, Store } from "lucide-react";
import {
  Badge,
  Button,
  EmptyState,
  ErrorState,
  MediaImage,
  Pagination,
  Skeleton,
} from "@/shared/components/ui";
import { getApiErrorMessage } from "@/shared/api";
import type { NearbyShop } from "../types/shop.types";
import { shopApi } from "../api/shopApi";
import { useHomePreferencesStore } from "../stores/homePreferencesStore";
import { NearbyStoresMapSection, type NearbyStoresMapSectionProps } from "./NearbyStoresMapSection";

export function NearbyStoresBrowser(props: NearbyStoresMapSectionProps) {
  const [focusRequest, setFocusRequest] = useState<{ shop: NearbyShop; location: string } | null>(
    null,
  );
  const locationKey = `${props.latitude}:${props.longitude}:${props.locationMode}:${props.locationVersion}:${props.radiusInKm}`;
  const view = useHomePreferencesStore((state) => state.storesView);
  const setView = useHomePreferencesStore((state) => state.setStoresView);
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-section font-semibold text-text-primary">Stores near you</h2>
        <div
          role="group"
          aria-label="Store view"
          className="inline-flex overflow-hidden rounded-md border border-border-default bg-bg-surface"
        >
          <button
            type="button"
            className={`inline-flex h-10 w-24 items-center justify-center gap-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-primary-700 ${view === "map" ? "bg-primary-700 text-text-inverse" : "text-text-secondary hover:bg-neutral-100"}`}
            aria-pressed={view === "map"}
            onClick={() => setView("map")}
          >
            <Map className="size-4" aria-hidden="true" />
            Map
          </button>
          <button
            type="button"
            className={`inline-flex h-10 w-24 items-center justify-center gap-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-primary-700 ${view === "list" ? "bg-primary-700 text-text-inverse" : "text-text-secondary hover:bg-neutral-100"}`}
            aria-pressed={view === "list"}
            onClick={() => setView("list")}
          >
            <List className="size-4" aria-hidden="true" />
            List
          </button>
        </div>
      </div>
      {view === "map" ? (
        <NearbyStoresMapSection
          key={locationKey}
          {...props}
          focusShop={focusRequest?.location === locationKey ? focusRequest.shop : undefined}
        />
      ) : (
        <NearbyStoresList
          key={`${props.latitude}:${props.longitude}:${props.locationMode}:${props.locationVersion}:${props.radiusInKm}`}
          {...props}
          onViewOnMap={(shop) => {
            setFocusRequest({ shop, location: locationKey });
            setView("map");
          }}
        />
      )}
    </div>
  );
}
function NearbyStoresList({
  latitude,
  longitude,
  radiusInKm = 10,
  locationMode,
  isLocationReady,
  onViewOnMap,
}: NearbyStoresMapSectionProps & { onViewOnMap: (shop: NearbyShop) => void }) {
  const [keyword, setKeyword] = useState("");
  const [query, setQuery] = useState("");
  const [onlyDeals, setOnlyDeals] = useState(false);
  const [page, setPage] = useState(1);
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState<Awaited<ReturnType<typeof shopApi.getNearbyShops>> | null>(
    null,
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isLocationReady) return;
    const controller = new AbortController();
    shopApi
      .getNearbyShops(
        {
          latitude: locationMode === "saved_address" ? undefined : (latitude ?? undefined),
          longitude: locationMode === "saved_address" ? undefined : (longitude ?? undefined),
          radiusInKm,
          keyword: query || undefined,
          onlyWithFlashDeals: onlyDeals,
          page,
          pageSize: 8,
        },
        controller.signal,
      )
      .then((data) => {
        if (!controller.signal.aborted) {
          setResult(data);
          setError(null);
          setLoading(false);
        }
      })
      .catch((cause: unknown) => {
        if (!controller.signal.aborted) {
          setError(getApiErrorMessage(cause));
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [
    latitude,
    longitude,
    radiusInKm,
    locationMode,
    isLocationReady,
    query,
    onlyDeals,
    page,
    attempt,
  ]);
  const waiting = loading;
  return (
    <div className="space-y-4">
      <form
        className="flex flex-col gap-3 sm:flex-row sm:items-center"
        onSubmit={(event) => {
          event.preventDefault();
          setLoading(true);
          setQuery(keyword.trim());
          setPage(1);
          setAttempt((value) => value + 1);
        }}
      >
        <label className="relative flex-1">
          <span className="sr-only">Search nearby stores by name</span>
          <Search
            className="pointer-events-none absolute left-3 top-3 size-4 text-text-muted"
            aria-hidden="true"
          />
          <input
            type="search"
            value={keyword}
            maxLength={120}
            placeholder="Search stores by name…"
            onChange={(event) => {
              setKeyword(event.target.value);
            }}
            onBlur={(event) => {
              if (!event.currentTarget.value.trim() && query) {
                setKeyword("");
                setQuery("");
                setPage(1);
                setLoading(true);
              }
            }}
            className="min-h-11 w-full rounded-md border border-border-default bg-bg-surface pl-10 pr-3 text-body text-text-primary focus-visible:outline-2 focus-visible:outline-primary-700"
          />
        </label>
        <Button type="submit" className="min-h-11" loading={loading}>
          Search
        </Button>
        <label className="flex min-h-11 cursor-pointer items-center gap-2 text-body text-text-secondary">
          <input
            type="checkbox"
            checked={onlyDeals}
            onChange={(event) => {
              setOnlyDeals(event.target.checked);
              setPage(1);
              setLoading(true);
            }}
            className="size-4 accent-primary-700"
          />
          With flash deals
        </label>
      </form>
      {waiting ? (
        <div role="status" aria-label="Loading nearby stores" className="grid gap-3 md:grid-cols-2">
          {[1, 2, 3, 4].map((id) => (
            <Skeleton key={id} className="h-28 rounded-lg" />
          ))}
        </div>
      ) : error ? (
        <ErrorState
          title="Couldn't load stores"
          description={error}
          onRetry={() => {
            setLoading(true);
            setAttempt((value) => value + 1);
          }}
        />
      ) : !result?.shops.length ? (
        <EmptyState
          icon={Store}
          title="No matching stores nearby"
          description="Try another store name or turn off the flash deals filter."
        />
      ) : (
        <>
          <p className="text-caption text-text-secondary">
            {result.totalItems} stores within {Math.min(radiusInKm, 10)} km · Nearest first
          </p>
          <ul className="grid gap-3 md:grid-cols-2">
            {result.shops.map((shop) => (
              <li
                key={shop.id}
                className="flex h-full min-w-0 flex-col rounded-lg border border-border-default bg-bg-surface"
              >
                <div className="flex min-w-0 flex-1 items-center gap-3 p-4">
                  <MediaImage
                    src={shop.logoUrl ?? undefined}
                    alt=""
                    fallbackIcon={Store}
                    showFallbackLabel={false}
                    className="size-14 shrink-0 rounded-md"
                  />
                  <div className="min-w-0 flex-1 space-y-1">
                    <div className="flex items-start justify-between gap-2">
                      <h3 className="min-w-0 font-semibold text-text-primary [overflow-wrap:anywhere]">
                        <Link
                          to={`/shops/${shop.id}`}
                          className="rounded-sm hover:text-primary-700 hover:underline focus-visible:outline-2 focus-visible:outline-primary-700"
                        >
                          {shop.name}
                        </Link>
                      </h3>
                      <button
                        type="button"
                        onClick={() => onViewOnMap(shop)}
                        aria-label={`View ${shop.name} on map`}
                        title="View on map"
                        className="flex size-8 shrink-0 items-center justify-center rounded-md text-primary-700 transition-colors hover:bg-primary-50 focus-visible:outline-2 focus-visible:outline-primary-700"
                      >
                        <MapPin className="size-4" aria-hidden="true" />
                      </button>
                    </div>
                    <p className="flex items-start gap-1 text-caption text-text-secondary">
                      <MapPin className="mt-0.5 size-3.5 shrink-0" aria-hidden="true" />
                      <span className="line-clamp-2">{shop.address}</span>
                    </p>
                    <div className="flex flex-wrap items-center gap-2 text-caption text-text-secondary">
                      <span>{shop.distanceInKm.toFixed(1)} km</span>
                      <span className="inline-flex items-center gap-1">
                        <Star className="size-3.5 text-accent-700" aria-hidden="true" />
                        {shop.totalReviews ? shop.averageRating.toFixed(1) : "No reviews"}
                      </span>
                      <Badge tone={shop.isOpen ? "primary" : "neutral"}>
                        {shop.isOpen ? "Open" : "Closed"}
                      </Badge>
                      <span>{shop.flashDealCount ?? 0} deals</span>
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
          <Pagination
            currentPage={page}
            totalPages={result.totalPages}
            totalItems={result.totalItems}
            pageSize={8}
            itemLabel="stores"
            hideOnSinglePage
            onPageChange={(next) => {
              setPage(next);
              setLoading(true);
            }}
          />
        </>
      )}
    </div>
  );
}
