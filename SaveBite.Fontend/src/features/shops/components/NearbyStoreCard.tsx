import { Link } from "react-router-dom";
import { Clock, MapPin, Star, Store, ArrowRight } from "lucide-react";
import type { NearbyShop } from "@/features/shops/types/shop.types";

interface NearbyStoreCardProps {
  shop: NearbyShop;
}

export function NearbyStoreCard({ shop }: NearbyStoreCardProps) {
  const fallbackCover =
    "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800&auto=format&fit=crop&q=80";

  const operatingHours =
    shop.openingTime && shop.closingTime
      ? `${String(shop.openingTime).slice(0, 5)} - ${String(shop.closingTime).slice(0, 5)}`
      : "08:00 - 21:30";

  const reviewCount = shop.totalReviews ?? 0;
  const rating = shop.averageRating > 0 ? shop.averageRating : 4.5;

  return (
    <article className="group flex flex-col overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-2xs transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md">
      {/* Cover Image & Badges */}
      <div className="relative aspect-[16/9] w-full overflow-hidden bg-neutral-900">
        <Link to={`/shops/${shop.id}`} className="block h-full w-full">
          <img
            src={shop.coverImageUrl || fallbackCover}
            alt={shop.name}
            className="h-full w-full object-cover transition duration-300 group-hover:scale-105"
            onError={(e) => {
              e.currentTarget.src = fallbackCover;
            }}
          />
        </Link>
        <div className="absolute inset-0 bg-linear-to-t from-black/40 via-transparent to-transparent pointer-events-none" />

        {/* Distance Badge (Top Left) */}
        {shop.distanceInKm !== undefined && (
          <span className="absolute left-3 top-3 flex items-center gap-1 rounded-full bg-neutral-900/80 px-2.5 py-1 text-xs font-semibold text-white backdrop-blur-xs shadow-xs">
            <MapPin size={11} className="text-emerald-400" />
            <span>{shop.distanceInKm} km</span>
          </span>
        )}

        {/* Status Badge: Open / Closed (Top Right) */}
        <span
          className={`absolute right-3 top-3 flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-bold backdrop-blur-xs shadow-xs ${
            shop.isOpen
              ? "bg-emerald-600 text-white"
              : "bg-neutral-900/85 text-neutral-200"
          }`}
        >
          <span
            className={`h-1.5 w-1.5 rounded-full ${
              shop.isOpen ? "bg-white animate-pulse" : "bg-neutral-400"
            }`}
          />
          <span>{shop.isOpen ? "Open" : "Closed"}</span>
        </span>
      </div>

      {/* Body Information */}
      <div className="flex flex-1 flex-col justify-between p-4">
        <div>
          {/* Avatar + Store Name + Rating Header */}
          <div className="flex items-start gap-3">
            {/* Store Avatar Logo */}
            <div className="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-2xs">
              {shop.logoUrl ? (
                <img
                  src={shop.logoUrl}
                  alt={shop.name}
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="flex h-full w-full items-center justify-center bg-emerald-50 text-emerald-700">
                  <Store size={22} />
                </div>
              )}
            </div>

            {/* Store Name & Rating */}
            <div className="min-w-0 flex-1">
              <Link
                to={`/shops/${shop.id}`}
                className="block text-base font-bold text-neutral-900 leading-snug transition hover:text-emerald-700 line-clamp-2"
                title={shop.name}
              >
                {shop.name}
              </Link>

              {/* Rating row */}
              <div className="mt-1 flex items-center gap-1.5 text-xs text-neutral-500">
                <span className="flex items-center gap-0.5 font-bold text-amber-500">
                  <Star size={12} className="fill-amber-400 text-amber-400" />
                  <span>{rating}</span>
                </span>
                <span>•</span>
                <span>{reviewCount > 0 ? `${reviewCount} reviews` : "No reviews yet"}</span>
              </div>
            </div>
          </div>

          {/* Address */}
          <p className="mt-3 line-clamp-1 text-xs text-neutral-600 flex items-center gap-1.5">
            <MapPin size={13} className="text-neutral-400 shrink-0" />
            <span className="truncate">{shop.address}</span>
          </p>

          {/* Operating Hours */}
          <p className="mt-1 flex items-center gap-1.5 text-xs text-neutral-500">
            <Clock size={13} className="text-neutral-400 shrink-0" />
            <span>{operatingHours}</span>
          </p>
        </div>

        {/* Action Button: View Store & Deals */}
        <div className="mt-4 pt-3 border-t border-neutral-100">
          <Link
            to={`/shops/${shop.id}`}
            className="flex items-center justify-center gap-1.5 rounded-xl bg-emerald-50 px-3 py-2 text-xs font-bold text-emerald-700 transition hover:bg-emerald-600 hover:text-white"
          >
            <span>View Store & Deals</span>
            <ArrowRight size={13} />
          </Link>
        </div>
      </div>
    </article>
  );
}
