export const VIETMAP_CONFIG = {
  TILEMAP_API_KEY:
    (import.meta.env.VITE_VIETMAP_API_KEY as string) ||
    "d027c00b29ec949da72da30a6d3943aa397043d03a4bd14c",
  SERVICES_API_KEY:
    (import.meta.env.VITE_VIETMAP_SERVICES_KEY as string) ||
    "23d5e67ebe9112a717edb7b00a6cb86b77ac04e69ccd0145",
  ROUTE_API_URL: "https://maps.vietmap.vn/api/route/v4",
  STYLE_BASE_URL: "https://maps.vietmap.vn/maps/styles/tm/style.json",
};
