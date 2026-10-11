import { useEffect, useRef, useState } from "react";
import type {
  AddressMapInstance,
  AddressMapMarker,
  AddressMapSdk,
  MapLatLng,
} from "@/features/addresses/types/map.types";
import { VIETMAP_CONFIG } from "@/features/shops/config/vietmap.config";
import { getVietmapStyle } from "@/features/shops/utils/vietmapUtils";
import type { AddressCoordinates } from "@/features/addresses/utils/addressLocation";

interface Props {
  point: AddressCoordinates | null;
  disabled: boolean;
  onSelect: (point: AddressCoordinates) => void;
}

export function AddressLocationMap({ point, disabled, onSelect }: Props) {
  const container = useRef<HTMLDivElement>(null);
  const mapRef = useRef<AddressMapInstance | null>(null);
  const markerRef = useRef<AddressMapMarker | null>(null);
  const initialPoint = useRef(point);
  const latest = useRef({ point, disabled, onSelect });
  const [mapError, setMapError] = useState<string | null>(null);

  useEffect(() => {
    latest.current = { point, disabled, onSelect };
  }, [point, disabled, onSelect]);

  useEffect(() => {
    const sdk = (window as unknown as { L?: AddressMapSdk }).L;
    if (!container.current) return;
    if (!sdk) {
      // Report after mounting; manual coordinate inputs remain available.
      const timer = setTimeout(
        () =>
          setMapError(
            "The map could not load. Check your connection and reopen the form, or enter coordinates manually.",
          ),
        0,
      );
      return () => clearTimeout(timer);
    }
    let disposed = false;
    const start = initialPoint.current;
    const map = sdk.map(container.current, {
      center: start ? [start.latitude, start.longitude] : [10.7769, 106.7009],
      zoom: start ? 17 : 6,
      scrollWheelZoom: false,
      maxBounds: [
        [-85, -180],
        [85, 180],
      ],
      maxBoundsViscosity: 1,
    });
    mapRef.current = map;
    const marker = sdk.marker(start ? [start.latitude, start.longitude] : map.getCenter(), {
      draggable: true,
      title: "Drag to correct your address location",
      icon: sdk.divIcon({
        className: "",
        html: '<div style="width:26px;height:26px;border:3px solid white;border-radius:50% 50% 50% 0;background:#059669;transform:rotate(-45deg);box-shadow:0 2px 7px #0005"></div>',
        iconSize: [26, 26],
        iconAnchor: [13, 26],
      }),
    });
    markerRef.current = marker;
    if (start) marker.addTo(map);
    const choose = (latlng: MapLatLng) => {
      if (latest.current.disabled) return;
      latest.current.onSelect({ latitude: latlng.lat, longitude: latlng.lng });
    };
    map.on("click", (event: { latlng: MapLatLng }) => choose(event.latlng));
    marker.on("dragend", () => choose(marker.getLatLng()));

    const fallback = () => {
      if (disposed) return;
      sdk
        .tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
          attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
          maxZoom: 19,
        })
        .addTo(map);
      setMapError(
        "VietMap background is unavailable. A backup map is shown; you can still choose your location.",
      );
    };
    void (async () => {
      try {
        if (!sdk.vietmapGL || !VIETMAP_CONFIG.TILEMAP_API_KEY) {
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
      markerRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    const marker = markerRef.current;
    if (!map || !marker) return;
    if (point) {
      marker.setLatLng([point.latitude, point.longitude]);
      if (!map.hasLayer(marker)) marker.addTo(map);
      map.setView([point.latitude, point.longitude], Math.max(map.getZoom(), 16));
    } else marker.remove();
    if (disabled) marker.dragging?.disable();
    else marker.dragging?.enable();
  }, [point?.latitude, point?.longitude, point, disabled]);

  return (
    <div className="space-y-3">
      <p className="text-sm text-text-secondary">
        Click the map or drag the pin to correct your location. The address fields update
        automatically.
      </p>
      {mapError && (
        <p role="status" className="text-sm text-text-secondary">
          {mapError}
        </p>
      )}
      <div
        ref={container}
        role="region"
        aria-label="Address location map"
        className="relative z-0 h-72 w-full overflow-hidden rounded-xl border border-border-default sm:h-80"
      />
      <button
        type="button"
        disabled={disabled}
        onClick={() => {
          const center = mapRef.current?.getCenter();
          if (center) onSelect({ latitude: center.lat, longitude: center.lng });
        }}
        className="rounded-full border border-border-default px-4 py-2 text-sm font-semibold text-text-secondary hover:bg-bg-subtle disabled:opacity-60"
      >
        Use map center
      </button>
      {!point && (
        <p className="text-xs text-text-muted">
          Choose a point to set your coordinates. The initial map center is not your detected
          location.
        </p>
      )}
    </div>
  );
}
