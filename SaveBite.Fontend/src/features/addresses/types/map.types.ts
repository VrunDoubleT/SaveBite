// Minimal interface for the Leaflet/VietMap SDK already loaded by index.html.
export interface MapLatLng {
  lat: number;
  lng: number;
}
export interface MapLayer {
  addTo(map: AddressMapInstance): this;
}
export interface AddressMapInstance {
  getCenter(): MapLatLng;
  getZoom(): number;
  setView(point: [number, number], zoom: number): this;
  hasLayer(layer: MapLayer): boolean;
  on(event: "click", listener: (event: { latlng: MapLatLng }) => void): this;
  invalidateSize(): this;
  remove(): void;
}
export interface AddressMapMarker extends MapLayer {
  getLatLng(): MapLatLng;
  setLatLng(point: [number, number]): this;
  on(event: "dragend", listener: () => void): this;
  remove(): this;
  dragging?: { enable(): void; disable(): void };
}
export interface AddressMapSdk {
  map(
    element: HTMLElement,
    options: {
      center: [number, number];
      zoom: number;
      scrollWheelZoom: boolean;
      maxBounds: [[number, number], [number, number]];
      maxBoundsViscosity: number;
    },
  ): AddressMapInstance;
  marker(
    point: [number, number] | MapLatLng,
    options: {
      draggable: boolean;
      title: string;
      icon: unknown;
    },
  ): AddressMapMarker;
  divIcon(options: {
    className: string;
    html: string;
    iconSize: [number, number];
    iconAnchor: [number, number];
  }): unknown;
  tileLayer(url: string, options: { attribution: string; maxZoom: number }): MapLayer;
  vietmapGL?: (options: { style: unknown }) => MapLayer;
}
