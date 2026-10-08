import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Package, ShoppingBag, Star } from "lucide-react";
import type { FlashDeal } from "@/features/flash-deals/types/flashDeal.types";

interface DealCardProps {
  deal: FlashDeal;
  isSameShop?: boolean; // cờ phân biệt dùng ở Trang chủ hay Trang Shop
}

// Hàm format tiền Việt Nam Đồng (VND)
const formatVnd = (value: number) => {
  return Number.isFinite(value) ? `${Math.round(value).toLocaleString("vi-VN")}đ` : "—";
};

export function DealCard({ deal, isSameShop = false }: DealCardProps) {
  // 1. Tính toán số lượng tổng, đã bán, và còn lại từ các biến thể
  const totalQty = deal.variants.reduce((sum, v) => sum + (v.totalQuantity || 0), 0) || 1;
  const soldQty = deal.variants.reduce((sum, v) => sum + (v.soldQuantity || 0), 0);
  const remainingQty = deal.variants.reduce((sum, v) => sum + (v.availableQuantity || 0), 0);

  const soldPct = Math.min(100, Math.round((soldQty / totalQty) * 100));
  const isSoldOut = remainingQty <= 0;

  // 2. Tính toán thời gian đếm ngược còn lại đến khi kết thúc đơn (orderEndTime)
  const getTimeLeft = () => {
    const diff = new Date(deal.orderEndTime).getTime() - Date.now();
    if (diff <= 0) return "Đã kết thúc";
    const min = Math.floor(diff / 60000);
    const h = Math.floor(min / 60);
    const m = min % 60;
    return h > 0 ? `Còn ${h}h ${m}p` : `Còn ${m} phút`;
  };

  const [timeLeft, setTimeLeft] = useState(getTimeLeft);

  // Tự động cập nhật đồng hồ mỗi 30 giây
  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft(getTimeLeft());
    }, 30000);
    return () => clearInterval(timer);
  }, [deal.orderEndTime]);

  const fallbackImg = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&auto=format&fit=crop&q=80";

  return (
    <article className="group flex flex-col overflow-hidden rounded-xl border border-neutral-200 bg-white shadow-sm transition hover:shadow-md">
      {/* Ảnh sản phẩm + Nhãn giảm giá & Đồng hồ */}
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

  {deal.maxDiscountPercent > 0 && (
    <span className="absolute top-3 left-3 rounded-full bg-amber-500 px-2.5 py-1 text-xs font-bold text-white shadow-sm">
      -{deal.maxDiscountPercent}%
      </span>
  )}

  <span className="absolute top-3 right-3 rounded-full bg-neutral-900/70 px-2.5 py-1 text-xs font-medium text-white backdrop-blur-xs">
    {timeLeft}
    </span>
    </div>

  {/* Thông tin Deal */}
  <div className="flex flex-1 flex-col gap-2 p-4">
  <Link
    to={`/flash-deals/${deal.id}`}
  className="line-clamp-2 text-base font-semibold leading-tight text-neutral-900 hover:text-emerald-600"
    >
    {deal.productName}
    </Link>

  {/* Khác biệt giữa Home và Shop */}
  {isSameShop ? (
    <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-500">
    <span className="flex items-center gap-1">
    <Package size={13} className="text-neutral-400" />
    {deal.variants.length} lựa chọn
  </span>
  <span>·</span>
  <span className="flex items-center gap-1">
  <ShoppingBag size={13} className="text-neutral-400" />
    Còn {remainingQty} suất
  </span>
  </div>
  ) : (
    <div className="flex items-center gap-1.5 text-xs text-neutral-600">
    <span className="font-medium text-neutral-800">{deal.shopName}</span>
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

  {/* Mức giá */}
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

  {/* Thanh tiến độ số lượng đã bán */}
  <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-neutral-100">
  <div
    className="h-full rounded-full bg-amber-500 transition-all duration-300"
  style={{ width: `${soldPct}%` }}
  />
  </div>

  <div className="flex items-center justify-between text-xs text-neutral-500">
    <span>Đã bán {soldQty}/{totalQty}</span>
  <span>Còn {remainingQty} suất</span>
  </div>

  {/* Nút xem chi tiết / Mua */}
  <Link
    to={`/flash-deals/${deal.id}`}
  className={`mt-3 block w-full rounded-lg py-2.5 text-center text-sm font-semibold transition ${
    isSoldOut
      ? "cursor-not-allowed bg-neutral-200 text-neutral-500"
      : "bg-emerald-600 text-white hover:bg-emerald-700"
  }`}
>
  {isSoldOut ? "Đã hết hàng" : "Xem chi tiết"}
  </Link>
  </div>
  </article>
);
}