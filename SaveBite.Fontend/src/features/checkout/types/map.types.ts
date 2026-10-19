import type { AddressMapInstance, AddressMapSdk } from "@/features/addresses/types/map.types";

export interface CheckoutMapInstance extends AddressMapInstance {
  fitBounds(bounds: unknown, options: { padding: [number, number]; maxZoom: number }): this;
}
interface RouteLayer {
  addTo(target: AddressMapInstance | RouteLayerGroup): this;
}
export interface RouteLayerGroup extends RouteLayer {
  clearLayers(): this;
}
interface RouteMarker extends RouteLayer {
  bindTooltip(text: string): this;
}
export type CheckoutMapSdk = Omit<AddressMapSdk, "map" | "marker"> & {
  map(
    element: HTMLElement,
    options: { center: [number, number]; zoom: number; scrollWheelZoom: boolean },
  ): CheckoutMapInstance;
  layerGroup(): RouteLayerGroup;
  marker(
    point: [number, number],
    options: { title: string; keyboard: boolean; draggable: boolean; icon: unknown },
  ): RouteMarker;
  polyline(
    points: [number, number][],
    options: { color: string; weight: number; opacity: number },
  ): RouteLayer;
  circle(
    point: [number, number],
    options: {
      radius: number;
      color: string;
      weight: number;
      fillOpacity: number;
      interactive: boolean;
    },
  ): RouteLayer;
  latLngBounds(points: [number, number][]): unknown;
};
