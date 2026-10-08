import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

interface ShopDealCardProps {
  deal: FlashDeal;
  onAddToCart?: (deal: FlashDeal) => void;
}

const formatVndPrice = (value: number) => {
  return Number.isFinite(value)
    ? `${Math.round(value).toLocaleString("vi-VN")}đ`
    : "—";
};

export function ShopDealCard({ deal, onAddToCart }: ShopDealCardProps) {
  // 1. Calculate quantities
  const totalQty = deal.variants.reduce((sum, v) => sum + (v.totalQuantity || 0), 0) || 1;
  const soldQty = deal.variants.reduce((sum, v) => sum + (v.soldQuantity || 0), 0);
  const remainingQty = deal.variants.reduce((sum, v) => sum + (v.availableQuantity || 0), 0);

  const soldPct = Math.min(100, Math.round((soldQty / totalQty) * 100));
  const isSoldOut = remainingQty <= 0;

  // 2. Status / Time calculation
  const getTimeLeftText = () => {
    const diff = new Date(deal.orderEndTime).getTime() - Date.now();
    if (diff <= 0) return "Đã kết thúc";
    const min = Math.floor(diff / 60000);
    const h = Math.floor(min / 60);
    const m = min % 60;
    return h > 0 ? `Ends in ${h}h ${m}m` : `Ends in ${m}m`;
  };

  const [timeText, setTimeText] = useState(getTimeLeftText);

  useEffect(() => {
    const timer = setInterval(() => {
      setTimeText(getTimeLeftText());
    }, 30000);
    return () => clearInterval(timer);
  }, [deal.orderEndTime]);

  const fallbackImg =
    "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&auto=format&fit=crop&q=80";

  return (
    <article className="group flex flex-col overflow-hidden rounded-2xl border border-neutral-200/90 bg-white shadow-2xs transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md">
      {/* Product Image + Badges */}
      <div className="relative aspect-[4/3] w-full overflow-hidden bg-neutral-100">
        <Link to={`/flash-deals/${deal.id}`} className="block h-full w-full">
          <img
            src={deal.productImageUrl || fallbackImg}
            alt={deal.productName}
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
            onError={(e) => {
              e.currentTarget.src = fallbackImg;
            }}
          />
        </Link>

        {/* Top-left Discount Badge */}
        {deal.maxDiscountPercent > 0 && (
          <span className="absolute left-3 top-3 rounded-full bg-amber-500 px-2.5 py-0.5 text-xs font-bold text-white shadow-xs">
            -{deal.maxDiscountPercent}%
          </span>
        )}

        {/* Top-right Status / Timer Badge */}
        <span className="absolute right-3 top-3 rounded-full bg-neutral-900/75 px-2.5 py-0.5 text-xs font-medium text-white backdrop-blur-xs">
          {timeText}
        </span>
      </div>

      {/* Card Body */}
      <div className="flex flex-1 flex-col justify-between p-4">
        <div>
          {/* Category pill */}
          {deal.categoryName && (
            <span className="inline-block rounded-md bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-700">
              {deal.categoryName}
            </span>
          )}

          {/* Deal Name */}
          <Link
            to={`/flash-deals/${deal.id}`}
            title={deal.productName}
            className="mt-1.5 block line-clamp-1 text-base font-bold text-neutral-900 transition hover:text-emerald-700"
          >
            {deal.productName}
          </Link>

          {/* Meta Info line: Rating (if any) • X lựa chọn • Còn Y suất */}
          <div className="mt-1 flex items-center gap-1.5 text-xs text-neutral-500">
            {/* Rating is null as per user request (no rating API yet) */}
            <span className="flex items-center gap-1 font-medium">
              {deal.variants.length > 0
                ? `${deal.variants.length} lựa chọn`
                : "1 lựa chọn"}
            </span>
            <span>•</span>
            <span className="font-medium text-neutral-600">
              Còn {remainingQty} suất
            </span>
          </div>

          {/* Price Line */}
          <div className="mt-2.5 flex items-baseline gap-2">
            <span className="text-lg font-bold text-emerald-700">
              {formatVndPrice(deal.minDealPrice)}
            </span>
            {deal.maxOriginalPrice > deal.minDealPrice && (
              <span className="text-xs text-neutral-400 line-through">
                {formatVndPrice(deal.maxOriginalPrice)}
              </span>
            )}
          </div>

          {/* Sales Progress Bar */}
          <div className="mt-2.5 h-1.5 w-full overflow-hidden rounded-full bg-neutral-100">
            <div
              className="h-full rounded-full bg-amber-500 transition-all duration-300"
              style={{ width: `${soldPct}%` }}
            />
          </div>

          {/* Sales Quantities */}
          <div className="mt-1 flex items-center justify-between text-xs text-neutral-400">
            <span>
              Đã bán {soldQty}/{totalQty}
            </span>
            <span>Còn {remainingQty} suất</span>
          </div>
        </div>

        {/* Action Button: Thêm vào giỏ hàng */}
        <div className="mt-4">
          {isSoldOut ? (
            <button
              type="button"
              disabled
              className="w-full rounded-xl bg-neutral-100 py-2.5 text-center text-sm font-semibold text-neutral-400 cursor-not-allowed"
            >
              Hết suất ưu đãi
            </button>
          ) : onAddToCart ? (
            <button
              type="button"
              onClick={() => onAddToCart(deal)}
              className="w-full rounded-xl bg-emerald-600 py-2.5 text-center text-sm font-semibold text-white shadow-2xs transition hover:bg-emerald-700 active:scale-[0.99]"
            >
              Thêm vào giỏ hàng
            </button>
          ) : (
            <Link
              to={`/flash-deals/${deal.id}`}
              className="block w-full rounded-xl bg-emerald-600 py-2.5 text-center text-sm font-semibold text-white shadow-2xs transition hover:bg-emerald-700 active:scale-[0.99]"
            >
              Thêm vào giỏ hàng
            </Link>
          )}
        </div>
      </div>
    </article>
  );
}
