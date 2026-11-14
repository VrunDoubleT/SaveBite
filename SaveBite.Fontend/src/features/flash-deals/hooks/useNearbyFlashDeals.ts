import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  flashDealApi,
  type SaveSearchFilterPayload,
} from "@/features/flash-deals/api/flashDealApi";
import { categoryApi } from "@/features/categories/api/categoryApi";
import { useAuthStore } from "@/shared/stores/authStore";
import { useDebounce } from "@/shared/hooks/useDebounce";
import { useNotificationStore } from "@/features/notifications/stores/notificationStore";
import { shopFollowApi } from "@/features/shop-follows/api/shopFollowApi";
import { shopApi } from "@/features/shops/api/shopApi";
import { isDealViewed, removeDealFromViewed } from "@/features/flash-deals/utils/viewedDeals";
import { useFlashDealRealtime, type FlashDealRealtimePayload } from "./useFlashDealRealtime";
import type { DealFilterState, FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

export type LocationMode = "gps" | "saved_address" | "default";

// Module-level in-memory cache to maintain state across navigation
let cachedCoords: { latitude: number; longitude: number } | null = null;
let cachedLocationMode: LocationMode | null = null;
let cachedLocationLabel: string = "Location Not Set";
let cachedLocationAddress: string | null = null;
let cachedDeals: FlashDeal[] = [];
let cachedNextCursor: string | null = null;
let cachedHasOlder = false;
let cachedServerMessage: string | null = null;
let cachedCategories: string[] = [];

export function cleanAddressParts(parts: (string | null | undefined)[]): string {
  const cleaned: string[] = [];
  for (const part of parts) {
    if (!part) continue;
    const trimmed = part.trim();
    if (!trimmed) continue;

    const normalized = trimmed
      .toLowerCase()
      .replace(/^(thành phố|tp\.|tp|tỉnh|quận|huyện|phường|thị xã|tx\.)\s+/i, "")
      .trim();

    const isDuplicate = cleaned.some((existing) => {
      const existingNorm = existing
        .toLowerCase()
        .replace(/^(thành phố|tp\.|tp|tỉnh|quận|huyện|phường|thị xã|tx\.)\s+/i, "")
        .trim();
      return (
        existing.toLowerCase() === trimmed.toLowerCase() ||
        (normalized.length > 2 && existingNorm === normalized) ||
        (existing.toLowerCase().includes(trimmed.toLowerCase()) && trimmed.length > 3)
      );
    });

    if (!isDuplicate) {
      cleaned.push(trimmed);
    }
  }
  return cleaned.join(", ");
}

export function useNearbyFlashDeals() {
  const user = useAuthStore((s) => s.user);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [coords, setCoords] = useState<{ latitude: number; longitude: number } | null>(
    () => cachedCoords,
  );
  const [locationMode, setLocationMode] = useState<LocationMode | null>(() => cachedLocationMode);
  const [locationLabel, setLocationLabel] = useState<string>(() => cachedLocationLabel);
  const [locationAddress, setLocationAddress] = useState<string | null>(
    () => cachedLocationAddress,
  );
  const [locationVersion, setLocationVersion] = useState(0);
  const [gpsError, setGpsError] = useState<string | null>(null);
  const [isGpsLoading, setIsGpsLoading] = useState(false);
  const [isLocationReady, setIsLocationReady] = useState(() => cachedCoords !== null);

  // Refresh authenticated user on mount to ensure default address & profile are up to date
  useEffect(() => {
    if (isAuthenticated) {
      void useAuthStore.getState().refreshUser();
    }
  }, [isAuthenticated]);

  const [radiusInKm, setRadiusInKm] = useState(5);
  const [deals, setDeals] = useState<FlashDeal[]>(() => cachedDeals);
  const [serverMessage, setServerMessage] = useState<string | null>(() => cachedServerMessage);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingOlder, setIsLoadingOlder] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Keyset Cursor state
  const [nextCursor, setNextCursor] = useState<string | null>(() => cachedNextCursor);
  const [hasOlder, setHasOlder] = useState(() => cachedHasOlder);
  const [newDealsCount, setNewDealsCount] = useState(0);

  const [categories, setCategories] = useState<string[]>(() => cachedCategories);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);

  // Reference guards
  const dealsRef = useRef(deals);
  const newDealIdsRef = useRef<Set<string>>(new Set());
  const notifiedDealsRef = useRef<Set<string>>(new Set());
  const followedShopIdsRef = useRef<Set<string>>(new Set());
  useEffect(() => {
    dealsRef.current = deals;
  }, [deals]);

  // Synchronize followed shops cache when user is authenticated
  useEffect(() => {
    if (!isAuthenticated) {
      followedShopIdsRef.current.clear();
      return;
    }
    let isMounted = true;
    shopFollowApi
      .list(1, 100)
      .then((data) => {
        if (isMounted && data?.items) {
          followedShopIdsRef.current = new Set(
            data.items
              .filter((item) => item.notificationStatus !== "Disabled")
              .map((item) => item.shopId.toLowerCase()),
          );
        }
      })
      .catch(() => {});

    return () => {
      isMounted = false;
    };
  }, [isAuthenticated]);

  const isLoadingOlderRef = useRef(false);
  const nextCursorRef = useRef(nextCursor);
  useEffect(() => {
    nextCursorRef.current = nextCursor;
  }, [nextCursor]);

  // Sync to module-level cache
  useEffect(() => {
    cachedCoords = coords;
    cachedLocationMode = locationMode;
    cachedLocationLabel = locationLabel;
    cachedLocationAddress = locationAddress;
    cachedDeals = deals;
    cachedNextCursor = nextCursor;
    cachedHasOlder = hasOlder;
    cachedServerMessage = serverMessage;
    cachedCategories = categories;
  }, [
    coords,
    locationMode,
    locationLabel,
    locationAddress,
    deals,
    nextCursor,
    hasOlder,
    serverMessage,
    categories,
  ]);

  const [selectedShop, setSelectedShop] = useState<string | null>(null);
  const [searchKeyword, setSearchKeyword] = useState<string>("");
  const debouncedKeyword = useDebounce(searchKeyword.trim(), 350);

  const [filters, setFilters] = useState<DealFilterState>({
    distance: [],
    price: [],
    category: [],
  });

  // Sync active search filter to Redis cache for backend recipient matching
  useEffect(() => {
    const payload: SaveSearchFilterPayload = {
      category: filters.category.length > 0 ? filters.category.join(",") : undefined,
      distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
      price: filters.price.length > 0 ? filters.price.join(",") : undefined,
      shopName: selectedShop ?? undefined,
      keyword: debouncedKeyword || undefined,
      radiusInKm,
    };
    flashDealApi.saveSearchFilter(payload).catch(() => {});
  }, [filters, selectedShop, debouncedKeyword, radiusInKm]);

  // Load active categories directly from database
  useEffect(() => {
    let isMounted = true;
    categoryApi
      .getCategories()
      .then((items) => {
        if (isMounted) {
          if (items.length > 0) {
            setCategories(items.map((c) => c.name));
          }
          setIsLoadingCategories(false);
        }
      })
      .catch(() => {
        if (isMounted) {
          setIsLoadingCategories(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  // Sync GPS to backend
  const syncGpsToBackend = useCallback(
    (lat: number, lon: number, accuracy?: number) => {
      if (isAuthenticated) {
        flashDealApi.updateGps({ latitude: lat, longitude: lon, accuracy }).catch(() => {});
      }
    },
    [isAuthenticated],
  );

  // Switch to saved default address
  const selectSavedAddress = useCallback(() => {
    if (user?.defaultAddress) {
      const newCoords = {
        latitude: user.defaultAddress.latitude,
        longitude: user.defaultAddress.longitude,
      };
      cachedCoords = newCoords;
      cachedLocationMode = "saved_address";
      const label = user.defaultAddress.label
        ? `${user.defaultAddress.label} - ${user.defaultAddress.addressLine}`
        : user.defaultAddress.addressLine;
      cachedLocationLabel = label;
      const addrToDisplay =
        cleanAddressParts([
          user.defaultAddress.addressLine,
          user.defaultAddress.ward,
          user.defaultAddress.district,
          user.defaultAddress.city,
        ]) || user.defaultAddress.addressLine;
      cachedLocationAddress = addrToDisplay;

      setCoords(newCoords);
      setLocationMode("saved_address");
      setLocationLabel(label);
      setLocationAddress(addrToDisplay);
      setGpsError(null);
      setIsLocationReady(true);
      setLocationVersion((v) => v + 1);
    }
  }, [user]);

  // Request browser GPS
  const requestGpsLocation = useCallback(() => {
    if (!("geolocation" in navigator)) {
      setGpsError("Geolocation is not supported by your browser.");
      return;
    }

    setIsGpsLoading(true);
    setGpsError(null);

    const onGeoSuccess = (position: GeolocationPosition) => {
      const newCoords = {
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
      };
      cachedCoords = newCoords;
      cachedLocationMode = "gps";
      cachedLocationLabel = "Current GPS Location";

      setCoords(newCoords);
      setLocationMode("gps");
      setLocationLabel("Current GPS Location");
      setIsGpsLoading(false);
      setIsLocationReady(true);
      setGpsError(null);
      setLocationVersion((v) => v + 1);

      syncGpsToBackend(newCoords.latitude, newCoords.longitude, position.coords.accuracy);
    };

    const onGeoError = (err: GeolocationPositionError) => {
      if (err.code !== 1) {
        navigator.geolocation.getCurrentPosition(
          onGeoSuccess,
          () => {
            setIsGpsLoading(false);
            if (user?.defaultAddress) {
              selectSavedAddress();
            } else {
              setIsLocationReady(false);
              setGpsError("Unable to retrieve GPS coordinates automatically.");
            }
          },
          { timeout: 8000, enableHighAccuracy: false, maximumAge: 300000 },
        );
        return;
      }

      setIsGpsLoading(false);
      if (user?.defaultAddress) {
        selectSavedAddress();
      } else {
        setIsLocationReady(false);
        setGpsError("Location permission is blocked in your browser.");
      }
    };

    navigator.geolocation.getCurrentPosition(onGeoSuccess, onGeoError, {
      timeout: 6000,
      enableHighAccuracy: true,
      maximumAge: 60000,
    });
  }, [user, selectSavedAddress, syncGpsToBackend]);

  // Location selection starts only from an explicit user action. Previously selected
  // coordinates remain cached so returning to Home preserves the browsing location.

  // Reverse geocoding for GPS display
  useEffect(() => {
    if (locationMode !== "gps" || !coords) return;
    let isMounted = true;

    async function resolveAddress() {
      if (!coords) return;
      try {
        const res = await fetch(
          `https://nominatim.openstreetmap.org/reverse?format=json&lat=${coords.latitude}&lon=${coords.longitude}&zoom=18&addressdetails=1`,
          { headers: { "Accept-Language": "vi,en" } },
        );
        if (res.ok) {
          const data = await res.json();
          if (data.display_name && isMounted) {
            const chunks = data.display_name.split(", ").slice(0, 4);
            const cleaned = cleanAddressParts(chunks);
            if (cleaned && isMounted) {
              setLocationAddress(cleaned);
              return;
            }
          }
        }
      } catch (err) {
        console.debug("Reverse geocode failed", err);
      }

      if (isMounted) {
        setLocationAddress(`${coords.latitude.toFixed(4)}°N, ${coords.longitude.toFixed(4)}°E`);
      }
    }

    void resolveAddress();
    return () => {
      isMounted = false;
    };
  }, [coords, locationMode]);

  // 1. Initial Page Load - Keyset Cursor fresh session (Page 1)
  const fetchInitialDeals = useCallback(async () => {
    if (!coords) return;
    await Promise.resolve();
    if (dealsRef.current.length === 0) {
      setIsLoading(true);
    }
    setError(null);
    setNewDealsCount(0);
    try {
      const res = await flashDealApi.getNearbyDeals({
        latitude: coords.latitude,
        longitude: coords.longitude,
        radiusInKm,
        locationMode: locationMode ?? undefined,
        limit: 10,
        category: filters.category.length > 0 ? filters.category.join(",") : undefined,
        distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
        price: filters.price.length > 0 ? filters.price.join(",") : undefined,
        shopName: selectedShop ?? undefined,
        keyword: debouncedKeyword ? debouncedKeyword : undefined,
      });

      const uniqueDeals = Array.from(
        new Map(
          res.deals.map((deal) => {
            const viewed = isDealViewed(deal.id);
            const isNewlyArrived = !viewed && newDealIdsRef.current.has(deal.id.toLowerCase());
            return [
              deal.id,
              { ...deal, isNew: isNewlyArrived || (!viewed && Boolean(deal.isNew)) },
            ];
          }),
        ).values(),
      );
      setDeals(uniqueDeals);
      setNextCursor(res.nextCursor ?? null);
      setHasOlder(res.hasMore ?? res.hasOlder);
      setServerMessage(res.message ?? null);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to load flash deals.";
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  }, [coords, radiusInKm, filters, selectedShop, debouncedKeyword, locationMode]);

  // 2. Keyset Cursor: Load next page (Older items)
  const loadOlderDeals = useCallback(async () => {
    const currentCursor = nextCursorRef.current;
    if (!coords || !currentCursor || isLoadingOlderRef.current) return;
    isLoadingOlderRef.current = true;
    setIsLoadingOlder(true);
    try {
      const res = await flashDealApi.getNearbyDeals({
        latitude: coords.latitude,
        longitude: coords.longitude,
        radiusInKm,
        cursor: currentCursor,
        locationMode: locationMode ?? undefined,
        limit: 10,
        category: filters.category.length > 0 ? filters.category.join(",") : undefined,
        distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
        price: filters.price.length > 0 ? filters.price.join(",") : undefined,
        shopName: selectedShop ?? undefined,
        keyword: debouncedKeyword ? debouncedKeyword : undefined,
      });

      if (res.deals.length > 0) {
        setDeals((prev) => {
          const existingIds = new Set(prev.map((d) => d.id));
          const newUnique = res.deals.filter((d) => !existingIds.has(d.id));
          return [...prev, ...newUnique];
        });
      }

      setNextCursor(res.nextCursor ?? null);
      setHasOlder(res.hasMore ?? res.hasOlder);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to load older deals.";
      if (msg.includes("CURSOR_LOCATION_CHANGED")) {
        setError("Your location context has changed. Refreshing deal list...");
        void fetchInitialDeals();
      } else {
        setError(msg);
      }
    } finally {
      isLoadingOlderRef.current = false;
      setIsLoadingOlder(false);
    }
  }, [
    coords,
    radiusInKm,
    filters,
    selectedShop,
    debouncedKeyword,
    locationMode,
    fetchInitialDeals,
  ]);

  // A targeted feed query applies exactly the same server rules as the Home list.
  const realtimeContext = useMemo(
    () => ({
      coords,
      radiusInKm,
      locationMode,
      filters,
      selectedShop,
      keyword: debouncedKeyword,
      isLocationReady,
      userId: user?.id,
    }),
    [
      coords,
      radiusInKm,
      locationMode,
      filters,
      selectedShop,
      debouncedKeyword,
      isLocationReady,
      user?.id,
    ],
  );
  const realtimeContextRef = useRef(realtimeContext);
  const realtimeVersions = useRef(new Map<string, number>());
  useEffect(() => {
    realtimeContextRef.current = realtimeContext;
  }, [realtimeContext]);
  useEffect(
    () => () => {
      realtimeContextRef.current = { ...realtimeContextRef.current, isLocationReady: false };
    },
    [],
  );

  const handleRealtimeDealEvent = useCallback(
    async (eventData?: FlashDealRealtimePayload) => {
      if (!eventData) return;

      const rawAction = eventData.action ?? eventData.Action;
      const action = rawAction ? String(rawAction).toLowerCase() : undefined;

      const rawStatus = eventData.status ?? eventData.Status;
      const status = rawStatus ? String(rawStatus).toLowerCase() : undefined;

      const rawDealId = eventData.dealId ?? eventData.DealId;
      const dealId = rawDealId ? String(rawDealId).toLowerCase() : undefined;

      const version = dealId ? (realtimeVersions.current.get(dealId) ?? 0) + 1 : 0;
      if (dealId) realtimeVersions.current.set(dealId, version);

      // 1. Hide / Close / Cancel: Shop disables deal -> Drop immediately from customer view
      if (
        action === "hidden" ||
        status === "hidden" ||
        action === "closed" ||
        status === "closed" ||
        action === "cancelled" ||
        status === "cancelled"
      ) {
        if (dealId) {
          newDealIdsRef.current.delete(dealId);
          notifiedDealsRef.current.delete(dealId);
          removeDealFromViewed(dealId);
          setDeals((prev) => prev.filter((d) => d.id.toLowerCase() !== dealId));
          setNewDealsCount((c) => Math.max(0, c - 1));
        }
        return;
      }

      // 2. Open / Published / Reopen: Shop enables deal
      if (
        action === "opened" ||
        action === "published" ||
        action === "reopened" ||
        status === "onsale" ||
        status === "open" ||
        status === "pending"
      ) {
        if (dealId) {
          removeDealFromViewed(dealId);
          try {
            const context = realtimeContextRef.current;
            if (!context.isLocationReady || !context.coords) return;
            const isCurrent = () =>
              realtimeContextRef.current === context &&
              realtimeVersions.current.get(dealId) === version;
            const response = await flashDealApi.getNearbyDeals({
              dealId,
              latitude: context.coords.latitude,
              longitude: context.coords.longitude,
              radiusInKm: context.radiusInKm,
              locationMode: context.locationMode ?? undefined,
              limit: 1,
              category: context.filters.category.join(",") || undefined,
              distance: context.filters.distance.join(",") || undefined,
              price: context.filters.price.join(",") || undefined,
              shopName: context.selectedShop ?? undefined,
              keyword: context.keyword || undefined,
            });
            if (!isCurrent()) return;
            const freshDeal = response.deals.find((deal) => deal.id.toLowerCase() === dealId);
            if (!freshDeal) {
              newDealIdsRef.current.delete(dealId);
              setDeals((prev) => prev.filter((deal) => deal.id.toLowerCase() !== dealId));
              return;
            }
            {
              // 2.5. Check if customer follows this shop (with deal notifications enabled)
              let isFollowed = false;
              const shopIdToCheck = freshDeal.shopId || eventData.shopId || eventData.ShopId;
              if (isAuthenticated && shopIdToCheck) {
                try {
                  const profile = await shopApi.getShopProfile(shopIdToCheck);
                  if (
                    profile?.currentUserFollow &&
                    profile.currentUserFollow.notificationStatus !== "Disabled"
                  ) {
                    isFollowed = true;
                    followedShopIdsRef.current.add(shopIdToCheck.toLowerCase());
                  }
                } catch {
                  isFollowed = followedShopIdsRef.current.has(shopIdToCheck.toLowerCase());
                }
              }

              if (!isCurrent()) return;

              // 2.6. All filters matched -> Display deal with NEW badge
              newDealIdsRef.current.add(dealId);
              const markedDeal: FlashDeal = {
                ...freshDeal,
                isNew: true,
              };
              setDeals((prev) => {
                const rest = prev.filter((d) => d.id.toLowerCase() !== dealId);
                return [markedDeal, ...rest];
              });

              // 2.7. Trigger notifications (once per deal opening session)
              if (!notifiedDealsRef.current.has(dealId)) {
                notifiedDealsRef.current.add(dealId);

                // Bottom alert toast is ALWAYS shown when deal matches filter
                setNewDealsCount((c) => c + 1);

                // Notification Bell is ONLY triggered if customer follows this shop!
                if (isFollowed) {
                  useNotificationStore.getState().onNotificationReceived({
                    notification: {
                      id: `deal-${freshDeal.id}`,
                      type: 17,
                      title: `New Flash Deal from ${freshDeal.shopName}!`,
                      body: `'${freshDeal.productName}' is now on sale. Order now before it runs out!`,
                      actionUrl: `/flash-deals/${freshDeal.id}`,
                      isRead: false,
                      createdAt: new Date().toISOString(),
                    },
                    unreadCount: useNotificationStore.getState().unreadCount + 1,
                  });
                }
              }
              return;
            }
          } catch {
            // Leave the feed unchanged when the server cannot verify this deal.
          }
        }
        // Failed validation never inserts or notifies an unverified deal.
      }
    },
    [isAuthenticated],
  );

  useFlashDealRealtime("all", undefined, handleRealtimeDealEvent);

  // Apply new deals from banner click
  const applyNewDeals = useCallback(() => {
    setNewDealsCount(0);
    setNextCursor(null);
    void fetchInitialDeals();
  }, [fetchInitialDeals]);

  // Trigger on init or location/radius change
  useEffect(() => {
    if (!isLocationReady) return;
    const timer = setTimeout(() => {
      void fetchInitialDeals();
    }, 0);
    return () => clearTimeout(timer);
  }, [isLocationReady, fetchInitialDeals]);

  return {
    deals,
    rawDeals: deals,
    categoryCounts: useMemo(() => {
      const map: Record<string, number> = {};
      for (const d of deals) {
        if (d.categoryName) {
          map[d.categoryName] = (map[d.categoryName] || 0) + 1;
        }
      }
      return map;
    }, [deals]),
    serverMessage,
    isLoading,
    isLoadingOlder,
    isCheckingNewer: false,
    error,
    coords,
    isLocationReady,
    radiusInKm,
    setRadiusInKm,
    selectedShop,
    setSelectedShop,
    searchKeyword,
    setSearchKeyword,
    clearSearch: () => setSearchKeyword(""),
    filters,
    setFilters,
    refreshDeals: fetchInitialDeals,
    cursor1: null,
    cursor2: null,
    hasOlder,
    newDealsCount,
    pendingNewDeals: [],
    loadOlderDeals,
    checkNewerDeals: () => {},
    applyNewDeals,
    categories,
    isLoadingCategories,
    locationMode,
    locationLabel,
    locationAddress,
    gpsError,
    isGpsLoading,
    requestGpsLocation,
    selectSavedAddress,
    selectDefaultLocation: selectSavedAddress,
    locationVersion,
    userDefaultAddress: user?.defaultAddress,
  };
}
