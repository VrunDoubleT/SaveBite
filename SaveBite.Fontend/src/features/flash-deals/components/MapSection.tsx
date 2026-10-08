import { MapPin, Navigation, Store } from "lucide-react";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

interface MapSectionProps {
  deals: FlashDeal[];
  coords: { latitude: number; longitude: number };
  radiusInKm: number;
  selectedShop: string | null;
  onSelectShop: (shopName: string | null) => void;
}

export function MapSection({
  deals,
  coords,
  radiusInKm,
  selectedShop,
  onSelectShop,
}: MapSectionProps) {
  // Lọc ra danh sách các cửa hàng duy nhất từ danh sách deals
  const uniqueShops = Array.from(
    new Map(
      deals.map((d) => [
        d.shopId,
        {
          id: d.shopId,
          name: d.shopName,
          address: d.shopAddress,
          distance: d.distanceInKm,
          logo: d.shopLogoUrl,
          dealCount: deals.filter((x) => x.shopId === d.shopId).length,
        },
      ]),
    ).values(),
  );

  return (
    <section className="mb-8 overflow-hidden rounded-2xl border border-neutral-200 bg-linear-to-b from-emerald-50/50 to-white p-6 shadow-xs">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-emerald-800">
            <MapPin size={20} className="text-emerald-600" />
            <h2 className="text-lg font-bold">Cửa hàng có Flash Deal quanh bạn</h2>
          </div>
          <p className="mt-1 text-xs text-neutral-600">
            Tìm thấy {uniqueShops.length} quán trong bán kính {radiusInKm}km từ vị trí của bạn
          </p>
        </div>

        {/* Huy hiệu tọa độ GPS */}
        <div className="flex items-center gap-2 rounded-full border border-emerald-200 bg-white px-3.5 py-1.5 text-xs font-medium text-emerald-800 shadow-2xs">
          <Navigation size={13} className="text-emerald-600" />
          <span>
            {coords.latitude.toFixed(4)}, {coords.longitude.toFixed(4)}
          </span>
        </div>
      </div>

      {/* Danh sách thẻ cửa hàng bấm để lọc */}
      <div className="mt-5 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {uniqueShops.map((shop) => {
          const isSelected = selectedShop === shop.name;
          return (
            <button
              key={shop.id}
              type="button"
              onClick={() => onSelectShop(isSelected ? null : shop.name)}
              className={`flex items-start gap-3 rounded-xl border p-3.5 text-left transition ${
                isSelected
                  ? "border-emerald-600 bg-emerald-50/60 shadow-xs ring-2 ring-emerald-500/20"
                  : "border-neutral-200 bg-white hover:border-emerald-300 hover:shadow-xs"
              }`}
            >
              <div className="relative flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-emerald-100 text-emerald-700">
                {shop.logo ? (
                  <img
                    src={shop.logo}
                    alt={shop.name}
                    className="h-full w-full object-cover"
                    onError={(e) => {
                      e.currentTarget.style.display = "none";
                    }}
                  />
                ) : (
                  <Store size={22} />
                )}
                <span className="absolute -top-1 -right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-amber-500 px-1 text-[9px] font-extrabold text-white">
                  {shop.dealCount}
                </span>
              </div>

              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <h4 className="truncate text-sm font-bold text-neutral-900">{shop.name}</h4>
                  {shop.distance !== undefined && (
                    <span className="shrink-0 text-[11px] font-semibold text-emerald-700">
                      {shop.distance} km
                    </span>
                  )}
                </div>
                <p className="mt-0.5 line-clamp-1 text-xs text-neutral-500">{shop.address}</p>
                <span className="mt-1.5 inline-block text-[11px] font-semibold text-amber-600">
                  {shop.dealCount} suất Flash Deal
                </span>
              </div>
            </button>
          );
        })}
      </div>
    </section>
  );
}
