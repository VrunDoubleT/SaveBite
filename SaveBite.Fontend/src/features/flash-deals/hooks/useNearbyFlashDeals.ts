import { useCallback, useEffect, useMemo, useState } from "react";
import { flashDealApi } from "@/features/flash-deals/api/flashDealApi";
import { categoryApi } from "@/features/categories/api/categoryApi";
import { useAuthStore } from "@/shared/stores/authStore";
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

export function useNearbyFlashDeals() {
  const user = useAuthStore((s) => s.user);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [coords, setCoords] = useState<{ latitude: number; longitude: number } | null>(() => cachedCoords);
  const [locationMode, setLocationMode] = useState<LocationMode | null>(() => cachedLocationMode);
  const [locationLabel, setLocationLabel] = useState<string>(() => cachedLocationLabel);
  const [locationAddress, setLocationAddress] = useState<string | null>(() => cachedLocationAddress);
  const [gpsError, setGpsError] = useState<string | null>(null);
  const [isGpsLoading, setIsGpsLoading] = useState(false);
  const [isLocationReady, setIsLocationReady] = useState(() => cachedCoords !== null);

  const [radiusInKm, setRadiusInKm] = useState(15);
  const [deals, setDeals] = useState<FlashDeal[]>(() => cachedDeals);
  const [serverMessage, setServerMessage] = useState<string | null>(() => cachedServerMessage);
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingOlder, setIsLoadingOlder] = useState(false);
  const [isCheckingNewer, setIsCheckingNewer] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // 2 con trỏ thời gian (Cursor 1: cũ nhất, Cursor 2: mới nhất)
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

  // Request browser GPS on user action (hoặc tự động khi đã có quyền)
  const requestGpsLocation = useCallback(() => {
    if (!("geolocation" in navigator)) {
      setGpsError("Geolocation is not supported by your browser.");
      return;
    }

    setIsGpsLoading(true);
    setGpsError(null);

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setCoords({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        });
        setLocationMode("gps");
        setLocationLabel("Current GPS Location");
        setIsGpsLoading(false);
        setIsLocationReady(true);
      },
      (err) => {
        setIsGpsLoading(false);
        setIsLocationReady(false);
        if (err.code === 1) {
          setGpsError("Please allow GPS location permission to view deals near you.");
        } else {
          setGpsError("Unable to retrieve GPS coordinates. Please try again.");
        }
      },
      { timeout: 8000, enableHighAccuracy: false, maximumAge: 60000 },
    );
  }, []);

  // Khởi tạo: kiểm tra quyền GPS tự động nếu user đã cấp trước đó, hoặc dùng địa chỉ lưu nếu đã đăng nhập
  useEffect(() => {
    // Nếu có địa chỉ lưu của user và người dùng chưa chủ động chọn GPS, ưu tiên dùng địa chỉ lưu
    if (user?.defaultAddress && cachedLocationMode !== "gps") {
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
      const fullAddr = [
        user.defaultAddress.addressLine,
        user.defaultAddress.ward,
        user.defaultAddress.district,
        user.defaultAddress.city,
      ].filter(Boolean).join(", ");
      const addrToDisplay = fullAddr || user.defaultAddress.addressLine;
      cachedLocationAddress = addrToDisplay;

      setCoords(newCoords);
      setLocationMode("saved_address");
      setLocationLabel(label);
      setLocationAddress(addrToDisplay);
      setIsLocationReady(true);
      return;
    }

    // Nếu đã có tọa độ từ phiên trước đó trong cache, dùng ngay không cần gọi lại GPS
    if (cachedCoords) {
      setIsLocationReady(true);
      return;
    }

    // Tự động kiểm tra quyền GPS của trình duyệt
    if (navigator.permissions && navigator.permissions.query) {
      navigator.permissions
        .query({ name: "geolocation" })
        .then((permission) => {
          if (permission.state === "granted") {
            requestGpsLocation();
          }
          permission.onchange = () => {
            if (permission.state === "granted") {
              requestGpsLocation();
            } else if (permission.state === "denied") {
              setCoords(null);
              cachedCoords = null;
              setIsLocationReady(false);
              setLocationMode(null);
              setLocationLabel("Location Not Set");
            }
          };
        })
        .catch(() => {});
    }
  }, [user?.defaultAddress, requestGpsLocation]);

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
      const fullAddr = [
        user.defaultAddress.addressLine,
        user.defaultAddress.ward,
        user.defaultAddress.district,
        user.defaultAddress.city,
      ].filter(Boolean).join(", ");
      const addrToDisplay = fullAddr || user.defaultAddress.addressLine;
      cachedLocationAddress = addrToDisplay;

      setCoords(newCoords);
      setLocationMode("saved_address");
      setLocationLabel(label);
      setLocationAddress(addrToDisplay);
      setGpsError(null);
      setIsLocationReady(true);
    }
  }, [user?.defaultAddress]);

  // Switch to default location
  const selectDefaultLocation = useCallback(() => {
    requestGpsLocation();
  }, [requestGpsLocation]);

  // Tự động phân giải địa chỉ thực tế từ tọa độ GPS để hiển thị
  useEffect(() => {
    if (!coords) {
      setLocationAddress(null);
      return;
    }

    // Nếu đang dùng địa chỉ lưu (saved_address), dùng trực tiếp thông tin từ profile
    if (locationMode === "saved_address" && user?.defaultAddress) {
      const fullAddr = [
        user.defaultAddress.addressLine,
        user.defaultAddress.ward,
        user.defaultAddress.district,
        user.defaultAddress.city,
      ].filter(Boolean).join(", ");
      setLocationAddress(fullAddr || user.defaultAddress.addressLine);
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
            ].filter(Boolean);
            if (parts.length > 0 && isMounted) {
              setLocationAddress(parts.join(", "));
              return;
            }
          }
          if (data.display_name && isMounted) {
            const chunks = data.display_name.split(", ").slice(0, 4);
            setLocationAddress(chunks.join(", "));
            return;
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
          const parts2 = [data2.locality, data2.city, data2.countryName].filter(Boolean);
          if (parts2.length > 0 && isMounted) {
            setLocationAddress(parts2.join(", "));
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
  }, [coords]);

  // 2. Tải ban đầu (Initial load) - Lọc trực tiếp từ Backend kết hợp Cursor Pagination
  const fetchInitialDeals = useCallback(async () => {
    if (!coords) return;
    // Chỉ hiện hiệu ứng Skeleton khi chưa có dữ liệu món ăn trong danh sách
    if (deals.length === 0) {
      setIsLoading(true);
    }
    setError(null);
    setNewDealsCount(0);
    setPendingNewDeals([]);
    try {
      const res = await flashDealApi.getNearbyDeals({
        latitude: coords.latitude,
        longitude: coords.longitude,
        radiusInKm,
        mode: "Initial",
        limit: 9,
        category: filters.category.length > 0 ? filters.category.join(",") : undefined,
        distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
        price: filters.price.length > 0 ? filters.price.join(",") : undefined,
        shopName: selectedShop ?? undefined,
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
  }, [coords, radiusInKm, filters, selectedShop, deals.length]);

  // 3. DUAL-CURSOR: Phân trang xuống – kết hợp cả 2 con trỏ trong 1 lần gọi API
  //
  // Luồng hoạt động:
  //   - Truyền cursor1 (mốc cũ nhất hiện tại) để lấy thêm deal cũ hơn
  //   - Truyền cursor2 (mốc mới nhất hiện tại) để backend đồng thời kiểm tra
  //     có deal nào được tạo mới kể từ lần load gần nhất không
  //
  // Backend trả về: [deal mới hơn cursor2] ++ [deal cũ hơn cursor1 (tối đa limit)]
  //
  // Frontend merge kết quả:
  //   - Deal mới (prependedCount đầu tiên) → sort lên đầu danh sách
  //   - Deal cũ → append xuống cuối danh sách
  //   - Cập nhật cursor2 lên mốc mới nhất vừa nhận được
  //   - Reset badge thông báo deal mới (đã được tải vào list)
  const loadOlderDeals = useCallback(async () => {
    if (!coords || !cursor1 || isLoadingOlder) return;
    setIsLoadingOlder(true);
    try {
      const res = await flashDealApi.getNearbyDeals({
        latitude: coords.latitude,
        longitude: coords.longitude,
        radiusInKm,
        cursor1,          // Con trỏ 1: mốc cũ nhất → lấy deal cũ hơn
        cursor2: cursor2 ?? undefined, // Con trỏ 2: mốc mới nhất → phát hiện deal mới
        mode: "Older",
        limit: 9,
        category: filters.category.length > 0 ? filters.category.join(",") : undefined,
        distance: filters.distance.length > 0 ? filters.distance.join(",") : undefined,
        price: filters.price.length > 0 ? filters.price.join(",") : undefined,
        shopName: selectedShop ?? undefined,
      });

      if (res.deals.length > 0) {
        setDeals((prev) => {
          const existingIds = new Set(prev.map((d) => d.id));

          // Deal mới hơn cursor2 (prependedCount đầu tiên trong res.deals)
          // Deal cũ hơn cursor1 (phần còn lại)
          const incomingNew = res.deals
            .slice(0, res.prependedCount)
            .filter((d) => !existingIds.has(d.id))
            .map((d) => ({ ...d, isNew: true }));
          const incomingOld = res.deals.slice(res.prependedCount).filter((d) => !existingIds.has(d.id));

          // Kết quả: [deal hiện có (Page 1 giữ nguyên)] ++ [deal mới tạo (đầu Page 2, có badge NEW)] ++ [deal cũ (tiếp theo Page 2)]
          return [...prev, ...incomingNew, ...incomingOld];
        });

        // Cập nhật cursor1 nếu có thêm deal cũ
        if (res.cursor1) {
          setCursor1(res.cursor1);
        }

        // Cập nhật cursor2 nếu backend phát hiện deal mới và đã merge vào
        if (res.prependedCount > 0 && res.cursor2) {
          setCursor2(res.cursor2);
          // Reset badge vì deal mới đã được hiển thị vào danh sách
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
  }, [coords, radiusInKm, cursor1, cursor2, isLoadingOlder, filters, selectedShop]);

  // 4. Con trỏ 2 – POLLING ONLY: Kiểm tra định kỳ xem có deal mới không.
  //
  // Backend (mode=Newer) chỉ trả về newDealsCount, KHÔNG trả deals, KHÔNG cập nhật cursor2.
  // Mục đích: hiển thị badge "X deal mới!" → user biết và scroll xuống để load page mới.
  // Cursor2 sẽ chỉ được cập nhật thực sự khi loadOlderDeals() được gọi.
  const checkNewerDeals = useCallback(async () => {
    if (!coords || !cursor2 || isCheckingNewer) return;
    setIsCheckingNewer(true);
    try {
      const res = await flashDealApi.getNearbyDeals({
        latitude: coords.latitude,
        longitude: coords.longitude,
        radiusInKm,
        cursor2,
        mode: "Newer",
      });

      if (res.hasNewer && res.newDealsCount > 0) {
        // Chỉ cập nhật số đếm badge, KHÔNG merge vào danh sách ở đây
        setNewDealsCount(res.newDealsCount);
      }
    } catch {
      // Polling thất bại trong âm thầm, không chặn UI
    } finally {
      setIsCheckingNewer(false);
    }
  }, [coords, radiusInKm, cursor2, isCheckingNewer]);

  // 5. Khi user click "Update now" trên badge thông báo deal mới:
  //    Gọi loadOlderDeals() ngay → backend sẽ prepend deal mới lên đầu page tiếp theo.
  //    Nếu chưa có page nào (chưa scroll), refresh toàn bộ.
  const applyNewDeals = useCallback(() => {
    if (cursor1) {
      // Đã có page 1 → load page tiếp theo với deal mới được prepend
      void loadOlderDeals();
    } else {
      // Chưa có page nào (vừa mở) → refresh toàn bộ
      void fetchInitialDeals();
    }
    setNewDealsCount(0);
    setPendingNewDeals([]);
  }, [cursor1, loadOlderDeals, fetchInitialDeals]);

  // Chạy khi khởi động hoặc đổi vị trí/bán kính
  useEffect(() => {
    if (!isLocationReady) return;
    void fetchInitialDeals();
  }, [isLocationReady, fetchInitialDeals]);

  // Định kỳ mỗi 45 giây dùng Cursor 2 kiểm tra deal mới
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
    gpsError,
    isGpsLoading,
    requestGpsLocation,
    selectSavedAddress,
    selectDefaultLocation,
    userDefaultAddress: user?.defaultAddress,
    isAuthenticated,
  };
}
