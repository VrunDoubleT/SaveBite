import { useCallback, useEffect, useMemo, useState } from "react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import { categoryApi } from "@/features/categories/api/categoryApi";
import type { DealFilterState, FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

// Default coordinates for HCMC center (where seed demo shop data is located)
const DEFAULT_COORDS = {
  latitude: 10.7626,
  longitude: 106.6601,
};

export function useNearbyFlashDeals() {
  const [coords, setCoords] = useState(DEFAULT_COORDS);
  const [isLocationReady, setIsLocationReady] = useState(false);
  const [radiusInKm, setRadiusInKm] = useState(15);
  const [deals, setDeals] = useState<FlashDeal[]>([]);
  const [serverMessage, setServerMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [categories, setCategories] = useState<string[]>([]);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);

  const [selectedShop, setSelectedShop] = useState<string | null>(null);
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

  // 1. Resolve GPS coordinates first before making the initial API call
  useEffect(() => {
    let isMounted = true;

    if ("geolocation" in navigator) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          if (isMounted) {
            setCoords({
              latitude: position.coords.latitude,
              longitude: position.coords.longitude,
            });
            setIsLocationReady(true);
          }
        },
        () => {
          // If denied, timeout or error, fallback to default coords
          if (isMounted) {
            setCoords(DEFAULT_COORDS);
            setIsLocationReady(true);
          }
        },
        { timeout: 3000, enableHighAccuracy: true },
      );
    } else {
      setCoords(DEFAULT_COORDS);
      setIsLocationReady(true);
    }

    return () => {
      isMounted = false;
    };
  }, []);

  // 2. Call backend API to fetch flash deals (only after location is ready)
  const fetchDeals = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await flashDealApi.getNearbyDeals({
        latitude: coords.latitude,
        longitude: coords.longitude,
        radiusInKm,
      });
      setDeals(res.deals);
      setServerMessage(res.message ?? null);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : "Failed to load flash deals.";
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  }, [coords.latitude, coords.longitude, radiusInKm]);

  useEffect(() => {
    if (!isLocationReady) return;
    void fetchDeals();
  }, [isLocationReady, fetchDeals]);

  // 3. Client-side deal filtering
  const filteredDeals = useMemo(() => {
    const distRules = [
      { label: "Under 2km", test: (d: FlashDeal) => (d.distanceInKm ?? 0) < 2 },
      { label: "Under 5km", test: (d: FlashDeal) => (d.distanceInKm ?? 0) < 5 },
      { label: "Over 5km", test: (d: FlashDeal) => (d.distanceInKm ?? 0) >= 5 },
    ].filter((x) => filters.distance.includes(x.label));

    const priceRules = [
      { label: "Under 30k", test: (d: FlashDeal) => d.minDealPrice < 30000 },
      {
        label: "30k - 50k",
        test: (d: FlashDeal) => d.minDealPrice >= 30000 && d.minDealPrice <= 50000,
      },
      { label: "Over 50k", test: (d: FlashDeal) => d.minDealPrice > 50000 },
    ].filter((x) => filters.price.includes(x.label));

    return deals.filter((d) => {
      if (selectedShop && d.shopName !== selectedShop) return false;
      if (filters.category.length && (!d.categoryName || !filters.category.includes(d.categoryName))) return false;
      if (distRules.length && !distRules.some((r) => r.test(d))) return false;
      if (priceRules.length && !priceRules.some((r) => r.test(d))) return false;
      return true;
    });
  }, [deals, selectedShop, filters]);

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
    error,
    coords,
    radiusInKm,
    setRadiusInKm,
    selectedShop,
    setSelectedShop,
    filters,
    setFilters,
    refreshDeals: fetchDeals,
    categories,
    isLoadingCategories,
  };
}
