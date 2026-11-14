import { useDebounce } from "@/shared/hooks/useDebounce";
import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { List, Map, MapPin, Search, Star, Store, X } from "lucide-react";
import {
  Badge,
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
  const locationKey = `${props.latitude}:${props.longitude}:${props.locationMode}:${props.locationVersion}`;
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
          key={locationKey}
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
  showSkeleton = true,
  latitude,
  longitude,
  radiusInKm = 5,
  locationMode,
  isLocationReady,
  onViewOnMap,
  searchKeyword,
  onSearchChange,
  category,
  price,
  distance,
  selectedShop: propSelectedShop,
}: NearbyStoresMapSectionProps & { onViewOnMap: (shop: NearbyShop) => void }) {
  const [localKeyword, setLocalKeyword] = useState("");
  const keyword = onSearchChange ? (searchKeyword ?? "") : localKeyword;
  const setKeyword = onSearchChange ?? setLocalKeyword;
  const [query, setQuery] = useState("");
  const [onlyDeals, setOnlyDeals] = useState(false);
  const [page, setPage] = useState(1);
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState<Awaited<ReturnType<typeof shopApi.getNearbyShops>> | null>(
    null,
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const debouncedKeyword = useDebounce(keyword.trim(), 700);

  const appliedKeyword = useRef<string | null>(null);

  // Apply a search only after the user pauses typing.
  useEffect(() => {
    if (appliedKeyword.current === debouncedKeyword) return;
    appliedKeyword.current = debouncedKeyword;
    const next = debouncedKeyword;
    if (next !== query) {
      setQuery(next);
      setPage(1);
      setLoading(true);
    }
  }, [debouncedKeyword, query]);

  // Real-time flash deal update listener
  useEffect(() => {
    const handleFlashDealChanged = () => {
      setAttempt((v) => v + 1);
    };
    window.addEventListener("flashdeal:changed", handleFlashDealChanged);
    return () => window.removeEventListener("flashdeal:changed", handleFlashDealChanged);
  }, []);

  useEffect(() => {
    if (!isLocationReady) return;
    const controller = new AbortController();
    const effectiveKw = query.trim() || undefined;
    shopApi
      .getNearbyShops(
        {
          latitude: locationMode === "saved_address" ? undefined : (latitude ?? undefined),
          longitude: locationMode === "saved_address" ? undefined : (longitude ?? undefined),
          radiusInKm: Math.min(radiusInKm ?? 5, 20),
          keyword: effectiveKw,
          category: effectiveKw ? undefined : category || undefined,
          price: effectiveKw ? undefined : price || undefined,
          distance: distance || undefined,
          shopName: effectiveKw ? undefined : propSelectedShop || undefined,
          onlyWithFlashDeals: onlyDeals,
          page,
          pageSize: 10,
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
    category,
    price,
    distance,
    propSelectedShop,
    onlyDeals,
    page,
    attempt,
  ]);

  // Real-time update: remove suspended store from nearby stores list
  useEffect(() => {
    const handleShopStatus = (e: Event) => {
      const detail = (e as CustomEvent<{ shopId?: string; status?: string; isBlocked?: boolean }>)
        .detail;
      const rawShopId = detail?.shopId || (detail as any)?.ShopId;
      const rawStatus = detail?.status || (detail as any)?.Status;
      const isBlocked = detail?.isBlocked || (detail as any)?.IsBlocked;
      const blockedShopId = rawShopId?.toLowerCase();
      if (!blockedShopId) return;

      const isInactive = !rawStatus || rawStatus.toLowerCase() !== "active" || Boolean(isBlocked);
      if (isInactive) {
        setResult((prev) => {
          if (!prev) return null;
          const hasBlocked = prev.shops.some(
            (s) => ((s.id || (s as any).shopId) as string)?.toLowerCase() === blockedShopId,
          );
          if (!hasBlocked) return prev;
          const nextShops = prev.shops.filter(
            (s) => ((s.id || (s as any).shopId) as string)?.toLowerCase() !== blockedShopId,
          );
          return {
            ...prev,
            shops: nextShops,
            totalItems: Math.max(0, prev.totalItems - 1),
          };
        });
      } else {
        setAttempt((v) => v + 1);
      }
    };

    window.addEventListener("shop:status-changed", handleShopStatus);
    return () => window.removeEventListener("shop:status-changed", handleShopStatus);
  }, []);

  const waiting = loading;
  return (
    <div className="space-y-4">
      <form
        className="flex flex-col gap-3 sm:flex-row sm:items-center"
        onSubmit={(event) => {
          event.preventDefault();
          const next = keyword.trim();
          if (next !== query) {
            setQuery(next);
            setPage(1);
            setLoading(true);
          }
        }}
      >
        <label className="relative flex-1">
          <span className="sr-only">Search nearby stores by name</span>
          <Search
            className="pointer-events-none absolute left-3 top-3.5 size-4 text-text-muted"
            aria-hidden="true"
          />
          <input
            type="search"
            value={keyword}
            maxLength={120}
            placeholder="Search stores by name…"
            onChange={(event) => {
              setKeyword(event.target.value.normalize("NFC"));
            }}
            className="min-h-11 w-full rounded-md border border-border-default bg-bg-surface pl-10 pr-9 text-body text-text-primary focus-visible:outline-2 focus-visible:outline-primary-700"
          />
          {keyword && (
            <button
              type="button"
              onClick={() => {
                setKeyword("");
                setQuery("");
                setPage(1);
                setLoading(true);
              }}
              className="absolute right-2.5 top-2.5 p-1 text-text-muted hover:text-text-primary transition-colors"
              aria-label="Clear search"
              title="Clear search"
            >
              <X className="size-4" />
            </button>
          )}
        </label>
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
          {showSkeleton ? (
            [1, 2, 3, 4].map((id) => <Skeleton key={id} className="h-28 rounded-lg" />)
          ) : (
            <p className="text-sm text-text-secondary">Loading nearby stores…</p>
          )}
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
            {result.totalItems} stores within {Math.min(radiusInKm, 20)} km · Nearest first
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
            pageSize={10}
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
