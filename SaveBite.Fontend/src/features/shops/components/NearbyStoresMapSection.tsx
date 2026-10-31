import { useEffect, useRef, useState, useCallback } from "react";
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
  Home,
  Flame,
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
  locationVersion?: number;
  isGpsLoading?: boolean;
  gpsError?: string | null;
  onRequestGps?: () => void;
  onSelectSavedAddress?: () => void;
  onSelectDefaultLocation?: () => void;
  hasDefaultAddress?: boolean;
  userDefaultAddressLabel?: string | null;
}

// Module cache to avoid blank flash on back navigation
let cachedNearbyShops: NearbyShop[] = [];

export function NearbyStoresMapSection({
  latitude,
  longitude,
  radiusInKm = 10,
  isLocationReady = false,
  locationMode,
  locationLabel,
  locationAddress,
  locationVersion,
  isGpsLoading,
  gpsError,
  onRequestGps,
  onSelectSavedAddress,
  onSelectDefaultLocation,
  hasDefaultAddress,
  userDefaultAddressLabel,
}: NearbyStoresMapSectionProps) {
  const [shops, setShops] = useState<NearbyShop[]>(() => cachedNearbyShops);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Selected shop for detail view
  const [selectedShop, setSelectedShop] = useState<NearbyShop | null>(null);

  // VietMap motorcycle route state
  const [routeResult, setRouteResult] = useState<RouteResult | null>(null);
  const [isRouting, setIsRouting] = useState(false);
  const [routeError, setRouteError] = useState<string | null>(null);
  const [showStepDetails, setShowStepDetails] = useState(false);
  const [isMapReady, setIsMapReady] = useState(false);

  // Filter mode: "all" stores vs "deals_only"
  const [storeFilter, setStoreFilter] = useState<"all" | "deals_only">("all");
  const [allStoresCount, setAllStoresCount] = useState<number>(() => cachedNearbyShops.length);
  const [dealsStoresCount, setDealsStoresCount] = useState<number>(
    () => cachedNearbyShops.filter((s) => (s.flashDealCount ?? 0) > 0).length,
  );

  const filteredShops = shops;

  const mapContainerRef = useRef<HTMLDivElement | null>(null);
  const mapInstanceRef = useRef<any>(null);
  const shopMarkersRef = useRef<Map<string, any>>(new Map());
  const userMarkerRef = useRef<any>(null);
  const userCircleRef = useRef<any>(null);
  const routePolylineRef = useRef<any>(null);
  const routeAbortControllerRef = useRef<AbortController | null>(null);
  const lastLocationRef = useRef<{ lat: number; lng: number; mode?: string | null } | null>(null);

  // 1. Fetch nearby shops from Backend API
  const fetchShops = useCallback(
    async (lat?: number | null, lng?: number | null, filterOverride?: "all" | "deals_only") => {
      const isSavedAddress = locationMode === "saved_address";
      const targetLat = lat ?? latitude;
      const targetLng = lng ?? longitude;
      if (!isLocationReady) return;
      if (!isSavedAddress && (targetLat == null || targetLng == null)) return;
      setIsLoading(true);
      setError(null);
      const activeFilter = filterOverride ?? storeFilter;
      try {
        const res = await shopApi.getNearbyShops({
          latitude: isSavedAddress ? undefined : (targetLat ?? undefined),
          longitude: isSavedAddress ? undefined : (targetLng ?? undefined),
          radiusInKm: Math.min(radiusInKm, 10),
          page: 1,
          pageSize: 50,
          onlyWithFlashDeals: activeFilter === "deals_only",
        });
        const list = res.shops || [];
        setShops(list);
        if (res.allStoresCount !== undefined) {
          setAllStoresCount(res.allStoresCount);
        } else if (activeFilter === "all") {
          setAllStoresCount(list.length);
        }
        if (res.flashDealsStoresCount !== undefined) {
          setDealsStoresCount(res.flashDealsStoresCount);
        } else {
          setDealsStoresCount(list.filter((s) => (s.flashDealCount ?? 0) > 0).length);
        }
        if (activeFilter === "all") {
          cachedNearbyShops = list;
        }
      } catch {
        setError("Unable to load nearby stores.");
      } finally {
        setIsLoading(false);
      }
    },
    [isLocationReady, latitude, longitude, locationMode, radiusInKm, storeFilter],
  );

  const handleFilterChange = (filter: "all" | "deals_only") => {
    if (filter === storeFilter) return;
    setStoreFilter(filter);
    if (filter === "deals_only" && selectedShop && (selectedShop.flashDealCount ?? 0) === 0) {
      setSelectedShop(null);
      setRouteResult(null);
      setRouteError(null);
      setShowStepDetails(false);
      if (routePolylineRef.current && mapInstanceRef.current) {
        mapInstanceRef.current.removeLayer(routePolylineRef.current);
        routePolylineRef.current = null;
      }
    }
    void fetchShops(latitude, longitude, filter);
  };

  // Fetch shops when location, mode, or version changes
  useEffect(() => {
    if (isLocationReady) {
      if (locationMode === "saved_address" || (latitude != null && longitude != null)) {
        void fetchShops(latitude, longitude);
      }
    }
  }, [isLocationReady, latitude, longitude, locationMode, locationVersion, radiusInKm, fetchShops]);

  // 2. Initialize VietMap Leaflet map instance ONCE (never destroy on prop change, only on unmount)
  const hasCoords = latitude != null && longitude != null;
  useEffect(() => {
    if (!isLocationReady || !mapContainerRef.current || !hasCoords) {
      return;
    }

    if (mapInstanceRef.current) {
      setIsMapReady(true);
      return;
    }

    const L = (window as any).L;
    if (!L) {
      console.warn("Leaflet is not ready on window.");
      return;
    }

    const map = L.map(mapContainerRef.current, {
      center: [latitude!, longitude!],
      zoom: 14,
      minZoom: 1,
      zoomControl: false,
      attributionControl: false,
      maxBounds: [
        [-85, -180],
        [85, 180],
      ],
      maxBoundsViscosity: 1,
    });

    L.control.zoom({ position: "topright" }).addTo(map);
    mapInstanceRef.current = map;
    setIsMapReady(true);

    let isCancelled = false;
    const initBaseMap = async () => {
      try {
        if (L.vietmapGL && VIETMAP_CONFIG.TILEMAP_API_KEY) {
          const style = await getVietmapStyle(VIETMAP_CONFIG.TILEMAP_API_KEY);
          if (isCancelled || !mapInstanceRef.current || (map as any)._leaflet_id == null) return;
          L.vietmapGL({ style }).addTo(map);
        } else {
          if (isCancelled || !mapInstanceRef.current || (map as any)._leaflet_id == null) return;
          L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "&copy; OpenStreetMap contributors",
          }).addTo(map);
        }
      } catch (err) {
        if (isCancelled || !mapInstanceRef.current || (map as any)._leaflet_id == null) return;
        console.error("Error loading VietMap style, falling back to OpenStreetMap:", err);
        try {
          L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "&copy; OpenStreetMap contributors",
          }).addTo(map);
        } catch {
          // Ignore if map was already removed
        }
      }
    };
    void initBaseMap();

    setTimeout(() => {
      if (mapInstanceRef.current) {
        mapInstanceRef.current.invalidateSize();
      }
    }, 200);

    return () => {
      isCancelled = true;
      if (routeAbortControllerRef.current) {
        routeAbortControllerRef.current.abort();
      }
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }
      setIsMapReady(false);
      userMarkerRef.current = null;
      userCircleRef.current = null;
      shopMarkersRef.current.clear();
    };
  }, [isLocationReady, hasCoords]);

  // 2b. Update User Pin & Circle without destroying the map
  useEffect(() => {
    const map = mapInstanceRef.current;
    const L = (window as any).L;
    if (!isMapReady || !map || !L || latitude == null || longitude == null) {
      return;
    }

    const isSavedAddress = locationMode === "saved_address";
    const primaryColor = isSavedAddress ? "#2563eb" : "#059669";
    const rippleColor = isSavedAddress ? "rgba(37, 99, 235, 0.4)" : "rgba(16, 185, 129, 0.4)";
    const tooltipText = isSavedAddress
      ? (userDefaultAddressLabel || locationLabel
          ? `🏠 ${userDefaultAddressLabel || locationLabel}`
          : "🏠 Your saved default address")
      : (locationAddress ? `📍 ${locationAddress}` : "📍 Your current GPS location");

    const userLatLng: [number, number] = [latitude, longitude];
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

    // Reset routing and selection when coordinates or location mode changes
    const prev = lastLocationRef.current;
    const locationChanged =
      !prev ||
      prev.lat !== latitude ||
      prev.lng !== longitude ||
      prev.mode !== locationMode;

    if (locationChanged) {
      lastLocationRef.current = { lat: latitude, lng: longitude, mode: locationMode };
      if (routeAbortControllerRef.current) {
        routeAbortControllerRef.current.abort();
        routeAbortControllerRef.current = null;
      }
      if (routePolylineRef.current) {
        map.removeLayer(routePolylineRef.current);
        routePolylineRef.current = null;
      }
      setSelectedShop(null);
      setRouteResult(null);
    }

    if (userMarkerRef.current) {
      userMarkerRef.current.setLatLng(userLatLng);
      userMarkerRef.current.setIcon(userIcon);
      userMarkerRef.current.setTooltipContent(tooltipText);
    } else {
      userMarkerRef.current = L.marker(userLatLng, { icon: userIcon })
        .addTo(map)
        .bindTooltip(tooltipText, {
          permanent: false,
          direction: "top",
          offset: [0, -12],
          className: "font-bold text-xs shadow-md rounded-lg px-2.5 py-1",
        });
    }

    if (userCircleRef.current) {
      userCircleRef.current.setLatLng(userLatLng);
      userCircleRef.current.setStyle({ color: primaryColor });
    } else {
      userCircleRef.current = L.circle(userLatLng, {
        radius: 120,
        color: primaryColor,
        weight: 1.5,
        fillOpacity: 0.08,
        interactive: false,
      }).addTo(map);
    }
  }, [
    isMapReady,
    latitude,
    longitude,
    locationMode,
    userDefaultAddressLabel,
    locationLabel,
    locationAddress,
  ]);

  // 3. Render circular avatar markers
  useEffect(() => {
    const map = mapInstanceRef.current;
    const L = (window as any).L;
    if (!isMapReady || !map || !L) return;

    // Remove old markers
    shopMarkersRef.current.forEach((marker) => marker.remove());
    shopMarkersRef.current.clear();

    const boundsPoints: [number, number][] = [];
    if (latitude != null && longitude != null) {
      boundsPoints.push([latitude, longitude]);
    }

    if (filteredShops && filteredShops.length > 0) {
      filteredShops.forEach((shop) => {
        if (!shop.latitude || !shop.longitude) return;
        boundsPoints.push([shop.latitude, shop.longitude]);

        const isCurrentSelected = selectedShop?.id === shop.id;
        const dealCount = shop.flashDealCount ?? 0;
        const hasDeals = dealCount > 0;

        const imageUrl =
          shop.coverImageUrl ||
          shop.logoUrl ||
          "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=200&auto=format&fit=crop&q=80";

        // Dynamic styling depending on whether the store has active flash deals:
        // - With deals: Vibrant emerald/white border + bright orange badge showing exact deal count
        // - Without deals (0 deals): Always dimmed ("mờ mờ", opacity ~0.45) even when selected/clicked + soft gray border & '0' badge
        const markerOpacity = hasDeals ? "1" : "0.45";
        const markerFilter = hasDeals ? "none" : "grayscale(35%)";

        const markerBorder = isCurrentSelected
          ? (hasDeals ? "3px solid #059669" : "2.5px solid #94a3b8")
          : (hasDeals ? "2.5px solid #ffffff" : "2px solid #cbd5e1");

        const markerShadow = isCurrentSelected
          ? (hasDeals ? "0 8px 20px rgba(5, 150, 105, 0.45)" : "0 3px 8px rgba(0, 0, 0, 0.15)")
          : (hasDeals ? "0 4px 12px rgba(0, 0, 0, 0.22)" : "0 2px 6px rgba(0, 0, 0, 0.1)");

        const badgeBg = hasDeals ? "#f97316" : "#94a3b8";
        const badgeBorder = "2px solid #ffffff";
        const badgeText = dealCount > 99 ? "99+" : `${dealCount}`;

        // HTML for circular marker with store avatar and deal badge
        const markerHtml = `
          <div style="
            position: relative;
            width: 44px;
            height: 44px;
            cursor: pointer;
            opacity: ${markerOpacity};
            filter: ${markerFilter};
            transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1), opacity 0.2s ease;
            transform: ${isCurrentSelected ? "scale(1.18)" : "scale(1)"};
          ">
            <!-- Store Avatar / Logo -->
            <div style="
              width: 42px;
              height: 42px;
              border-radius: 9999px;
              overflow: hidden;
              position: relative;
              border: ${markerBorder};
              box-shadow: ${markerShadow};
              background: #ffffff;
            ">
              <img 
                src="${imageUrl}" 
                alt="${shop.name}" 
                style="width: 100%; height: 100%; object-fit: cover;"
                onerror="this.src='https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=200&auto=format&fit=crop&q=80';"
              />
            </div>

            <!-- Deal count badge at top-right -->
            <span style="
              position: absolute;
              top: -3px;
              right: -3px;
              min-width: 18px;
              height: 18px;
              padding: 0 4px;
              border-radius: 9999px;
              background: ${badgeBg};
              color: #ffffff;
              font-size: 11px;
              font-weight: 800;
              display: flex;
              align-items: center;
              justify-content: center;
              border: ${badgeBorder};
              box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2);
            " title="${hasDeals ? `${dealCount} Flash Deals` : "0 Flash Deals (No active deals)"}">
              ${badgeText}
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

        // Tooltip hover showing shop name and flash deal count status (in English)
        const tooltipText = hasDeals
          ? `${shop.name} (${dealCount} Flash Deal${dealCount > 1 ? "s" : ""})`
          : `${shop.name} (No flash deals - 0 deals)`;

        marker.bindTooltip(tooltipText, {
          direction: "top",
          offset: [0, -22],
          className: "font-semibold text-xs text-neutral-800 shadow-sm",
        });

        // Click marker to view shop details
        marker.on("click", () => {
          handleSelectShop(shop);
        });

        shopMarkersRef.current.set(shop.id, marker);
      });
    }

    // Smoothly fit bounds when no store is selected
    if (!selectedShop) {
      if (boundsPoints.length > 1) {
        map.fitBounds(L.latLngBounds(boundsPoints), {
          padding: [50, 50],
          maxZoom: 15,
        });
      } else if (latitude != null && longitude != null) {
        map.flyTo([latitude, longitude], 14, { animate: true, duration: 0.8 });
      }
    }
  }, [isMapReady, filteredShops, selectedShop, latitude, longitude]);

  // 4. Handle shop selection
  const handleSelectShop = (shop: NearbyShop) => {
    setSelectedShop(shop);
    // Reset old route when selecting a new store
    setRouteResult(null);
    setRouteError(null);
    setShowStepDetails(false);

    const map = mapInstanceRef.current;
    if (map) {
      // Remove old polyline
      if (routePolylineRef.current) {
        map.removeLayer(routePolylineRef.current);
        routePolylineRef.current = null;
      }
      map.flyTo([shop.latitude, shop.longitude], 16, { duration: 0.8 });
    }
  };

  // 5. Motorbike routing via VietMap API
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

      // Draw route line in emerald green
      const polyline = L.polyline(result.coordinates, {
        color: "#059669",
        weight: 6,
        opacity: 0.9,
        lineCap: "round",
        lineJoin: "round",
      }).addTo(map);
      routePolylineRef.current = polyline;

      // Fit bounds to show entire route
      const routeBounds = polyline
        .getBounds()
        .extend(origin)
        .extend(destination);
      map.fitBounds(routeBounds, { padding: [60, 60], maxZoom: 16 });
    } catch (err: any) {
      if (err.name !== "AbortError") {
        console.error("Error finding route with VietMap:", err);
        setRouteError(
          err.message || "Could not find a motorbike route to this store.",
        );
      }
    } finally {
      setIsRouting(false);
    }
  };

  // 6. Close store details or cancel route
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
    // Reset view to overview
    handleResetView();
  };

  // Fly smoothly to user location
  const handlePanToUserLocation = () => {
    const map = mapInstanceRef.current;
    if (!map || latitude == null || longitude == null) return;
    map.flyTo([latitude, longitude], 15, { animate: true, duration: 0.8 });
    if (userMarkerRef.current) {
      userMarkerRef.current.openTooltip();
    }
  };

  // Reset view to overview of all stores
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
      {/* GPS Error Alert */}
      {gpsError && (
        <div className="mb-3 flex items-center justify-between rounded-xl border border-amber-200 bg-amber-50 p-2.5 text-xs font-medium text-amber-800">
          <div className="flex items-center gap-2">
            <AlertCircle size={14} className="text-amber-600 shrink-0" />
            <span>{gpsError}</span>
          </div>
          {onRequestGps && (
            <button
              type="button"
              onClick={onRequestGps}
              className="font-bold underline hover:text-amber-900 cursor-pointer"
            >
              Retry GPS
            </button>
          )}
        </div>
      )}

      {/* 1. Location Hub Card: Clearly shows Active Browsing Location & its dual application to Store Map & Flash Deals */}
      <div className="mb-4 overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-2xs">
        {/* Top Row: Location Icon + Status + Full Address + GPS/Home Switcher */}
        <div className="flex flex-col gap-3.5 p-4 sm:flex-row sm:items-center sm:justify-between sm:px-5 sm:py-4 border-b border-neutral-100 bg-neutral-50/50">
          <div className="flex items-center gap-3.5 min-w-0">
            <div
              className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl transition shadow-xs ${
                locationMode === "gps"
                  ? "bg-emerald-600 text-white shadow-emerald-600/20"
                  : "bg-blue-600 text-white shadow-blue-600/20"
              }`}
            >
              {locationMode === "gps" ? (
                <Navigation size={20} className={isGpsLoading ? "animate-spin" : ""} />
              ) : (
                <Home size={20} />
              )}
            </div>

            <div className="min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-[11px] font-bold uppercase tracking-wider text-neutral-400">
                  Active Browsing Location
                </span>
                <span
                  className={`inline-flex items-center gap-1 rounded-md px-2 py-0.5 text-[11px] font-bold ${
                    locationMode === "gps"
                      ? "bg-emerald-100 text-emerald-800"
                      : "bg-blue-100 text-blue-800"
                  }`}
                >
                  {locationMode === "gps" ? (
                    <>
                      <span className="h-1.5 w-1.5 rounded-full bg-emerald-600 animate-pulse" />
                      GPS Active
                    </>
                  ) : (
                    <>
                      <Home size={11} />
                      Home Address
                    </>
                  )}
                </span>
              </div>
              <h3 className="mt-0.5 text-base sm:text-lg font-extrabold text-neutral-900 truncate max-w-md sm:max-w-xl">
                {locationAddress || locationLabel}
              </h3>
            </div>
          </div>

          {/* Location Mode Switcher (GPS vs Home's) */}
          <div className="flex flex-col sm:items-end gap-1 shrink-0">
            <span className="text-[10px] font-bold uppercase tracking-wider text-neutral-400">
              Switch Location Source
            </span>
            {hasDefaultAddress ? (
              <div className="flex items-center rounded-xl border border-neutral-200 bg-white p-1 text-xs shadow-2xs">
                <button
                  type="button"
                  onClick={onRequestGps}
                  disabled={isGpsLoading}
                  className={`inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-bold transition cursor-pointer ${
                    locationMode === "gps"
                      ? "bg-emerald-600 text-white shadow-xs"
                      : "text-neutral-600 hover:text-neutral-900 hover:bg-neutral-50"
                  } disabled:opacity-60`}
                  title="Use real-time GPS location (updates Store Map & Flash Deals)"
                >
                  <Navigation
                    size={12}
                    className={
                      isGpsLoading
                        ? "animate-spin text-white"
                        : locationMode === "gps"
                        ? "text-white"
                        : "text-neutral-500"
                    }
                  />
                  <span>GPS Location</span>
                </button>
                <button
                  type="button"
                  onClick={onSelectSavedAddress}
                  className={`inline-flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-bold transition cursor-pointer ${
                    locationMode === "saved_address"
                      ? "bg-blue-600 text-white shadow-xs"
                      : "text-neutral-600 hover:text-neutral-900 hover:bg-neutral-50"
                  }`}
                  title={`Use saved address for Store Map & Flash Deals: ${userDefaultAddressLabel || "Saved Address"}`}
                >
                  <Home size={12} className={locationMode === "saved_address" ? "text-white" : "text-neutral-500"} />
                  <span className="max-w-[130px] truncate">
                    {userDefaultAddressLabel || "Saved Address"}
                  </span>
                </button>
              </div>
            ) : (
              <button
                type="button"
                onClick={onRequestGps}
                disabled={isGpsLoading}
                className="inline-flex items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3 py-1.5 text-xs font-bold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-60 cursor-pointer"
                title="Use real-time GPS location (updates Store Map & Flash Deals)"
              >
                <Navigation size={12} className={isGpsLoading ? "animate-spin text-emerald-600" : "text-emerald-600"} />
                <span>GPS Location</span>
              </button>
            )}
          </div>
        </div>

        {/* Bottom Row: Dual Application Indicators + Map Canvas Controls */}
        <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-2.5 sm:px-5 text-xs bg-white">
          <div className="flex flex-wrap items-center gap-2 sm:gap-3">
            <span className="text-[11px] font-bold text-neutral-500 uppercase tracking-wide">
              Shared between:
            </span>
            <div className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-50 border border-emerald-200/80 px-2.5 py-1 text-xs font-bold text-emerald-800 shadow-2xs">
              <Store size={13} className="text-emerald-600" />
              <span>1. Nearby Stores Map</span>
            </div>
            <span className="text-neutral-300 font-bold">•</span>
            <div className="inline-flex items-center gap-1.5 rounded-lg bg-amber-50 border border-amber-200/80 px-2.5 py-1 text-xs font-bold text-amber-800 shadow-2xs">
              <Flame size={13} className="text-amber-600" />
              <span>2. Today's Flash Deals</span>
            </div>
            <span className="text-neutral-400 text-[11px] hidden md:inline">
              (within ≤ 10km radius)
            </span>
          </div>

          {/* Map Controls */}
          <div className="flex items-center gap-2 ml-auto">
            <button
              type="button"
              onClick={handlePanToUserLocation}
              className={`inline-flex items-center gap-1.5 rounded-lg border px-3 py-1 text-xs font-bold shadow-2xs transition cursor-pointer ${
                locationMode === "saved_address"
                  ? "border-blue-200 bg-blue-50/80 text-blue-800 hover:bg-blue-100/80"
                  : "border-emerald-200 bg-emerald-50/80 text-emerald-800 hover:bg-emerald-100/80"
              }`}
              title="Focus on active location on map"
            >
              {locationMode === "saved_address" ? (
                <Home size={12} className="text-blue-600" />
              ) : (
                <Navigation size={12} className="text-emerald-600" />
              )}
              <span>My Location</span>
            </button>

            <button
              type="button"
              onClick={() => {
                if (latitude != null && longitude != null) {
                  void fetchShops(latitude, longitude);
                }
                if (onSelectDefaultLocation && locationMode === "default") {
                  onSelectDefaultLocation();
                } else if (onRequestGps && locationMode === "gps") {
                  onRequestGps();
                } else if (onSelectSavedAddress && locationMode === "saved_address") {
                  onSelectSavedAddress();
                } else {
                  handleResetView();
                }
              }}
              disabled={isLoading}
              className="inline-flex items-center gap-1.5 rounded-lg border border-neutral-200 bg-white px-3 py-1 text-xs font-bold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-50 cursor-pointer"
              title="Refresh stores and reset map view"
            >
              <RefreshCw size={12} className={isLoading ? "animate-spin text-emerald-600" : ""} />
              <span>Refresh</span>
            </button>
          </div>
        </div>
      </div>

      {/* 2. Map Section Title & Filter Toggle */}
      <div className="mb-2.5 flex flex-wrap items-center justify-between gap-2.5">
        <div className="flex items-center gap-2">
          <Store size={18} className="text-emerald-600" />
          <h3 className="text-base sm:text-lg font-extrabold text-neutral-900">
            Nearby Stores 
          </h3>
          <span className="text-xs font-semibold text-neutral-500">
            ({storeFilter === "deals_only" ? dealsStoresCount : allStoresCount} store{(storeFilter === "deals_only" ? dealsStoresCount : allStoresCount) === 1 ? "" : "s"} found)
          </span>
        </div>

        {/* Filter Toggle: All Stores vs Flash Deals Only */}
        <div className="flex items-center gap-1 rounded-xl bg-neutral-100 p-1 border border-neutral-200/80 shadow-2xs">
          <button
            type="button"
            onClick={() => handleFilterChange("all")}
            className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-bold transition cursor-pointer ${
              storeFilter === "all"
                ? "bg-white text-neutral-900 shadow-2xs border border-neutral-200/60"
                : "text-neutral-500 hover:text-neutral-800"
            }`}
          >
            <Store
              size={13}
              className={storeFilter === "all" ? "text-emerald-600" : "text-neutral-400"}
            />
            <span>All Stores</span>
            <span
              className={`rounded-full px-1.5 py-0.2 text-[10px] font-extrabold ${
                storeFilter === "all"
                  ? "bg-neutral-100 text-neutral-700"
                  : "text-neutral-400"
              }`}
            >
              {allStoresCount}
            </span>
          </button>

          <button
            type="button"
            onClick={() => handleFilterChange("deals_only")}
            className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1 text-xs font-bold transition cursor-pointer ${
              storeFilter === "deals_only"
                ? "bg-white text-orange-600 shadow-2xs border border-orange-200/60"
                : "text-neutral-500 hover:text-neutral-800"
            }`}
          >
            <Flame
              size={13}
              className={
                storeFilter === "deals_only"
                  ? "text-orange-500 fill-orange-500"
                  : "text-neutral-400"
              }
            />
            <span>Flash Deals Only</span>
            <span
              className={`rounded-full px-1.5 py-0.2 text-[10px] font-extrabold ${
                storeFilter === "deals_only"
                  ? "bg-orange-100 text-orange-700"
                  : "text-neutral-400"
              }`}
            >
              {dealsStoresCount}
            </span>
          </button>
        </div>
      </div>

      {/* 2. VietMap Leaflet Map Container */}
      <div className="relative h-[380px] sm:h-[420px] md:h-[460px] w-full overflow-hidden rounded-3xl border border-neutral-200/90 bg-neutral-100 shadow-2xs">
        {/* Leaflet render container */}
        <div ref={mapContainerRef} className="h-full w-full" style={{ zIndex: 1 }} />

        {/* Loading overlay when fetching shops */}
        {isLoading && (
          <div className="absolute inset-0 z-20 flex items-center justify-center bg-white/60 backdrop-blur-xs">
            <div className="flex items-center gap-2.5 rounded-2xl bg-white px-4 py-2.5 text-xs font-bold text-emerald-800 shadow-md">
              <RefreshCw size={15} className="animate-spin text-emerald-600" />
              <span>Loading nearby stores…</span>
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

        {/* Interaction Hint */}
        {!selectedShop && (
          <div className="pointer-events-none absolute bottom-4 left-4 z-10 hidden sm:block">
            <div className="flex items-center gap-2 rounded-2xl border border-white/80 bg-white/95 px-3.5 py-2 text-xs font-semibold text-neutral-700 shadow-md backdrop-blur-md">
              <Compass size={14} className="text-emerald-600" />
              <span>Tap a store on the map to view details & directions</span>
            </div>
          </div>
        )}

        {/* Floating location button */}
        <button
          type="button"
          onClick={handlePanToUserLocation}
          className="absolute bottom-4 right-4 z-20 flex h-10 w-10 items-center justify-center rounded-2xl border border-neutral-200 bg-white/95 text-neutral-700 shadow-md backdrop-blur-md transition hover:bg-white hover:scale-105 active:scale-95 cursor-pointer"
          title="Focus on my location"
        >
          {locationMode === "saved_address" ? (
            <Home size={18} className="text-blue-600" />
          ) : (
            <Navigation size={18} className="text-emerald-600" />
          )}
        </button>

        {/* 3. STORE DETAIL CARD */}
        {selectedShop && (
          <div className="absolute bottom-4 left-4 right-4 z-20 sm:right-auto sm:max-w-md animate-in fade-in slide-in-from-bottom-3 duration-200">
            <div className="overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-xl backdrop-blur-md">
              {/* Header image & close button */}
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

                {/* Close Button */}
                <button
                  type="button"
                  onClick={handleCloseDetail}
                  className="absolute right-2.5 top-2.5 flex h-7 w-7 items-center justify-center rounded-full bg-black/50 text-white hover:bg-black/80 transition cursor-pointer"
                  title="Close"
                >
                  <X size={15} />
                </button>

                {/* Distance & Open/Closed Badge */}
                <div className="absolute bottom-2.5 left-3 right-3 flex items-center justify-between text-white">
                  <div className="flex items-center gap-1.5 rounded-full bg-black/60 px-2.5 py-0.5 text-[11px] font-bold backdrop-blur-xs">
                    <MapPin size={11} className="text-emerald-400" />
                    <span>{selectedShop.distanceInKm} km away</span>
                  </div>

                  <span
                    className={`rounded-full px-2 py-0.5 text-[10px] font-bold backdrop-blur-xs ${
                      selectedShop.isOpen
                        ? "bg-emerald-600 text-white"
                        : "bg-neutral-800 text-neutral-200"
                    }`}
                  >
                    {selectedShop.isOpen ? "Open" : "Closed"}
                  </span>
                </div>
              </div>

              {/* Details Content */}
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

                  {/* Store Name & Reviews */}
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
                          ? `${selectedShop.totalReviews} reviews`
                          : "No reviews yet"}
                      </span>
                    </div>

                    {/* Deal Count Badge */}
                    <div className="mt-1 flex items-center gap-1.5">
                      {(selectedShop.flashDealCount ?? 0) > 0 ? (
                        <span className="inline-flex items-center gap-1 rounded-md bg-orange-50 border border-orange-200 px-2 py-0.5 text-[11px] font-extrabold text-orange-700">
                          <Flame size={12} className="text-orange-500 fill-orange-500" />
                          <span>
                            {selectedShop.flashDealCount} Flash Deal
                            {(selectedShop.flashDealCount ?? 0) > 1 ? "s" : ""}
                          </span>
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 rounded-md bg-neutral-100 border border-neutral-200 px-2 py-0.5 text-[11px] font-medium text-neutral-500">
                          <span className="h-1.5 w-1.5 rounded-full bg-neutral-400" />
                          <span>No flash deals (0 deals)</span>
                        </span>
                      )}
                    </div>
                  </div>
                </div>

                {/* Address */}
                <p className="mt-2.5 line-clamp-1 text-xs text-neutral-600 flex items-center gap-1.5">
                  <MapPin size={13} className="text-neutral-400 shrink-0" />
                  <span className="truncate">{selectedShop.address}</span>
                </p>

                {/* Operating hours */}
                {selectedShop.openingTime && selectedShop.closingTime && (
                  <p className="mt-1 text-xs text-neutral-500 flex items-center gap-1.5">
                    <Clock size={13} className="text-neutral-400 shrink-0" />
                    <span>
                      {String(selectedShop.openingTime).slice(0, 5)} -{" "}
                      {String(selectedShop.closingTime).slice(0, 5)}
                    </span>
                  </p>
                )}

                {/* 4. ROUTE DIRECTIONS & TRAVEL TIME */}
                {isRouting && (
                  <div className="mt-3 flex items-center gap-2 rounded-xl bg-emerald-50 p-2.5 text-xs font-bold text-emerald-800">
                    <Navigation size={14} className="animate-spin text-emerald-600" />
                    <span>Finding motorbike route & calculating arrival time…</span>
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
                          VietMap Motorcycle Route
                        </div>
                        <div className="mt-0.5 flex items-baseline gap-2">
                          <span className="text-base font-extrabold text-emerald-900">
                            ⏱️ ~{routeResult.durationMinutes} mins
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
                          <span>{showStepDetails ? "Hide steps" : "View steps"}</span>
                          {showStepDetails ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                        </button>
                      )}
                    </div>

                    {/* Step details (Accordion) */}
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

                {/* Action buttons: Directions & View Deals */}
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
                      {routeResult ? "Update Route" : "Get Directions"}
                    </span>
                  </button>

                  <Link
                    to={`/shops/${selectedShop.id}`}
                    className="inline-flex items-center justify-center gap-1 rounded-xl border border-neutral-200 bg-white px-3.5 py-2.5 text-xs font-bold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 hover:text-emerald-700"
                  >
                    <span>
                      {(selectedShop.flashDealCount ?? 0) > 0
                        ? "View Store & Deals"
                        : "View Store"}
                    </span>
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
