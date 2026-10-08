import { useEffect, useState } from "react";
import { Store, MapPin, RefreshCw, AlertCircle } from "lucide-react";
import { shopApi } from "@/features/shops/api/shopApi";
import { NearbyStoreCard } from "@/features/shops/components/NearbyStoreCard";
import type { NearbyShop } from "@/features/shops/types/shop.types";

interface NearbyStoresSectionProps {
  latitude?: number;
  longitude?: number;
  radiusInKm?: number;
  isLocationReady?: boolean;
}

export function NearbyStoresSection({
  latitude = 10.7626,
  longitude = 106.6601,
  radiusInKm = 15,
  isLocationReady = true,
}: NearbyStoresSectionProps) {
  const [shops, setShops] = useState<NearbyShop[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchShops = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await shopApi.getNearbyShops({
        latitude,
        longitude,
        radiusInKm,
      });
      setShops(res.shops);
    } catch {
      setError("Failed to load nearby stores.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (!isLocationReady) return;
    void fetchShops();
  }, [isLocationReady, latitude, longitude, radiusInKm]);

  const showSkeleton = !isLocationReady || isLoading;

  return (
    <section className="mb-12">
      {/* Section Header */}
      <div className="mb-5 flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700">
              <Store size={18} />
            </div>
            <h2 className="text-xl font-extrabold tracking-tight text-neutral-900 sm:text-2xl">
              Nearby Stores
            </h2>
          </div>
          <p className="mt-1 text-xs text-neutral-500 sm:text-sm">
            Discover stores and restaurants with active flash deals near you
          </p>
        </div>

        <button
          type="button"
          onClick={() => void fetchShops()}
          disabled={isLoading}
          className="inline-flex items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3 py-1.5 text-xs font-semibold text-neutral-700 shadow-2xs transition hover:bg-neutral-50 disabled:opacity-50"
        >
          <RefreshCw size={13} className={isLoading ? "animate-spin text-emerald-600" : ""} />
          <span className="hidden sm:inline">Refresh</span>
        </button>
      </div>

      {/* Loading Skeleton */}
      {showSkeleton && (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 md:grid-cols-3">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="animate-pulse rounded-2xl border border-neutral-200 bg-white p-4"
            >
              <div className="aspect-[16/9] w-full rounded-xl bg-neutral-200" />
              <div className="mt-4 flex items-center gap-3">
                <div className="h-10 w-10 rounded-xl bg-neutral-200" />
                <div className="space-y-1.5 flex-1">
                  <div className="h-4 w-3/4 rounded bg-neutral-200" />
                  <div className="h-3 w-1/2 rounded bg-neutral-200" />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Error state */}
      {!showSkeleton && error && (
        <div className="flex items-center gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-4 text-xs font-medium text-amber-800">
          <AlertCircle size={16} className="text-amber-600 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Empty State */}
      {!showSkeleton && !error && shops.length === 0 && (
        <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-neutral-200 bg-white px-6 py-12 text-center shadow-xs">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
            <MapPin size={24} />
          </div>
          <h4 className="mt-3 text-sm font-bold text-neutral-800">
            No stores found within {radiusInKm} km radius
          </h4>
          <p className="mt-1 text-xs text-neutral-500 max-w-sm">
            Try expanding your search radius or check your location settings.
          </p>
        </div>
      )}

      {/* Stores Grid */}
      {!showSkeleton && !error && shops.length > 0 && (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {shops.map((shop) => (
            <NearbyStoreCard key={shop.id} shop={shop} />
          ))}
        </div>
      )}
    </section>
  );
}
