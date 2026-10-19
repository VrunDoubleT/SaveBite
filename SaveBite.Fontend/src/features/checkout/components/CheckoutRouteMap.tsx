import { useEffect, useMemo, useRef, useState } from "react";
import type { CheckoutMapInstance, CheckoutMapSdk, RouteLayerGroup } from "../types/map.types";
import type { CartShop } from "@/features/cart/types/cart.types";
import type { CheckoutGps, CheckoutRoute } from "../types/checkout.types";
import { decodePolyline, getVietmapStyle } from "@/features/shops/utils/vietmapUtils";

function validPoint(latitude: unknown, longitude: unknown): boolean {
  return (
    typeof latitude === "number" &&
    typeof longitude === "number" &&
    Number.isFinite(latitude) &&
    Number.isFinite(longitude) &&
    Math.abs(latitude) <= 90 &&
    Math.abs(longitude) <= 180
  );
}
export function CheckoutRouteMap({
  shop,
  gps,
  route,
  loading,
}: {
  shop: CartShop;
  gps: CheckoutGps | null;
  route?: CheckoutRoute;
  loading: boolean;
}) {
  const latitude = shop.latitude ?? route?.shopLatitude;
  const longitude = shop.longitude ?? route?.shopLongitude;
  if (!validPoint(latitude, longitude))
    return (
      <p className="mt-3 text-xs text-neutral-500">
        The shop location is unavailable. Refresh checkout to load the map.
      </p>
    );
  return (
    <RouteMap
      key={shop.shopId}
      latitude={latitude!}
      longitude={longitude!}
      gps={gps}
      route={route}
      loading={loading}
    />
  );
}
function RouteMap({
  latitude,
  longitude,
  gps,
  route,
  loading,
}: {
  latitude: number;
  longitude: number;
  gps: CheckoutGps | null;
  route?: CheckoutRoute;
  loading: boolean;
}) {
  const container = useRef<HTMLDivElement>(null);
  const mapRef = useRef<CheckoutMapInstance | null>(null);
  const overlays = useRef<RouteLayerGroup | null>(null);
  const start = useRef({ latitude, longitude });
  const [mapError, setMapError] = useState<string | null>(null);
  const [backgroundError, setBackgroundError] = useState(false);
  const geometry = useMemo(() => {
    if (
      !route ||
      !gps ||
      route.originLatitude !== gps.latitude ||
      route.originLongitude !== gps.longitude ||
      route.shopLatitude !== latitude ||
      route.shopLongitude !== longitude
    )
      return null;
    try {
      const points = decodePolyline(route.encodedPolyline);
      return points.length >= 2 && points.every(([lat, lng]) => validPoint(lat, lng))
        ? points
        : null;
    } catch {
      return null;
    }
  }, [route, gps, latitude, longitude]);
  useEffect(() => {
    const sdk = (window as unknown as { L?: CheckoutMapSdk }).L;
    if (!container.current || !sdk) {
      const timer = setTimeout(
        () =>
          setMapError(
            "The map could not load. You can still use GPS and review your pickup times.",
          ),
        0,
      );
      return () => clearTimeout(timer);
    }
    let disposed = false;
    const map = sdk.map(container.current, {
      center: [start.current.latitude, start.current.longitude],
      zoom: 16,
      scrollWheelZoom: false,
    });
    mapRef.current = map;
    overlays.current = sdk.layerGroup().addTo(map);
    const fallback = () => {
      if (disposed) return;
      sdk
        .tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
          maxZoom: 19,
          attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        })
        .addTo(map);
      setBackgroundError(true);
    };
    void (async () => {
      try {
        if (!sdk.vietmapGL) {
          fallback();
          return;
        }
        const style = await getVietmapStyle();
        if (!disposed) sdk.vietmapGL({ style }).addTo(map);
      } catch {
        fallback();
      }
    })();
    const observer = new ResizeObserver(() => map.invalidateSize());
    observer.observe(container.current);
    return () => {
      disposed = true;
      observer.disconnect();
      map.remove();
      mapRef.current = null;
      overlays.current = null;
    };
  }, []);
  useEffect(() => {
    const sdk = (window as unknown as { L?: CheckoutMapSdk }).L;
    const map = mapRef.current,
      layers = overlays.current;
    if (!sdk || !map || !layers) return;
    layers.clearLayers();
    const marker = (point: [number, number], color: string, title: string, label: string) =>
      sdk
        .marker(point, {
          title,
          keyboard: true,
          draggable: false,
          icon: sdk.divIcon({
            className: "",
            html: `<div style="height:28px;width:28px;border:3px solid white;border-radius:50%;background:${color};color:white;display:flex;align-items:center;justify-content:center;box-shadow:0 1px 6px #0005;font-size:11px;font-weight:700">${label}</div>`,
            iconSize: [28, 28],
            iconAnchor: [14, 14],
          }),
        })
        .bindTooltip(title)
        .addTo(layers);
    marker([latitude, longitude], "#059669", "Pickup shop", "S");
    const points: [number, number][] = [[latitude, longitude]];
    if (gps && validPoint(gps.latitude, gps.longitude)) {
      points.push([gps.latitude, gps.longitude]);
      if (geometry) {
        sdk.polyline(geometry, { color: "#059669", weight: 5, opacity: 0.9 }).addTo(layers);
        points.push(...geometry);
      }
      sdk
        .circle([gps.latitude, gps.longitude], {
          radius: gps.accuracyMeters,
          color: "#2563eb",
          weight: 1,
          fillOpacity: 0.08,
          interactive: false,
        })
        .addTo(layers);
      marker([gps.latitude, gps.longitude], "#2563eb", "Your GPS location", "Y");
      map.fitBounds(sdk.latLngBounds(points), { padding: [28, 28], maxZoom: 16 });
    } else map.setView([latitude, longitude], 16);
  }, [latitude, longitude, gps, geometry]);
  return (
    <div className="mt-4 space-y-2">
      <div
        ref={container}
        role="region"
        aria-label="Route from your location to the shop"
        className="relative z-0 h-56 w-full overflow-hidden rounded-lg border border-neutral-200 sm:h-64"
      />
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-neutral-500">
        <span className="inline-flex items-center gap-1.5">
          <span className="size-2 rounded-full bg-blue-600" />
          Your GPS location
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span className="size-2 rounded-full bg-emerald-600" />
          Pickup shop
        </span>
        {geometry && (
          <span className="inline-flex items-center gap-1.5">
            <span className="h-0.5 w-4 bg-emerald-600" />
            Motorcycle route
          </span>
        )}
      </div>
      <p role="status" className="text-xs text-neutral-500">
        {mapError ??
          (loading
            ? "Finding your location and motorcycle route…"
            : !gps
              ? "Enable GPS above to see the route from your current location."
              : !geometry
                ? "A route preview is not available yet."
                : "Motorcycle route from your current location to the pickup shop.")}
      </p>
      {backgroundError && (
        <p className="text-xs text-neutral-500">
          A backup map is shown because the VietMap background is unavailable.
        </p>
      )}
    </div>
  );
}
