import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Package, ShoppingBag, Sparkles, Star } from "lucide-react";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

interface DealCardProps {
  deal: FlashDeal;
  isSameShop?: boolean; // flag to distinguish Home page vs Shop page usage
}

// Format currency
const formatVnd = (value: number) => {
  return Number.isFinite(value) ? `${Math.round(value).toLocaleString("vi-VN")} VND` : "—";
};

export function DealCard({ deal, isSameShop = false }: DealCardProps) {
  // 1. Calculate quantities
  const totalQty = deal.variants.reduce((sum, v) => sum + (v.totalQuantity || 0), 0) || 1;
  const soldQty = deal.variants.reduce((sum, v) => sum + (v.soldQuantity || 0), 0);
  const remainingQty = deal.variants.reduce((sum, v) => sum + (v.availableQuantity || 0), 0);

  const soldPct = Math.min(100, Math.round((soldQty / totalQty) * 100));
  const isSoldOut = remainingQty <= 0;

  // 2. Countdown timer calculation
  const getTimeLeft = () => {
    const diff = new Date(deal.orderEndTime).getTime() - Date.now();
    if (diff <= 0) return "Ended";
    const min = Math.floor(diff / 60000);
    const h = Math.floor(min / 60);
    const m = min % 60;
    return h > 0 ? `Ends in ${h}h ${m}m` : `Ends in ${m}m`;
  };

  const [timeLeft, setTimeLeft] = useState(getTimeLeft);

  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft(getTimeLeft());
    }, 30000);
    return () => clearInterval(timer);
  }, [deal.orderEndTime]);

  const fallbackImg = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&auto=format&fit=crop&q=80";

  return (
    <article className="group relative flex flex-col overflow-hidden rounded-2xl border border-neutral-200 bg-white shadow-xs transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md">
      {/* Product Image + Badges */}
      <div className="relative aspect-[4/3] w-full overflow-hidden bg-neutral-100">
        <Link to={`/flash-deals/${deal.id}`}>
          <img
            src={deal.productImageUrl || fallbackImg}
            alt={deal.productName}
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
            onError={(e) => {
              e.currentTarget.src = fallbackImg;
            }}
          />
        </Link>

        {/* Góc trên bên trái: Xếp dọc ngăn nắp, KHÔNG BAO GIỜ bị đè chữ */}
        <div className="absolute top-3 left-3 z-10 flex flex-col items-start gap-1.5">
          {deal.isNew && (
            <span className="inline-flex items-center gap-1 rounded-full bg-gradient-to-r from-red-600 via-orange-500 to-amber-500 px-2.5 py-1 text-[11px] font-black uppercase tracking-wider text-white shadow-lg shadow-orange-500/40 ring-2 ring-white animate-pulse">
              <Sparkles size={12} className="fill-white text-white" />
              NEW DEAL
            </span>
          )}

          {deal.maxDiscountPercent > 0 && (
            <span className="rounded-full bg-amber-500 px-2.5 py-0.5 text-xs font-bold text-white shadow-md ring-1 ring-white/60">
              -{Math.round(deal.maxDiscountPercent)}%
            </span>
          )}
        </div>

        {/* Góc trên bên phải: Countdown Timer độc lập */}
        <span className="absolute top-3 right-3 z-10 rounded-full bg-neutral-900/75 px-2.5 py-1 text-[11px] font-semibold text-white shadow-xs backdrop-blur-xs">
          {timeLeft}
        </span>
      </div>

      {/* Deal Information */}
      <div className="flex flex-1 flex-col gap-2 p-4">
        <div className="flex flex-wrap items-center gap-1.5">
          {deal.isNew && (
            <span className="inline-flex items-center gap-1 rounded-md bg-orange-50 border border-orange-200 px-2 py-0.5 text-[11px] font-bold text-orange-700">
              <Sparkles size={11} className="text-orange-500" />
              NEW DEAL
            </span>
          )}
          {deal.categoryName && (
            <span className="w-fit rounded-sm bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-600">
              {deal.categoryName}
            </span>
          )}
        </div>

        <Link
          to={`/flash-deals/${deal.id}`}
          className="line-clamp-2 text-base font-semibold leading-tight text-neutral-900 hover:text-emerald-600"
        >
          {deal.productName}
        </Link>

        {/* Home vs Shop distinction */}
        {isSameShop ? (
          <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-500">
            <span className="flex items-center gap-1">
              <Package size={13} className="text-neutral-400" />
              {deal.variants.length} options
            </span>
            <span>·</span>
            <span className="flex items-center gap-1">
              <ShoppingBag size={13} className="text-neutral-400" />
              {remainingQty} left
            </span>
          </div>
        ) : (
          <div className="flex items-center gap-1.5 text-xs text-neutral-600">
            <Link
              to={`/shops/${deal.shopId}`}
              className="font-medium text-neutral-800 hover:text-emerald-700 hover:underline"
            >
              {deal.shopName}
            </Link>
            <span>·</span>
            <span className="flex items-center gap-0.5 text-amber-500">
              <Star size={12} className="fill-amber-400 text-amber-400" />
              5.0
            </span>
            {deal.distanceInKm !== undefined && deal.distanceInKm !== null && (
              <>
                <span>·</span>
                <span className="text-emerald-700 font-medium">{deal.distanceInKm} km</span>
              </>
            )}
          </div>
        )}

        {/* Pricing */}
        <div className="mt-1 flex items-baseline gap-2">
          <span className="text-lg font-bold text-emerald-700">
            {formatVnd(deal.minDealPrice)}
          </span>
          {deal.maxOriginalPrice > deal.minDealPrice && (
            <span className="text-xs text-neutral-400 line-through">
              {formatVnd(deal.maxOriginalPrice)}
            </span>
          )}
        </div>

        {/* Sales Progress Bar */}
        <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-neutral-100">
          <div
            className="h-full rounded-full bg-amber-500 transition-all duration-300"
            style={{ width: `${soldPct}%` }}
          />
        </div>

        <div className="flex items-center justify-between text-xs text-neutral-500">
          <span>Sold {soldQty}/{totalQty}</span>
          <span>{remainingQty} left</span>
        </div>

        {/* Action Button */}
        <Link
          to={`/flash-deals/${deal.id}`}
          className={`mt-3 block w-full rounded-lg py-2.5 text-center text-sm font-semibold transition ${
            isSoldOut
              ? "cursor-not-allowed bg-neutral-200 text-neutral-500"
              : "bg-emerald-600 text-white hover:bg-emerald-700"
          }`}
        >
          {isSoldOut ? "Sold Out" : "Add to Cart"}
        </Link>
      </div>
    </article>
  );
}