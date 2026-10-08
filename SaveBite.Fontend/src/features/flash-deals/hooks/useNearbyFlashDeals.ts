import { useCallback, useEffect, useMemo, useState } from "react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import type { DealFilterState, FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

// Tọa độ mặc định trung tâm TP.HCM nếu người dùng không bật GPS
const DEFAULT_COORDS = {
  latitude: 10.7626,
  longitude: 106.6601,
};

export function useNearbyFlashDeals() {
  const [coords, setCoords] = useState(DEFAULT_COORDS);
  const [radiusInKm, setRadiusInKm] = useState(15);
  const [deals, setDeals] = useState<FlashDeal[]>([]);
  const [serverMessage, setServerMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [selectedShop, setSelectedShop] = useState<string | null>(null);
  const [filters, setFilters] = useState<DealFilterState>({
    distance: [],
    price: [],
    category: [],
  });

  // 1. Tự động xin quyền GPS từ trình duyệt
  useEffect(() => {
    if ("geolocation" in navigator) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setCoords({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          });
        },
        () => {
          // Nếu người dùng từ chối, giữ nguyên tọa độ TP.HCM
        },
        { timeout: 5000 },
      );
    }
  }, []);

  // 2. Gọi API Backend lấy danh sách Flash Deal
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
      const msg = err instanceof Error ? err.message : "Không thể tải danh sách Flash Deal.";
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  }, [coords.latitude, coords.longitude, radiusInKm]);

  useEffect(() => {
    void fetchDeals();
  }, [fetchDeals]);

  // 3. Lọc danh sách Deal theo bộ lọc phía Client
  const filteredDeals = useMemo(() => {
    const distRules = [
      { label: "Dưới 2km", test: (d: FlashDeal) => (d.distanceInKm ?? 0) < 2 },
      { label: "Dưới 5km", test: (d: FlashDeal) => (d.distanceInKm ?? 0) < 5 },
      { label: "Trên 5km", test: (d: FlashDeal) => (d.distanceInKm ?? 0) >= 5 },
    ].filter((x) => filters.distance.includes(x.label));

    const priceRules = [
      { label: "Dưới 30k", test: (d: FlashDeal) => d.minDealPrice < 30000 },
      {
        label: "30k - 50k",
        test: (d: FlashDeal) => d.minDealPrice >= 30000 && d.minDealPrice <= 50000,
      },
      { label: "Trên 50k", test: (d: FlashDeal) => d.minDealPrice > 50000 },
    ].filter((x) => filters.price.includes(x.label));

    return deals.filter((d) => {
      if (selectedShop && d.shopName !== selectedShop) return false;
      if (distRules.length && !distRules.some((r) => r.test(d))) return false;
      if (priceRules.length && !priceRules.some((r) => r.test(d))) return false;
      return true;
    });
  }, [deals, selectedShop, filters]);

  return {
    deals: filteredDeals,
    rawDeals: deals,
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
  };
}
