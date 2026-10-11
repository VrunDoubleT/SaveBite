import { useCallback, useEffect, useMemo, useState } from "react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import { categoryApi } from "@/features/categories/api/categoryApi";
import { useAuthStore } from "@/shared/stores/authStore";
import { useDebounce } from "@/shared/hooks/useDebounce";
import type { DealFilterState, FlashDeal } from "@/features/flash-deals/types/flashDeal.types";


export type LocationMode = "gps" | "saved_address" | "default";

// Module-level in-memory cache to maintain state across navigation
let cachedCoords: { latitude: number; longitude: number } | null = null;
let cachedLocationMode: LocationMode | null = null;
let cachedLocationLabel: string = "Location Not Set";
let cachedLocationAddress: string | null = null;
let cachedDeals: FlashDeal[] = [];
let cachedCursor1: string | null = null;
let cachedCursor2: string | null = null;
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

  const [coords, setCoords] = useState<{ latitude: number; longitude: number } | null>(() => cachedCoords);
  const [locationMode, setLocationMode] = useState<LocationMode | null>(() => cachedLocationMode);
  const [locationLabel, setLocationLabel] = useState<string>(() => cachedLocationLabel);
  const [locationAddress, setLocationAddress] = useState<string | null>(() => cachedLocationAddress);
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

  const [radiusInKm, setRadiusInKm] = useState(10);
  const [deals, setDeals] = useState<FlashDeal[]>(() => cachedDeals);
  const [serverMessage, setServerMessage] = useState<string | null>(() => cachedServerMessage);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingOlder, setIsLoadingOlder] = useState(false);
  const [isCheckingNewer, setIsCheckingNewer] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Dual timestamp cursors (Cursor 1: oldest deal, Cursor 2: newest deal)
  const [cursor1, setCursor1] = useState<string | null>(() => cachedCursor1);
  const [cursor2, setCursor2] = useState<string | null>(() => cachedCursor2);
  const [hasOlder, setHasOlder] = useState(() => cachedHasOlder);
  const [newDealsCount, setNewDealsCount] = useState(0);
  const [pendingNewDeals, setPendingNewDeals] = useState<FlashDeal[]>([]);

  const [categories, setCategories] = useState<string[]>(() => cachedCategories);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);

  // Sync to module-level cache
  useEffect(() => {
    cachedCoords = coords;
    cachedLocationMode = locationMode;
    cachedLocationLabel = locationLabel;
    cachedLocationAddress = locationAddress;
    cachedDeals = deals;
    cachedCursor1 = cursor1;
    cachedCursor2 = cursor2;
    cachedHasOlder = hasOlder;
    cachedServerMessage = serverMessage;
    cachedCategories = categories;
  }, [coords, locationMode, locationLabel, locationAddress, deals, cursor1, cursor2, hasOlder, serverMessage, categories]);

  const [selectedShop, setSelectedShop] = useState<string | null>(null);
  const [searchKeyword, setSearchKeyword] = useState<string>("");
  const debouncedKeyword = useDebounce(searchKeyword.trim(), 350);

  const [filters, setFilters] = useState<DealFilterState>({
    distance: [],
    price: [],
    category: [],
  });

  // Load active categories directly from database
  useEffect(() => {
    let isMounted = true;
    setIsLoadingCategories(true);
    categoryApi
      .getCategories()
      .then((items) => {
        if (isMounted && items.length > 0) {
          setCategories(items.map((c) => c.name));
        }
      })
      .catch(() => {
        // Fallback gracefully if database is unreachable
      })
      .finally(() => {
        if (isMounted) {
          setIsLoadingCategories(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  // Request browser GPS on user action (or automatically when permission is already granted)
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
    };

    const onGeoError = (err: GeolocationPositionError) => {
      // If high accuracy failed (e.g. timeout or position unavailable), retry once with network/standard accuracy
      if (err.code !== 1) {
        navigator.geolocation.getCurrentPosition(
          onGeoSuccess,
          () => {
            setIsGpsLoading(false);
            setIsLocationReady(false);
            setGpsError("Unable to retrieve GPS coordinates automatically. Please check your device location settings or use your saved address.");
          },
          { timeout: 8000, enableHighAccuracy: false, maximumAge: 300000 },
        );
        return;
      }

      setIsGpsLoading(false);
      setIsLocationReady(false);
      if (err.code === 1) {
        setGpsError("Location permission is blocked in your browser. Please allow location access in your address bar.");
      } else {
        setGpsError("Unable to retrieve GPS coordinates automatically. Please check your device location settings or use your saved address.");
      }
    };

    navigator.geolocation.getCurrentPosition(onGeoSuccess, onGeoError, {
      timeout: 6000,
      enableHighAccuracy: true,
      maximumAge: 60000,
    });
  }, []);

  // Initialization: check GPS permission automatically if previously granted, or use saved address if logged in
  useEffect(() => {
    // If user has a saved default address, prioritize it and do not call browser GPS API
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
      setIsLocationReady(true);
      setLocationVersion((v) => v + 1);
      return;
    }

    // If coordinates exist from previous session in module cache, use immediately
    if (cachedCoords) {
      setIsLocationReady(true);
      return;
    }
  }, [user?.defaultAddress]);

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
  }, [user?.defaultAddress]);

  // Switch to default location (uses saved address if available, or requests GPS)
  const selectDefaultLocation = useCallback(() => {
    if (user?.defaultAddress) {
      selectSavedAddress();
    } else {
      requestGpsLocation();
    }
  }, [user?.defaultAddress, selectSavedAddress, requestGpsLocation]);

  // Automatically reverse-geocode physical address from GPS coordinates for display (ONLY FOR GPS)
  useEffect(() => {
    // If using saved default address, directly use the profile address and NEVER call any reverse geocoding API
    if (locationMode === "saved_address" || user?.defaultAddress) {
      if (user?.defaultAddress) {
        const addrToDisplay =
          cleanAddressParts([
            user.defaultAddress.addressLine,
            user.defaultAddress.ward,
            user.defaultAddress.district,
            user.defaultAddress.city,
          ]) || user.defaultAddress.addressLine;
        setLocationAddress(addrToDisplay);
      }
      return;
    }

    // Only GPS mode requires calling the reverse-geocoding API to resolve coordinates to a text address
    if (locationMode !== "gps" || !coords) {
      return;
    }

    let isMounted = true;

    async function resolveAddress() {
      if (!coords) return;
      try {
        const res = await fetch(
          `https://nominatim.openstreetmap.org/reverse?format=json&lat=${coords.latitude}&lon=${coords.longitude}&zoom=18&addressdetails=1`,
          { headers: { "Accept-Language": "vi,en" } }
        );
        if (res.ok) {
          const data = await res.json();
          const addr = data.address;
          if (addr) {
            const parts = [
              data.name && data.name !== addr.road && !data.name.startsWith(addr.road || "") ? data.name : null,
              addr.road,
              addr.suburb || addr.quarter || addr.neighbourhood,
              addr.city_district || addr.district || addr.city || addr.town,
              addr.city && addr.city !== addr.city_district && addr.city !== addr.district ? addr.city : null,
            ];
            const cleaned = cleanAddressParts(parts);
            if (cleaned && isMounted) {
              setLocationAddress(cleaned);
              return;
            }
          }
          if (data.display_name && isMounted) {
            const chunks = data.display_name.split(", ").slice(0, 4);
            const cleaned = cleanAddressParts(chunks);
            if (cleaned && isMounted) {
              setLocationAddress(cleaned);
              return;
            }
          }
        }
      } catch {
        // Fallback
      }

      try {
        const res2 = await fetch(
          `https://api.bigdatacloud.net/data/reverse-geocode-client?latitude=${coords.latitude}&longitude=${coords.longitude}&localityLanguage=vi`
        );
        if (res2.ok) {
          const data2 = await res2.json();
          const parts2 = [data2.locality, data2.city, data2.countryName];
          const cleaned2 = cleanAddressParts(parts2);
          if (cleaned2 && isMounted) {
            setLocationAddress(cleaned2);
            return;
          }
        }
      } catch {
        // Fallback
      }

      if (isMounted) {
        setLocationAddress(`${coords.latitude.toFixed(4)}°N, ${coords.longitude.toFixed(4)}°E`);
      }
    }

    void resolveAddress();

    return () => {
      isMounted = false;
    };
  }, [coords, locationMode, user?.defaultAddress]);

  // 2. Initial load - Query backend directly with Cursor Pagination and active filters
  const fetchInitialDeals = useCallback(async () => {
    if (!coords) return;
    // Only show Skeleton when there are no deals currently loaded
    if (deals.length === 0) {
      setIsLoading(true);
    }
    setError(null);
    setNewDealsCount(0);
    setPendingNewDeals([]);
    try {
      const isSavedAddress = locationMode === "saved_address";
      const res = await flashDealApi.getNearbyDeals({
        latitude: isSavedAddress ? undefined : coords.latitude,
        longitude: isSavedAddress ? undefined : coords.longitude,
        radiusInKm,
        mode: "Initial",
        limit: 9,
        category: filters.category.length > 0 ? filters.category.join(",") : undefined,
        distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
        price: filters.price.length > 0 ? filters.price.join(",") : undefined,
        shopName: selectedShop ?? undefined,
        keyword: debouncedKeyword ? debouncedKeyword : undefined,
      });

      // Deduplicate deals by ID
      const uniqueDeals = Array.from(
        new Map(res.deals.map((deal) => [deal.id, deal])).values(),
      );
      setDeals(uniqueDeals);
      setCursor1(res.cursor1 ?? null);
      setCursor2(res.cursor2 ?? null);
      setHasOlder(res.hasOlder);
      setServerMessage(res.message ?? null);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to load flash deals.";
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  }, [coords, radiusInKm, filters, selectedShop, deals.length, debouncedKeyword, locationMode]);

  // 3. DUAL-CURSOR: Pagination downward - combine both cursors in a single API call
  //
  // Workflow:
  //   - Pass cursor1 (current oldest timestamp) to fetch older deals
  //   - Pass cursor2 (current newest timestamp) to check if new deals have been published
  //     since the last fetch
  //
  // Backend returns: [deals newer than cursor2] ++ [deals older than cursor1 (up to limit)]
  //
  // Frontend merges the result:
  //   - New deals (first prependedCount items) -> prepend with "isNew: true" badge
  //   - Older deals -> append to bottom of list
  //   - Update cursor2 to latest timestamp received
  //   - Reset new deals notification badge
  const loadOlderDeals = useCallback(async () => {
    if (!coords || !cursor1 || isLoadingOlder) return;
    setIsLoadingOlder(true);
    try {
      const isSavedAddress = locationMode === "saved_address";
      const res = await flashDealApi.getNearbyDeals({
        latitude: isSavedAddress ? undefined : coords.latitude,
        longitude: isSavedAddress ? undefined : coords.longitude,
        radiusInKm,
        cursor1,          // Cursor 1: oldest boundary -> fetch older deals
        cursor2: cursor2 ?? undefined, // Cursor 2: newest boundary -> detect newer deals
        mode: "Older",
        limit: 9,
        category: filters.category.length > 0 ? filters.category.join(",") : undefined,
        distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
        price: filters.price.length > 0 ? filters.price.join(",") : undefined,
        shopName: selectedShop ?? undefined,
        keyword: debouncedKeyword ? debouncedKeyword : undefined,
      });

      if (res.deals.length > 0) {
        setDeals((prev) => {
          const existingIds = new Set(prev.map((d) => d.id));

          // Deals newer than cursor2 (first prependedCount items in res.deals)
          // Deals older than cursor1 (remaining items)
          const incomingNew = res.deals
            .slice(0, res.prependedCount)
            .filter((d) => !existingIds.has(d.id))
            .map((d) => ({ ...d, isNew: true }));
          const incomingOld = res.deals.slice(res.prependedCount).filter((d) => !existingIds.has(d.id));

          // Result: [existing deals] ++ [newly created deals with NEW badge] ++ [older deals]
          return [...prev, ...incomingNew, ...incomingOld];
        });

        // Update cursor1 if older deals were returned
        if (res.cursor1) {
          setCursor1(res.cursor1);
        }

        // Update cursor2 if backend detected new deals and merged them in
        if (res.prependedCount > 0 && res.cursor2) {
          setCursor2(res.cursor2);
          // Reset badge since new deals are now displayed in the list
          setNewDealsCount(0);
          setPendingNewDeals([]);
        }
      }

      setHasOlder(res.hasOlder);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to load older deals.";
      setError(msg);
    } finally {
      setIsLoadingOlder(false);
    }
  }, [coords, radiusInKm, cursor1, cursor2, isLoadingOlder, filters, selectedShop, debouncedKeyword, locationMode]);

  // 4. Cursor 2 - POLLING ONLY: Periodically check if new deals exist.
  //
  // Backend (mode=Newer) only returns newDealsCount, does not return deals, does not update cursor2.
  // Purpose: show "X new deals!" badge -> user knows to load new deals.
  // Cursor2 will only be updated when loadOlderDeals() is called.
  const checkNewerDeals = useCallback(async () => {
    if (!coords || !cursor2 || isCheckingNewer) return;
    setIsCheckingNewer(true);
    try {
      const isSavedAddress = locationMode === "saved_address";
      const res = await flashDealApi.getNearbyDeals({
        latitude: isSavedAddress ? undefined : coords.latitude,
        longitude: isSavedAddress ? undefined : coords.longitude,
        radiusInKm,
        cursor2,
        mode: "Newer",
      });

      if (res.hasNewer && res.newDealsCount > 0) {
        // Only update badge count, do NOT merge into list here
        setNewDealsCount(res.newDealsCount);
      }
    } catch {
      // Polling fails silently without interrupting UI
    } finally {
      setIsCheckingNewer(false);
    }
  }, [coords, radiusInKm, cursor2, isCheckingNewer, locationMode]);

  // 5. When user clicks "Update now" on new deals notification badge:
  //    Call loadOlderDeals() immediately -> backend prepends new deals.
  //    If no pages loaded yet, refresh entire list.
  const applyNewDeals = useCallback(() => {
    if (cursor1) {
      // Page 1 exists -> load next page with new deals prepended
      void loadOlderDeals();
    } else {
      // No pages yet -> refresh entire list
      void fetchInitialDeals();
    }
    setNewDealsCount(0);
    setPendingNewDeals([]);
  }, [cursor1, loadOlderDeals, fetchInitialDeals]);

  // Trigger on init or location/radius change
  useEffect(() => {
    if (!isLocationReady) return;
    void fetchInitialDeals();
  }, [isLocationReady, fetchInitialDeals]);

  // Periodically every 45s check for new deals using Cursor 2
  useEffect(() => {
    if (!cursor2) return;
    const interval = setInterval(() => {
      void checkNewerDeals();
    }, 45000);
    return () => clearInterval(interval);
  }, [cursor2, checkNewerDeals]);

  // Backend handles all filtering before cursor pagination, so deals is already filtered
  const filteredDeals = deals;

  const categoryCounts = useMemo(() => {
    const map: Record<string, number> = {};
    for (const d of deals) {
      if (d.categoryName) {
        map[d.categoryName] = (map[d.categoryName] || 0) + 1;
      }
    }
    return map;
  }, [deals]);

  return {
    deals: filteredDeals,
    rawDeals: deals,
    categoryCounts,
    serverMessage,
    isLoading,
    isLoadingOlder,
    isCheckingNewer,
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
    cursor1,
    cursor2,
    hasOlder,
    newDealsCount,
    pendingNewDeals,
    loadOlderDeals,
    checkNewerDeals,
    applyNewDeals,
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
    userDefaultAddress: user?.defaultAddress,
    isAuthenticated,
  };
}
