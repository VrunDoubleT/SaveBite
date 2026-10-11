import { VIETMAP_CONFIG } from "@/features/shops/config/vietmap.config";

export interface AddressCoordinates {
  latitude: number;
  longitude: number;
}

export interface ResolvedAddress {
  addressLine: string;
  ward: string;
  district: string;
  city: string;
}

export function parseCoordinates(latitude: string, longitude: string): AddressCoordinates | null {
  if (!latitude.trim() || !longitude.trim()) return null;
  const lat = Number(latitude);
  const lng = Number(longitude);
  return Number.isFinite(lat) &&
    Number.isFinite(lng) &&
    lat >= -90 &&
    lat <= 90 &&
    lng >= -180 &&
    lng <= 180
    ? { latitude: lat, longitude: lng }
    : null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function text(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

/** VietMap Reverse v4: name is the street address; boundaries carry administrative fields. */
export function parseVietmapAddress(data: unknown): ResolvedAddress {
  if (!Array.isArray(data) || !isRecord(data[0])) {
    throw new Error("No address was found at this point. Please enter the address manually.");
  }
  const place = data[0];
  const boundaries = Array.isArray(place.boundaries) ? place.boundaries.filter(isRecord) : [];
  const boundary = (type: number) => {
    const item = boundaries.find((value) => value.type === type);
    return item
      ? text(item.full_name) || [text(item.prefix), text(item.name)].filter(Boolean).join(" ")
      : "";
  };
  const resolved = {
    addressLine: text(place.name),
    ward: boundary(2),
    district: boundary(1),
    city: boundary(0),
  };
  if (!Object.values(resolved).some(Boolean)) {
    throw new Error("No address was found at this point. Please enter the address manually.");
  }
  return resolved;
}

export async function reverseGeocodeVietmap(
  point: AddressCoordinates,
  signal: AbortSignal,
): Promise<ResolvedAddress> {
  const key = VIETMAP_CONFIG.SERVICES_API_KEY.trim();
  if (!key || key.startsWith("your_")) {
    throw new Error("Address lookup is unavailable. Please enter the address manually.");
  }
  const url = new URL("https://maps.vietmap.vn/api/reverse/v4");
  url.searchParams.set("apikey", key);
  url.searchParams.set("lat", String(point.latitude));
  url.searchParams.set("lng", String(point.longitude));
  // Existing saved-address fields use ward / district / city.
  url.searchParams.set("display_type", "2");
  const response = await fetch(url, { signal, headers: { Accept: "application/json" } });
  if (!response.ok) {
    throw new Error(
      response.status === 429
        ? "Address lookup is busy. Try again shortly or enter the address manually."
        : "Unable to look up this address. Check your connection or enter the address manually.",
    );
  }
  return parseVietmapAddress(await response.json());
}

export function geolocationErrorMessage(code: number): string {
  switch (code) {
    case 1:
      return "Location permission was denied. Allow location access in your browser or choose a point on the map.";
    case 2:
      return "Your location is unavailable. Try again or choose a point on the map.";
    case 3:
      return "Location lookup timed out. Try again or choose a point on the map.";
    default:
      return "Unable to get your location. Choose a point on the map or enter coordinates manually.";
  }
}
