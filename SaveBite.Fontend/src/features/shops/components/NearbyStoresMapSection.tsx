import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import {
  Store,
  MapPin,
  Navigation,
  RefreshCw,
  AlertCircle,
  Star,
  ExternalLink,
  ChevronDown,
  ChevronUp,
  X,
  Compass,
  Clock,
  RotateCcw,
  Home,
} from "lucide-react";
import { shopApi } from "../api/shopApi";
import type { NearbyShop } from "../types/shop.types";
import { VIETMAP_CONFIG } from "../config/vietmap.config";
import {
  fetchVietmapRoute,
  getVietmapStyle,
  type RouteResult,
} from "../utils/vietmapUtils";
import type { LocationMode } from "@/features/flash-deals/hooks/useNearbyFlashDeals";

interface NearbyStoresMapSectionProps {
  latitude?: number | null;
  longitude?: number | null;
  radiusInKm?: number;
  isLocationReady?: boolean;
  locationMode?: LocationMode | null;
  locationLabel?: string;
  locationAddress?: string | null;
  onRequestGps?: () => void;
  onSelectSavedAddress?: () => void;
  hasDefaultAddress?: boolean;
  userDefaultAddressLabel?: string | null;
}

// Module cache to avoid blank flash on back navigation
let cachedNearbyShops: NearbyShop[] = [];

export function NearbyStoresMapSection({
  latitude,
  longitude,
  radiusInKm = 15,
  isLocationReady = false,
  locationMode,
  locationLabel,
  locationAddress,
  onRequestGps,
  onSelectSavedAddress,
  hasDefaultAddress,
  userDefaultAddressLabel,
}: NearbyStoresMapSectionProps) {
  const [shops, setShops] = useState<NearbyShop[]>(() => cachedNearbyShops);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Shop đang được chọn để xem chi tiết
  const [selectedShop, setSelectedShop] = useState<NearbyShop | null>(null);

  // Trạng thái dẫn đường VietMap
  const [routeResult, setRouteResult] = useState<RouteResult | null>(null);
  const [isRouting, setIsRouting] = useState(false);
  const [routeError, setRouteError] = useState<string | null>(null);
  const [showStepDetails, setShowStepDetails] = useState(false);

  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<any>(null);
  const shopMarkersRef = useRef<Map<string, any>>(new Map());
  const userMarkerRef = useRef<any>(null);
  const userCircleRef = useRef<any>(null);
  const routePolylineRef = useRef<any>(null);
  const routeAbortControllerRef = useRef<AbortController | null>(null);

  // 1. Tải danh sách cửa hàng lân cận từ Backend API
  const fetchShops = async () => {
    if (!isLocationReady || latitude == null || longitude == null) return;
    if (cachedNearbyShops.length === 0) {
      setIsLoading(true);
    }
    setError(null);
    try {
      const res = await shopApi.getNearbyShops({
        latitude,
        longitude,
        radiusInKm,
        page: 1,
        pageSize: 50,
      });
      const list = res.shops || [];
      setShops(list);
      cachedNearbyShops = list;
    } catch {
      setError("Không thể tải danh sách cửa hàng lân cận.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (isLocationReady && latitude != null && longitude != null) {
      void fetchShops();
    }
  }, [isLocationReady, latitude, longitude, radiusInKm]);

  // 2. Khởi tạo bản đồ VietMap Leaflet (Full Width Banner phong cách ảnh mẫu)
  useEffect(() => {
    if (!isLocationReady || !mapContainerRef.current || latitude == null || longitude == null) {
      return;
    }

    const L = (window as any).L;
    if (!L) {
      console.warn("Leaflet chưa sẵn sàng trên window.");
      return;
    }

    if (mapInstanceRef.current) {
      mapInstanceRef.current.remove();
      mapInstanceRef.current = null;
    }

    const map = L.map(mapContainerRef.current, {
      center: [latitude, longitude],
      zoom: 14,
      minZoom: 1,
      zoomControl: false, // Tùy biến vị trí nút zoom
      attributionControl: false, // Ẩn logo và attribution Leaflet
      maxBounds: [
        [-85, -180],
        [85, 180],
      ],
      maxBoundsViscosity: 1,
    });

    // Thêm nút zoom góc trên bên phải
    L.control.zoom({ position: "topright" }).addTo(map);

    mapInstanceRef.current = map;

    // Tải style bản đồ nền VietMap Vector Tile
    const initBaseMap = async () => {
      try {
        if (L.vietmapGL && VIETMAP_CONFIG.TILEMAP_API_KEY) {
          const style = await getVietmapStyle(VIETMAP_CONFIG.TILEMAP_API_KEY);
          L.vietmapGL({ style }).addTo(map);
        } else {
          L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "&copy; OpenStreetMap contributors",
          }).addTo(map);
        }
      } catch (err) {
        console.error("Lỗi tải nền VietMap, dùng OpenStreetMap:", err);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
          attribution: "&copy; OpenStreetMap contributors",
        }).addTo(map);
      }
    };
    void initBaseMap();

    // Ghim vị trí người dùng (Vòng tròn GPS Pulse hoặc Nhà riêng đã lưu)
    const isSavedAddress = locationMode === "saved_address";
    const primaryColor = isSavedAddress ? "#2563eb" : "#059669";
    const rippleColor = isSavedAddress ? "rgba(37, 99, 235, 0.4)" : "rgba(16, 185, 129, 0.4)";
    const tooltipText = isSavedAddress
      ? (userDefaultAddressLabel || locationLabel
          ? `🏠 ${userDefaultAddressLabel || locationLabel}`
          : "🏠 Địa chỉ mặc định của bạn")
      : (locationAddress ? `📍 ${locationAddress}` : "📍 Vị trí GPS hiện tại của bạn");

    const userLatLng = [latitude, longitude];
    const userIconHtml = `
      <div style="
        position: relative;
        width: 28px;
        height: 28px;
        display: flex;
        align-items: center;
        justify-content: center;
      ">
        <span style="
          position: absolute;
          width: 28px;
          height: 28px;
          border-radius: 9999px;
          background: ${rippleColor};
          animation: ping 1.5s cubic-bezier(0, 0, 0.2, 1) infinite;
        "></span>
        <span style="
          position: relative;
          width: 18px;
          height: 18px;
          border-radius: 9999px;
          background: ${primaryColor};
          border: 2.5px solid #ffffff;
          box-shadow: 0 2px 8px rgba(0,0,0,0.35);
          display: flex;
          align-items: center;
          justify-content: center;
          color: white;
          font-size: 10px;
          line-height: 1;
        ">${isSavedAddress ? "🏠" : "📍"}</span>
      </div>
    `;

    const userIcon = L.divIcon({
      className: isSavedAddress ? "user-saved-pin" : "user-gps-pulse-pin",
      html: userIconHtml,
      iconSize: [28, 28],
      iconAnchor: [14, 14],
    });

    userMarkerRef.current = L.marker(userLatLng, { icon: userIcon })
      .addTo(map)
      .bindTooltip(tooltipText, {
        permanent: false,
        direction: "top",
        offset: [0, -12],
        className: "font-bold text-xs shadow-md rounded-lg px-2.5 py-1",
      });

    userCircleRef.current = L.circle(userLatLng, {
      radius: 120,
      color: primaryColor,
      weight: 1.5,
      fillOpacity: 0.08,
      interactive: false,
    }).addTo(map);

    return () => {
      if (routeAbortControllerRef.current) {
        routeAbortControllerRef.current.abort();
      }
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }
    };
  }, [isLocationReady, latitude, longitude, locationMode, userDefaultAddressLabel, locationLabel, locationAddress]);

  // 3. Render các Marker hình tròn Avatar (giống ảnh mẫu UI)
  useEffect(() => {
    const map = mapInstanceRef.current;
    const L = (window as any).L;
    if (!map || !L) return;

    // Xóa marker cũ
    shopMarkersRef.current.forEach((marker) => marker.remove());
    shopMarkersRef.current.clear();

    if (!shops || shops.length === 0) return;

    const boundsPoints: [number, number][] = [];
    if (latitude != null && longitude != null) {
      boundsPoints.push([latitude, longitude]);
    }

    shops.forEach((shop, index) => {
      if (!shop.latitude || !shop.longitude) return;
      boundsPoints.push([shop.latitude, shop.longitude]);

      const isCurrentSelected = selectedShop?.id === shop.id;
      const imageUrl =
        shop.coverImageUrl ||
        shop.logoUrl ||
        "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=200&auto=format&fit=crop&q=80";

      // HTML cho marker hình tròn có ảnh và badge cam (giống y hệt ảnh mẫu)
      const markerHtml = `
        <div style="
          position: relative;
          width: 44px;
          height: 44px;
          cursor: pointer;
          transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
          transform: ${isCurrentSelected ? "scale(1.18)" : "scale(1)"};
        ">
          <!-- Hình ảnh Avatar / Logo quán -->
          <div style="
            width: 42px;
            height: 42px;
            border-radius: 9999px;
            overflow: hidden;
            border: ${isCurrentSelected ? "3px solid #059669" : "2.5px solid #ffffff"};
            box-shadow: ${
              isCurrentSelected
                ? "0 8px 20px rgba(5, 150, 105, 0.45)"
                : "0 4px 12px rgba(0, 0, 0, 0.22)"
            };
            background: #ffffff;
          ">
            <img 
              src="${imageUrl}" 
              alt="${shop.name}" 
              style="width: 100%; height: 100%; object-fit: cover;"
              onerror="this.src='https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=200&auto=format&fit=crop&q=80';"
            />
          </div>

          <!-- Huy hiệu số cam (Orange Badge) ở góc trên bên phải -->
          <span style="
            position: absolute;
            top: -3px;
            right: -3px;
            min-width: 18px;
            height: 18px;
            padding: 0 4px;
            border-radius: 9999px;
            background: #f97316;
            color: #ffffff;
            font-size: 11px;
            font-weight: 800;
            display: flex;
            align-items: center;
            justify-content: center;
            border: 2px solid #ffffff;
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2);
          ">
            ${index + 1}
          </span>
        </div>
      `;

      const customIcon = L.divIcon({
        className: "custom-avatar-shop-pin",
        html: markerHtml,
        iconSize: [44, 44],
        iconAnchor: [22, 22],
      });

      const marker = L.marker([shop.latitude, shop.longitude], {
        icon: customIcon,
        title: shop.name,
      }).addTo(map);

      // Tooltip hover hiển thị tên shop
      marker.bindTooltip(shop.name, {
        direction: "top",
        offset: [0, -22],
        className: "font-semibold text-xs text-neutral-800 shadow-sm",
      });

      // Bấm vào marker -> Kích hoạt xem chi tiết cửa hàng
      marker.on("click", () => {
        handleSelectShop(shop);
      });

      shopMarkersRef.current.set(shop.id, marker);
    });

    // Fit bounds ban đầu nếu chưa chọn quán nào
    if (!selectedShop && boundsPoints.length > 1) {
      map.fitBounds(L.latLngBounds(boundsPoints), {
        padding: [50, 50],
        maxZoom: 15,
      });
    }
  }, [shops, selectedShop, latitude, longitude]);

  // 4. Xử lý khi bấm vào 1 cửa hàng: Hiện thông tin chi tiết
  const handleSelectShop = (shop: NearbyShop) => {
    setSelectedShop(shop);
    // Reset lộ trình cũ khi chọn quán mới
    setRouteResult(null);
    setRouteError(null);
    setShowStepDetails(false);

    const map = mapInstanceRef.current;
    if (map) {
      // Xóa polyline cũ
      if (routePolylineRef.current) {
        map.removeLayer(routePolylineRef.current);
        routePolylineRef.current = null;
      }
      map.flyTo([shop.latitude, shop.longitude], 16, { duration: 0.8 });
    }
  };

  // 5. Xử lý khi bấm "Dẫn đường": Gọi VietMap Route API & tính thời gian tới nơi
  const handleStartRouting = async () => {
    if (!selectedShop || latitude == null || longitude == null) return;

    const map = mapInstanceRef.current;
    const L = (window as any).L;
    if (!map || !L) return;

    if (routePolylineRef.current) {
      map.removeLayer(routePolylineRef.current);
      routePolylineRef.current = null;
    }

    if (routeAbortControllerRef.current) {
      routeAbortControllerRef.current.abort();
    }

    const abortController = new AbortController();
    routeAbortControllerRef.current = abortController;
    setIsRouting(true);
    setRouteError(null);

    try {
      const origin: [number, number] = [latitude, longitude];
      const destination: [number, number] = [selectedShop.latitude, selectedShop.longitude];

      const result = await fetchVietmapRoute(
        origin,
        destination,
        VIETMAP_CONFIG.SERVICES_API_KEY,
        abortController.signal,
      );

      setRouteResult(result);

      // Vẽ đường dẫn màu xanh emerald nổi bật
      const polyline = L.polyline(result.coordinates, {
        color: "#059669",
        weight: 6,
        opacity: 0.9,
        lineCap: "round",
        lineJoin: "round",
      }).addTo(map);
      routePolylineRef.current = polyline;

      // Fit bounds để thấy cả tuyến đường
      const routeBounds = polyline
        .getBounds()
        .extend(origin)
        .extend(destination);
      map.fitBounds(routeBounds, { padding: [60, 60], maxZoom: 16 });
    } catch (err: any) {
      if (err.name !== "AbortError") {
        console.error("Lỗi tìm đường VietMap:", err);
        setRouteError(
          err.message || "Không thể tìm tuyến đường xe máy đến cửa hàng này.",
        );
      }
    } finally {
      setIsRouting(false);
    }
  };

  // 6. Đóng Card chi tiết hoặc Hủy dẫn đường
  const handleCloseDetail = () => {
    setSelectedShop(null);
    setRouteResult(null);
    setRouteError(null);
    setShowStepDetails(false);

    const map = mapInstanceRef.current;
    if (map && routePolylineRef.current) {
      map.removeLayer(routePolylineRef.current);
      routePolylineRef.current = null;
    }
    // Thu phóng lại toàn cảnh
    handleResetView();
  };

  // Định vị bay mượt đến vị trí của người dùng (GPS hoặc địa chỉ lưu)
  const handlePanToUserLocation = () => {
    const map = mapInstanceRef.current;
    if (!map || latitude == null || longitude == null) return;
    map.flyTo([latitude, longitude], 15, { animate: true, duration: 0.8 });
    if (userMarkerRef.current) {
      userMarkerRef.current.openTooltip();
    }
  };

  // Đặt lại góc nhìn toàn cảnh tất cả quán
  const handleResetView = () => {
    const map = mapInstanceRef.current;
    const L = (window as any).L;
    if (!map || !L) return;

    const points: [number, number][] = shops
      .filter((s) => s.latitude && s.longitude)
      .map((s) => [s.latitude, s.longitude]);
    if (latitude != null && longitude != null) {
      points.push([latitude, longitude]);
    }
    if (points.length > 0) {
      map.fitBounds(L.latLngBounds(points), { padding: [50, 50], maxZoom: 15 });
    }
  };

  if (!isLocationReady) {
    return null;
  }

  const fallbackCover =
    "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800&auto=format&fit=crop&q=80";

  return (
    <section className="mb-8">
      {/* 1. Header giống ảnh mẫu: Tiêu đề + Phụ đề + Bộ nút điều khiển bản đồ */}
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-extrabold tracking-tight text-neutral-900 sm:text-2xl">
            Cửa hàng gần bạn
          </h2>
          <p className="mt-0.5 text-xs text-neutral-500 sm:text-sm">
            Chạm vào một điểm trên bản đồ để xem ưu đãi của cửa hàng đó
          </p>
        </div>

        {/* Nút hành động phụ */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Quick Toggle GPS / Địa chỉ mặc định nếu tài khoản có địa chỉ lưu */}
          {hasDefaultAddress && (
            <div className="flex items-center rounded-xl border border-neutral-200 bg-neutral-100/90 p-0.5 text-xs shadow-2xs">
              <button
                type="button"
                onClick={onRequestGps}
                className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-bold transition cursor-pointer ${
                  locationMode === "gps"
                    ? "bg-white text-emerald-700 shadow-2xs"
                    : "text-neutral-600 hover:text-neutral-900"
                }`}
                title="Dùng vị trí GPS thực tế"
              >
                <Navigation size={12} className={locationMode === "gps" ? "text-emerald-600" : "text-neutral-400"} />
                <span>GPS</span>
              </button>
              <button
                type="button"
                onClick={onSelectSavedAddress}
                className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-bold transition cursor-pointer ${
                  locationMode === "saved_address"
                    ? "bg-white text-blue-700 shadow-2xs"
                    : "text-neutral-600 hover:text-neutral-900"
                }`}
                title={`Dùng địa chỉ mặc định: ${userDefaultAddressLabel || ""}`}
              >
                <Home size={12} className={locationMode === "saved_address" ? "text-blue-600" : "text-neutral-400"} />
                <span className="max-w-[130px] truncate">
                  {userDefaultAddressLabel ? `${userDefaultAddressLabel}` : "Địa chỉ mặc định"}
                </span>
              </button>
            </div>
          )}

          {/* Nút Xem vị trí của tôi */}
          <button
            type="button"
            onClick={handlePanToUserLocation}
            className={`inline-flex items-center gap-1.5 rounded-xl border px-3 py-1.5 text-xs font-bold shadow-2xs transition cursor-pointer ${
              locationMode === "saved_address"
                ? "border-blue-200 bg-blue-50/80 text-blue-800 hover:bg-blue-100/80"
                : "border-emerald-200 bg-emerald-50/80 text-emerald-800 hover:bg-emerald-100/80"
            }`}
            title="Định vị đến vị trí của bạn trên bản đồ"
          >
            {locationMode === "saved_address" ? (
              <Home size={13} className="text-blue-600" />
            ) : (
              <Navigation size={13} className="text-emerald-600" />
            )}
            <span>Vị trí của tôi</span>
          </button>

          <button
            type="button"
            onClick={handleResetView}
            className="inline-flex items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs hover:bg-neutral-50 transition cursor-pointer"
            title="Xem toàn bộ cửa hàng"
          >
            <RotateCcw size={13} className="text-neutral-500" />
            <span className="hidden sm:inline">Toàn cảnh</span>
          </button>

          <button
            type="button"
            onClick={() => void fetchShops()}
            disabled={isLoading}
            className="inline-flex items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs hover:bg-neutral-50 transition disabled:opacity-50 cursor-pointer"
            title="Tải lại danh sách"
          >
            <RefreshCw
              size={13}
              className={isLoading ? "animate-spin text-emerald-600" : "text-neutral-500"}
            />
            <span className="hidden sm:inline">Làm mới</span>
          </button>
        </div>
      </div>

      {/* 2. Khung Bản Đồ VietMap Leaflet (Full Width Banner giống ảnh mẫu) */}
      <div className="relative h-[380px] sm:h-[420px] md:h-[460px] w-full overflow-hidden rounded-3xl border border-neutral-200/90 bg-neutral-100 shadow-2xs">
        {/* Container render Leaflet */}
        <div ref={mapContainerRef} className="h-full w-full" style={{ zIndex: 1 }} />

        {/* Loading overlay khi fetch shops */}
        {isLoading && (
          <div className="absolute inset-0 z-20 flex items-center justify-center bg-white/60 backdrop-blur-xs">
            <div className="flex items-center gap-2.5 rounded-2xl bg-white px-4 py-2.5 text-xs font-bold text-emerald-800 shadow-md">
              <RefreshCw size={15} className="animate-spin text-emerald-600" />
              <span>Đang tải các cửa hàng quanh bạn…</span>
            </div>
          </div>
        )}

        {/* Error message */}
        {error && (
          <div className="absolute left-4 top-4 z-20 flex items-center gap-2 rounded-xl border border-amber-200 bg-amber-50 px-3.5 py-2 text-xs font-medium text-amber-800 shadow-sm">
            <AlertCircle size={15} className="text-amber-600 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {/* Gợi ý tương tác khi chưa chọn quán */}
        {!selectedShop && (
          <div className="pointer-events-none absolute bottom-4 left-4 z-10 hidden sm:block">
            <div className="flex items-center gap-2 rounded-2xl border border-white/80 bg-white/95 px-3.5 py-2 text-xs font-semibold text-neutral-700 shadow-md backdrop-blur-md">
              <Compass size={14} className="text-emerald-600" />
              <span>Chạm vào ảnh cửa hàng trên bản đồ để xem chi tiết & dẫn đường</span>
            </div>
          </div>
        )}

        {/* Nút định vị nổi trên bản đồ */}
        <button
          type="button"
          onClick={handlePanToUserLocation}
          className="absolute bottom-4 right-4 z-20 flex h-10 w-10 items-center justify-center rounded-2xl border border-neutral-200 bg-white/95 text-neutral-700 shadow-md backdrop-blur-md transition hover:bg-white hover:scale-105 active:scale-95 cursor-pointer"
          title="Định vị đến vị trí của tôi"
        >
          {locationMode === "saved_address" ? (
            <Home size={18} className="text-blue-600" />
          ) : (
            <Navigation size={18} className="text-emerald-600" />
          )}
        </button>

        {/* 3. CARD THÔNG TIN CHI TIẾT CỬA HÀNG (Hiện khi click vô cửa hàng) */}
        {selectedShop && (
          <div className="absolute bottom-4 left-4 right-4 z-20 sm:right-auto sm:max-w-md animate-in fade-in slide-in-from-bottom-3 duration-200">
            <div className="overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-xl backdrop-blur-md">
              {/* Header ảnh & nút đóng */}
              <div className="relative h-28 w-full bg-neutral-900">
                <img
                  src={selectedShop.coverImageUrl || fallbackCover}
                  alt={selectedShop.name}
                  className="h-full w-full object-cover"
                  onError={(e) => {
                    e.currentTarget.src = fallbackCover;
                  }}
                />
                <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent" />

                {/* Nút đóng Card */}
                <button
                  type="button"
                  onClick={handleCloseDetail}
                  className="absolute right-2.5 top-2.5 flex h-7 w-7 items-center justify-center rounded-full bg-black/50 text-white hover:bg-black/80 transition cursor-pointer"
                  title="Đóng thông tin"
                >
                  <X size={15} />
                </button>

                {/* Badge Khoảng cách & Mở/Đóng cửa */}
                <div className="absolute bottom-2.5 left-3 right-3 flex items-center justify-between text-white">
                  <div className="flex items-center gap-1.5 rounded-full bg-black/60 px-2.5 py-0.5 text-[11px] font-bold backdrop-blur-xs">
                    <MapPin size={11} className="text-emerald-400" />
                    <span>Cách bạn {selectedShop.distanceInKm} km</span>
                  </div>

                  <span
                    className={`rounded-full px-2 py-0.5 text-[10px] font-bold backdrop-blur-xs ${
                      selectedShop.isOpen
                        ? "bg-emerald-600 text-white"
                        : "bg-neutral-800 text-neutral-200"
                    }`}
                  >
                    {selectedShop.isOpen ? "Đang mở cửa" : "Đã đóng cửa"}
                  </span>
                </div>
              </div>

              {/* Thông tin chi tiết */}
              <div className="p-4">
                <div className="flex items-start gap-3">
                  {/* Logo Avatar */}
                  <div className="relative -mt-7 flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-xl border-2 border-white bg-white shadow-md">
                    {selectedShop.logoUrl ? (
                      <img
                        src={selectedShop.logoUrl}
                        alt={selectedShop.name}
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <div className="flex h-full w-full items-center justify-center bg-emerald-50 text-emerald-700">
                        <Store size={22} />
                      </div>
                    )}
                  </div>

                  {/* Tên quán & Đánh giá */}
                  <div className="min-w-0 flex-1">
                    <h3 className="truncate text-base font-extrabold text-neutral-900">
                      {selectedShop.name}
                    </h3>
                    <div className="mt-0.5 flex items-center gap-1.5 text-xs text-neutral-500">
                      <span className="flex items-center gap-0.5 font-bold text-amber-500">
                        <Star size={12} className="fill-amber-400 text-amber-400" />
                        <span>
                          {selectedShop.averageRating > 0
                            ? selectedShop.averageRating
                            : "5.0"}
                        </span>
                      </span>
                      <span>•</span>
                      <span>
                        {selectedShop.totalReviews > 0
                          ? `${selectedShop.totalReviews} đánh giá`
                          : "Chưa có đánh giá"}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Địa chỉ cụ thể */}
                <p className="mt-2.5 line-clamp-1 text-xs text-neutral-600 flex items-center gap-1.5">
                  <MapPin size={13} className="text-neutral-400 shrink-0" />
                  <span className="truncate">{selectedShop.address}</span>
                </p>

                {/* Giờ hoạt động */}
                {selectedShop.openingTime && selectedShop.closingTime && (
                  <p className="mt-1 text-xs text-neutral-500 flex items-center gap-1.5">
                    <Clock size={13} className="text-neutral-400 shrink-0" />
                    <span>
                      {String(selectedShop.openingTime).slice(0, 5)} -{" "}
                      {String(selectedShop.closingTime).slice(0, 5)}
                    </span>
                  </p>
                )}

                {/* 4. KHỐI HIỂN THỊ KẾT QUẢ DẪN ĐƯỜNG & THỜI GIAN ĐẾN NƠI */}
                {isRouting && (
                  <div className="mt-3 flex items-center gap-2 rounded-xl bg-emerald-50 p-2.5 text-xs font-bold text-emerald-800">
                    <Navigation size={14} className="animate-spin text-emerald-600" />
                    <span>Đang tìm lộ trình xe máy và tính thời gian tới nơi…</span>
                  </div>
                )}

                {routeError && (
                  <div className="mt-3 rounded-xl border border-amber-200 bg-amber-50 p-2.5 text-xs font-medium text-amber-800">
                    {routeError}
                  </div>
                )}

                {routeResult && (
                  <div className="mt-3 rounded-2xl border border-emerald-200 bg-emerald-50/70 p-3">
                    <div className="flex items-center justify-between">
                      <div>
                        <div className="text-[10px] font-black uppercase tracking-wider text-emerald-800">
                          Lộ trình xe máy VietMap
                        </div>
                        <div className="mt-0.5 flex items-baseline gap-2">
                          <span className="text-base font-extrabold text-emerald-900">
                            ⏱️ ~{routeResult.durationMinutes} phút
                          </span>
                          <span className="text-xs font-bold text-emerald-700">
                            ({routeResult.distanceFormatted})
                          </span>
                        </div>
                      </div>

                      {routeResult.instructions.length > 0 && (
                        <button
                          type="button"
                          onClick={() => setShowStepDetails(!showStepDetails)}
                          className="flex items-center gap-1 rounded-lg bg-white px-2 py-1 text-[11px] font-bold text-emerald-800 shadow-2xs hover:bg-emerald-100 transition cursor-pointer"
                        >
                          <span>{showStepDetails ? "Ẩn bước" : "Xem bước đi"}</span>
                          {showStepDetails ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                        </button>
                      )}
                    </div>

                    {/* Chi tiết từng bước chỉ đường (Accordion) */}
                    {showStepDetails && routeResult.instructions.length > 0 && (
                      <ol className="mt-2.5 max-h-32 overflow-y-auto space-y-1 rounded-xl bg-white p-2.5 text-[11px] text-neutral-700 list-decimal pl-5">
                        {routeResult.instructions.map((step, idx) => (
                          <li key={idx} className="leading-relaxed">
                            {step}
                          </li>
                        ))}
                      </ol>
                    )}
                  </div>
                )}

                {/* Các nút hành động chính: Dẫn đường & Xem Deals */}
                <div className="mt-3.5 flex items-center gap-2 pt-2 border-t border-neutral-100">
                  <button
                    type="button"
                    onClick={handleStartRouting}
                    disabled={isRouting}
                    className="flex-1 inline-flex items-center justify-center gap-1.5 rounded-xl bg-emerald-600 px-3.5 py-2.5 text-xs font-extrabold text-white shadow-md shadow-emerald-600/25 transition hover:bg-emerald-700 disabled:opacity-60 cursor-pointer"
                  >
                    <Navigation
                      size={14}
                      className={isRouting ? "animate-spin" : ""}
                    />
                    <span>
                      {routeResult ? "Cập nhật lộ trình" : "Dẫn đường (Xe máy)"}
                    </span>
                  </button>

                  <Link
                    to={`/shops/${selectedShop.id}`}
                    className="inline-flex items-center justify-center gap-1 rounded-xl border border-neutral-200 bg-white px-3.5 py-2.5 text-xs font-bold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 hover:text-emerald-700"
                  >
                    <span>Xem cửa hàng & Deals</span>
                    <ExternalLink size={12} />
                  </Link>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
