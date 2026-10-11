import { VIETMAP_CONFIG } from "../config/vietmap.config";

export interface RouteInstruction {
  text: string;
  distance?: number;
  time?: number;
}

export interface RouteResult {
  coordinates: [number, number][];
  distanceMeters: number;
  distanceFormatted: string;
  durationMinutes: number;
  instructions: string[];
}

/**
 * Decode VietMap encoded polyline5 into array of [lat, lng] coordinates for Leaflet
 */
export function decodePolyline(encoded: string): [number, number][] {
  let index = 0;
  let lat = 0;
  let lng = 0;
  const points: [number, number][] = [];

  function readDelta(): number {
    let result = 0;
    let shift = 0;
    let byte: number;
    do {
      if (index >= encoded.length || shift > 30) {
        throw new Error("Invalid route polyline data.");
      }
      byte = encoded.charCodeAt(index++) - 63;
      if (byte < 0 || byte > 63) {
        throw new Error("Invalid route polyline data.");
      }
      result |= (byte & 31) << shift;
      shift += 5;
    } while (byte >= 32);
    return result & 1 ? ~(result >> 1) : result >> 1;
  }

  while (index < encoded.length) {
    lat += readDelta();
    lng += readDelta();
    points.push([lat / 1e5, lng / 1e5]);
  }
  return points;
}

/**
 * Fetch Motorcycle route from VietMap Route API v4
 */
export async function fetchVietmapRoute(
  origin: [number, number],
  destination: [number, number],
  servicesKey: string = VIETMAP_CONFIG.SERVICES_API_KEY,
  signal?: AbortSignal,
): Promise<RouteResult> {
  const url = new URL(VIETMAP_CONFIG.ROUTE_API_URL);
  url.searchParams.set("apikey", servicesKey.trim());
  url.searchParams.append("point", `${origin[0]},${origin[1]}`);
  url.searchParams.append("point", `${destination[0]},${destination[1]}`);
  url.searchParams.set("vehicle", "motorcycle");
  url.searchParams.set("points_encoded", "true");

  const response = await fetch(url.href, { signal });
  if (!response.ok) {
    if (response.status === 423) {
      throw new Error(
        "VietMap Services key is not authorized for Route API or quota exceeded.",
      );
    }
    if (response.status === 401 || response.status === 403) {
      throw new Error(
        "Invalid Services key or route feature is not activated.",
      );
    }
    throw new Error(`Could not find route (HTTP ${response.status}).`);
  }

  const data = await response.json();
  if (data.code && data.code !== "OK") {
    const errorDict: Record<string, string> = {
      ZERO_RESULTS: "No route found between these locations.",
      OVER_DAILY_LIMIT: "VietMap Route API daily quota exceeded.",
      INVALID_REQUEST: "Invalid coordinates or API key.",
    };
    throw new Error(errorDict[data.code] || `VietMap route error: ${data.code}`);
  }

  const path = data.paths && data.paths[0];
  if (!path || typeof path.points !== "string") {
    throw new Error("No valid route data from VietMap.");
  }

  const coordinates = decodePolyline(path.points);
  if (coordinates.length < 2) {
    throw new Error("Could not draw route to this store.");
  }

  const distanceMeters = Math.round(path.distance || 0);
  const distanceFormatted =
    distanceMeters < 1000
      ? `${distanceMeters} m`
      : `${(distanceMeters / 1000).toFixed(1)} km`;

  const durationMinutes = Math.max(1, Math.ceil((path.time || 0) / 60000));

  const instructions = (path.instructions || [])
    .map((item: { text?: string }) => item.text?.trim())
    .filter(Boolean) as string[];

  return {
    coordinates,
    distanceMeters,
    distanceFormatted,
    durationMinutes,
    instructions,
  };
}

/**
 * Fetch and prepare VietMap vector tile style JSON
 */
export async function getVietmapStyle(
  tilemapKey: string = VIETMAP_CONFIG.TILEMAP_API_KEY,
): Promise<any> {
  const styleUrl = `${VIETMAP_CONFIG.STYLE_BASE_URL}?apikey=${encodeURIComponent(tilemapKey.trim())}`;
  const response = await fetch(styleUrl);
  if (!response.ok) {
    throw new Error(`Failed to load basemap: HTTP ${response.status}`);
  }
  const style = await response.json();
  if (!Array.isArray(style.layers) || !style.sources) {
    throw new Error("Invalid VietMap map style.");
  }

  const absoluteUrl = (value: string) =>
    new URL(value, styleUrl).href.replace(/%7B/gi, "{").replace(/%7D/gi, "}");

  if (typeof style.sprite === "string") {
    style.sprite = absoluteUrl(style.sprite);
  } else if (Array.isArray(style.sprite)) {
    style.sprite = style.sprite.map((s: { url: string }) => ({
      ...s,
      url: absoluteUrl(s.url),
    }));
  }

  if (typeof style.glyphs === "string") {
    style.glyphs = absoluteUrl(style.glyphs);
  }

  Object.values(style.sources as Record<string, any>).forEach((source: any) => {
    if (typeof source.url === "string") source.url = absoluteUrl(source.url);
    if (Array.isArray(source.tiles)) source.tiles = source.tiles.map(absoluteUrl);
  });

  // Retain geographic/natural labels, filter out clutter utility POIs
  const geographicPoiLayers = new Set([
    "water_point",
    "poiz18_beach",
    "park_label",
    "poiz12_park",
  ]);

  style.layers = style.layers.filter((layer: any) => {
    if (geographicPoiLayers.has(layer.id)) return true;
    const sourceLayer = layer["source-layer"] || "";
    if (sourceLayer !== "poi") return true;
    return false;
  });

  return style;
}
