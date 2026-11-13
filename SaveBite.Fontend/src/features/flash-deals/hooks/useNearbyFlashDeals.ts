import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import { categoryApi } from "@/features/categories/api/categoryApi";
import { useAuthStore } from "@/shared/stores/authStore";
import { useDebounce } from "@/shared/hooks/useDebounce";
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
  const [error, setError] = useState<string | null>(null);

  // Keyset Cursor state (Section 8.2 & 8.4)
  const [nextCursor, setNextCursor] = useState<string | null>(() => cachedNextCursor);
  const [hasOlder, setHasOlder] = useState(() => cachedHasOlder);
  const [newDealsCount, setNewDealsCount] = useState(0);

  const [categories, setCategories] = useState<string[]>(() => cachedCategories);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);

  // Reference guards
  const dealsRef = useRef(deals);
  const newDealIdsRef = useRef<Set<string>>(new Set());
  useEffect(() => {
    dealsRef.current = deals;
  }, [deals]);

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
  }, [coords, locationMode, locationLabel, locationAddress, deals, nextCursor, hasOlder, serverMessage, categories]);

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
      .catch(() => {})
      .finally(() => {
        if (isMounted) {
          setIsLoadingCategories(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  // Sync GPS to backend
  const syncGpsToBackend = useCallback((lat: number, lon: number, accuracy?: number) => {
    if (isAuthenticated) {
      flashDealApi.updateGps({ latitude: lat, longitude: lon, accuracy }).catch(() => {
        // Non-blocking sync error
      });
    }
  }, [isAuthenticated]);

  // Request browser GPS on user action or auto
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
      // Fallback to saved address if GPS permission denied
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
  }, [user?.defaultAddress, syncGpsToBackend]);

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

  // Section 2: GPS is Priority #1! Only fallback to Default Address if GPS not available.
  useEffect(() => {
    if (cachedCoords) {
      setIsLocationReady(true);
      return;
    }

    // Attempt GPS first
    if ("geolocation" in navigator) {
      navigator.permissions?.query({ name: "geolocation" as PermissionName }).then((perm) => {
        if (perm.state === "granted" || perm.state === "prompt") {
          requestGpsLocation();
        } else if (user?.defaultAddress) {
          selectSavedAddress();
        }
      }).catch(() => {
        if (user?.defaultAddress) {
          selectSavedAddress();
        } else {
          requestGpsLocation();
        }
      });
    } else if (user?.defaultAddress) {
      selectSavedAddress();
    }
  }, [user?.defaultAddress, requestGpsLocation, selectSavedAddress]);

  // Reverse geocoding for GPS display
  useEffect(() => {
    if (locationMode !== "gps" || !coords) return;
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
          if (data.display_name && isMounted) {
            const chunks = data.display_name.split(", ").slice(0, 4);
            const cleaned = cleanAddressParts(chunks);
            if (cleaned && isMounted) {
              setLocationAddress(cleaned);
              return;
            }
          }
        }
      } catch {}

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
        limit: 9,
        category: filters.category.length > 0 ? filters.category.join(",") : undefined,
        distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
        price: filters.price.length > 0 ? filters.price.join(",") : undefined,
        shopName: selectedShop ?? undefined,
        keyword: debouncedKeyword ? debouncedKeyword : undefined,
      });

      const uniqueDeals = Array.from(
        new Map(
          res.deals.map((deal) => {
            const isNewlyArrived = newDealIdsRef.current.has(deal.id.toLowerCase());
            return [deal.id, { ...deal, isNew: isNewlyArrived || deal.isNew }];
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
          const newUnique = res.deals.filter((d) => !existingIds.has(d.id));
          return [...prev, ...newUnique];
        });
      }

      setNextCursor(res.nextCursor ?? null);
      setHasOlder(res.hasMore ?? res.hasOlder);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to load older deals.";
      if (msg.includes("CURSOR_LOCATION_CHANGED")) {
        // Location context changed beyond threshold: reset feed to Page 1
        setError("Vị trí của bạn đã thay đổi. Đang làm mới danh sách...");
        void fetchInitialDeals();
      } else {
        setError(msg);
      }
    } finally {
      isLoadingOlderRef.current = false;
      setIsLoadingOlder(false);
    }
  }, [coords, radiusInKm, filters, selectedShop, debouncedKeyword, locationMode, fetchInitialDeals]);

  // Section 8.3 & Realtime handling:
  // - Deal Hidden / Closed / Cancelled: Lập tức loại bỏ khỏi danh sách deal đang hiển thị
  // - Deal Opened / Re-opened / Published: Tự động hiện ra ngay trên màn hình với chữ "NEW", không cần click nút
  const handleRealtimeDealEvent = useCallback(
    async (eventData?: FlashDealRealtimePayload) => {
      if (!eventData) return;

      const action = eventData.action?.toLowerCase();
      const status = eventData.status?.toLowerCase();
      const dealId = eventData.dealId?.toLowerCase();

      // 1. Hide / Close / Cancel: IMMEDIATELY remove from active feed!
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
          setDeals((prev) => prev.filter((d) => d.id.toLowerCase() !== dealId));
        }
        return;
      }

      // 2. Open / Published / Reopen: Tự động hiện ra ngay lập tức trên UI với nhãn NEW!
      if (
        action === "opened" ||
        action === "published" ||
        action === "reopened" ||
        status === "onsale" ||
        status === "open" ||
        status === "pending"
      ) {
        if (dealId) {
          newDealIdsRef.current.add(dealId);
          try {
            const freshDeal = await flashDealApi.getDealById(dealId);
            if (freshDeal && freshDeal.status !== 2 && freshDeal.status !== 4 && freshDeal.status !== 5) {
              const markedDeal: FlashDeal = {
                ...freshDeal,
                isNew: true,
              };
              setDeals((prev) => {
                const rest = prev.filter((d) => d.id.toLowerCase() !== dealId);
                return [markedDeal, ...rest];
              });
              return;
            }
          } catch {
            // Fallback: nếu lấy chi tiết lỗi, tự động refresh Page 1
          }
        }
        void fetchInitialDeals();
      }
    },
    [fetchInitialDeals],
  );

  useFlashDealRealtime("all", undefined, handleRealtimeDealEvent);

  // When user clicks "Show new deals" on banner: refresh from Page 1 with fresh SnapshotAt
  const applyNewDeals = useCallback(() => {
    setNewDealsCount(0);
    setNextCursor(null);
    void fetchInitialDeals();
  }, [fetchInitialDeals]);

  // Trigger on init or location/radius change
  useEffect(() => {
    if (!isLocationReady) return;
    void fetchInitialDeals();
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
