import { Navigation, MapPin, Home, AlertCircle, Loader2, Building2, RotateCcw } from "lucide-react";
import type { LocationMode } from "@/features/flash-deals/hooks/useNearbyFlashDeals";
import type { UserAddress } from "@/features/auth/types/auth.types";

interface LocationBarProps {
  locationMode: LocationMode | null;
  locationLabel: string;
  locationAddress?: string | null;
  coords?: { latitude: number; longitude: number } | null;
  isGpsLoading: boolean;
  gpsError: string | null;
  onRequestGps: () => void;
  onSelectSavedAddress: () => void;
  onSelectDefaultLocation: () => void;
  userDefaultAddress?: UserAddress | null;
  isAuthenticated?: boolean;
}

export function LocationBar({
  locationMode,
  locationLabel,
  locationAddress,
  coords,
  isGpsLoading,
  gpsError,
  onRequestGps,
  onSelectSavedAddress,
  onSelectDefaultLocation,
  userDefaultAddress,
  isAuthenticated,
}: LocationBarProps) {
  return (
    <section className="mb-6 rounded-2xl border border-neutral-200/90 bg-white p-3.5 sm:px-5 sm:py-3.5 shadow-2xs transition-all hover:border-neutral-300">
      <div className="flex flex-col gap-3.5 sm:flex-row sm:items-center sm:justify-between">
        {/* Left: Active Location Information */}
        <div className="flex items-center gap-3.5 min-w-0">
          <div
            className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl transition-colors ${
              locationMode === "gps"
                ? "bg-emerald-600 text-white shadow-xs shadow-emerald-600/25"
                : locationMode === "saved_address"
                ? "bg-blue-600 text-white shadow-xs shadow-blue-600/25"
                : locationMode === "default"
                ? "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/15"
                : "bg-amber-50 text-amber-600 ring-1 ring-amber-500/20"
            }`}
          >
            {locationMode === "gps" ? (
              <Navigation size={18} className={isGpsLoading ? "animate-spin" : "animate-pulse"} />
            ) : locationMode === "saved_address" ? (
              <Home size={18} />
            ) : (
              <MapPin size={18} className={!locationMode ? "animate-bounce" : ""} />
            )}
          </div>

          <div className="min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-[11px] font-bold uppercase tracking-wider text-neutral-400">
                Browsing Location
              </span>
              <span
                className={`inline-flex items-center gap-1 rounded-md px-2 py-0.5 text-[11px] font-semibold ${
                  locationMode === "gps"
                    ? "bg-emerald-100 text-emerald-800"
                    : locationMode === "saved_address"
                    ? "bg-blue-100 text-blue-800"
                    : locationMode === "default"
                    ? "bg-neutral-100 text-neutral-700"
                    : "bg-amber-100 text-amber-800"
                }`}
              >
                {locationMode === "gps" && (
                  <>
                    <span className="h-1.5 w-1.5 rounded-full bg-emerald-600 animate-pulse" />
                    GPS Active
                  </>
                )}
                {locationMode === "saved_address" && (
                  <>
                    <Home size={11} />
                    Saved Address
                  </>
                )}
                {locationMode === "default" && (
                  <>
                    <Building2 size={11} className="text-neutral-500" />
                    Default Location
                  </>
                )}
                {locationMode === null && (
                  <>
                    <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
                    GPS Inactive
                  </>
                )}
              </span>
            </div>
            <h3 className="mt-0.5 text-sm sm:text-base font-bold text-neutral-900 truncate max-w-md sm:max-w-xl">
              {locationAddress || locationLabel}
            </h3>
            {coords && (
              <p className="text-[11px] font-medium text-emerald-700/80 flex items-center gap-1 mt-0.5">
                <span>📍</span>
                <span>Coordinates: {coords.latitude.toFixed(4)}°N, {coords.longitude.toFixed(4)}°E</span>
              </p>
            )}
          </div>
        </div>

        {/* Right: Location Selection Actions */}
        <div className="flex flex-wrap items-center gap-2 sm:shrink-0">
          {/* 1. Nút chọn GPS hiện tại */}
          <button
            type="button"
            onClick={onRequestGps}
            disabled={isGpsLoading}
            className={`inline-flex h-9 items-center gap-1.5 rounded-xl px-3.5 text-xs font-bold transition shadow-2xs cursor-pointer ${
              locationMode === "gps"
                ? "border border-emerald-300 bg-emerald-50 text-emerald-800 ring-2 ring-emerald-500/20"
                : "border border-neutral-200 bg-white text-neutral-700 hover:border-emerald-300 hover:bg-emerald-50/50 hover:text-emerald-700"
            } disabled:opacity-60`}
            title="Sử dụng định vị GPS thực tế của bạn"
          >
            {isGpsLoading ? (
              <Loader2 size={13} className="animate-spin text-emerald-600" />
            ) : (
              <Navigation size={13} className={locationMode === "gps" ? "text-emerald-600" : "text-neutral-500"} />
            )}
            <span>{locationMode === "gps" ? "GPS hiện tại (Đang dùng)" : "Dùng GPS hiện tại"}</span>
          </button>

          {/* 2. Nút chọn Địa chỉ mặc định (khi user có địa chỉ lưu) */}
          {userDefaultAddress ? (
            <button
              type="button"
              onClick={onSelectSavedAddress}
              className={`inline-flex h-9 items-center gap-1.5 rounded-xl px-3.5 text-xs font-bold transition shadow-2xs cursor-pointer ${
                locationMode === "saved_address"
                  ? "border border-blue-300 bg-blue-50 text-blue-800 ring-2 ring-blue-500/20"
                  : "border border-neutral-200 bg-white text-neutral-700 hover:border-blue-300 hover:bg-blue-50/50 hover:text-blue-700"
              }`}
              title={`Sử dụng địa chỉ mặc định đã lưu: ${userDefaultAddress.addressLine}`}
            >
              <Home size={13} className={locationMode === "saved_address" ? "text-blue-600" : "text-neutral-500"} />
              <span className="truncate max-w-[170px]">
                {locationMode === "saved_address"
                  ? "Địa chỉ mặc định (Đang dùng)"
                  : userDefaultAddress.label
                  ? `Địa chỉ: ${userDefaultAddress.label}`
                  : "Dùng địa chỉ mặc định"}
              </span>
            </button>
          ) : isAuthenticated ? (
            <span
              className="inline-flex h-9 items-center gap-1.5 rounded-xl border border-dashed border-neutral-300 bg-neutral-50/80 px-3 text-xs text-neutral-400"
              title="Bạn chưa lưu địa chỉ mặc định trong tài khoản"
            >
              <Home size={12} className="text-neutral-400" />
              <span>Chưa lưu địa chỉ</span>
            </span>
          ) : null}

          {/* 3. Nút Làm mới vị trí */}
          <button
            type="button"
            onClick={onSelectDefaultLocation}
            className="inline-flex h-9 items-center gap-1.5 rounded-xl border border-neutral-200 bg-white px-3 text-xs font-medium text-neutral-600 hover:border-neutral-300 hover:bg-neutral-50 hover:text-neutral-900 transition shadow-2xs cursor-pointer"
            title="Làm mới lại vị trí"
          >
            <RotateCcw size={12} />
            <span className="hidden sm:inline">Làm mới</span>
          </button>
        </div>
      </div>

      {/* GPS Error Alert */}
      {gpsError && (
        <div className="mt-3 flex items-center gap-2 rounded-xl border border-amber-200 bg-amber-50 p-2.5 text-xs font-medium text-amber-800">
          <AlertCircle size={14} className="text-amber-600 shrink-0" />
          <span className="flex-1">{gpsError}</span>
          <button
            type="button"
            onClick={onRequestGps}
            className="font-bold underline hover:text-amber-900 cursor-pointer"
          >
            Retry GPS
          </button>
        </div>
      )}
    </section>
  );
}
